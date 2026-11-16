using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Helpers;
using ErpBackend.Models.Finance;

namespace ErpBackend.Services;

public sealed class AccountingPolicyOptions
{
    public decimal ApprovalThresholdTwd { get; set; } = 50_000m;
    public string DepartmentManagerRole { get; set; } = "DepartmentManager";
    public string CfoRole { get; set; } = "CFO";
    public string GeneralManagerRole { get; set; } = "GeneralManager";
    public string FinancePostingRole { get; set; } = "Finance";
}

public interface ISalesCreditNoteService
{
    Task<SalesCreditNoteDto> CreateAsync(
        CreateSalesCreditNoteRequest request,
        string idempotencyKey,
        Guid actorId,
        CancellationToken cancellationToken);

    Task<SalesCreditNoteDto> ApproveAsync(
        Guid creditNoteId,
        ApproveSalesCreditNoteRequest request,
        Guid actorId,
        IReadOnlyCollection<string> actorRoles,
        CancellationToken cancellationToken);

    Task<SalesCreditNoteDto> PostAsync(
        Guid creditNoteId,
        Guid actorId,
        IReadOnlyCollection<string> actorRoles,
        CancellationToken cancellationToken);

    Task<SalesCreditNoteDto?> GetAsync(Guid creditNoteId, CancellationToken cancellationToken);
}

public sealed class SalesCreditNoteService : ISalesCreditNoteService
{
    private readonly ErpGeneralDbContext _db;
    private readonly IAccountingService _accountingService;
    private readonly AccountingPolicyOptions _policy;

    public SalesCreditNoteService(
        ErpGeneralDbContext db,
        IAccountingService accountingService,
        IOptions<AccountingPolicyOptions> policy)
    {
        _db = db;
        _accountingService = accountingService;
        _policy = policy.Value;
        if (_policy.ApprovalThresholdTwd <= 0 ||
            string.IsNullOrWhiteSpace(_policy.DepartmentManagerRole) ||
            string.IsNullOrWhiteSpace(_policy.CfoRole) ||
            string.IsNullOrWhiteSpace(_policy.GeneralManagerRole) ||
            string.IsNullOrWhiteSpace(_policy.FinancePostingRole) ||
            new[]
            {
                _policy.DepartmentManagerRole,
                _policy.CfoRole,
                _policy.GeneralManagerRole
            }.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3)
            throw new InvalidOperationException("銷貨折讓簽核政策設定無效");
    }

    public async Task<SalesCreditNoteDto> CreateAsync(
        CreateSalesCreditNoteRequest request,
        string idempotencyKey,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("無效的建立者身分", nameof(actorId));
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100)
            throw new ArgumentException("Idempotency-Key 必須為 1 至 100 個字元", nameof(idempotencyKey));
        if (request.SalesInvoiceId == Guid.Empty || request.DeliveryOrderId == Guid.Empty)
            throw new ArgumentException("必須指定原始發票及出貨單");
        if (request.NetAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.NetAmount), "折讓未稅金額必須大於零");
        if (decimal.Round(request.NetAmount, 2) != request.NetAmount)
            throw new ArgumentException("折讓金額最多只能有兩位小數", nameof(request.NetAmount));
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500)
            throw new ArgumentException("折讓原因必須為 1 至 500 個字元", nameof(request.Reason));

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var existing = await _db.SalesCreditNotes
            .Include(note => note.SalesInvoice)
            .Include(note => note.DeliveryOrder)
            .Include(note => note.Approvals)
            .SingleOrDefaultAsync(note => note.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.SalesInvoiceId != request.SalesInvoiceId ||
                existing.DeliveryOrderId != request.DeliveryOrderId ||
                existing.NetAmount != request.NetAmount ||
                existing.Reason != request.Reason.Trim() ||
                existing.CreatedBy != actorId)
                throw new InvalidOperationException("Idempotency-Key 已用於不同的折讓單內容");
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return Map(existing);
        }

        var invoice = await _db.SalesInvoices
            .SingleOrDefaultAsync(item => item.Id == request.SalesInvoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("找不到原始銷貨發票");
        if (invoice.DeliveryOrderId != request.DeliveryOrderId ||
            invoice.Status.Equals("voided", StringComparison.OrdinalIgnoreCase) ||
            invoice.Status.Equals("draft", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("發票與出貨單不符，或發票尚未核准／已作廢");
        if (invoice.SalesAmount <= 0 || invoice.ExchangeRate <= 0)
            throw new InvalidOperationException("原始發票金額或匯率無效");

        var delivery = await _db.DeliveryOrders
            .SingleOrDefaultAsync(item => item.Id == request.DeliveryOrderId, cancellationToken)
            ?? throw new KeyNotFoundException("找不到原始出貨單");
        if (!delivery.Status.Equals("completed", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("銷貨折讓必須關聯已完成的原始出貨單");

        var priorNotes = await _db.SalesCreditNotes
            .Where(note => note.SalesInvoiceId == invoice.Id)
            .Select(note => new
            {
                note.Status,
                note.NetAmount,
                note.TaxAmount,
                note.TotalAmount,
                note.ReceivableAmount
            })
            .ToListAsync(cancellationToken);
        if (request.NetAmount > invoice.SalesAmount)
            throw new InvalidOperationException("折讓未稅金額不可超過原發票銷貨金額");

        var activeNotes = priorNotes
            .Where(note => note.Status is not ("Rejected" or "Cancelled"))
            .ToList();
        var nonCreditDiscounts = Math.Max(
            0m,
            invoice.DiscountedAmount -
            activeNotes.Where(note => note.Status == "Posted").Sum(note => note.ReceivableAmount));
        var remainingCreditable = invoice.TotalAmount - nonCreditDiscounts -
                                  activeNotes.Sum(note => note.TotalAmount);
        var reservedNet = activeNotes.Sum(note => note.NetAmount);
        var reservedTax = activeNotes.Sum(note => note.TaxAmount);
        if (request.NetAmount > invoice.SalesAmount - reservedNet)
            throw new InvalidOperationException("折讓未稅金額超過原發票尚可折讓餘額");

        var taxRemaining = Math.Max(0m, invoice.TaxAmount - reservedTax);
        var taxAmount = Math.Min(
            taxRemaining,
            Math.Round(invoice.TaxAmount * request.NetAmount / invoice.SalesAmount, 2, MidpointRounding.AwayFromZero));
        var totalAmount = request.NetAmount + taxAmount;
        if (totalAmount > remainingCreditable)
            throw new InvalidOperationException("折讓含稅金額超過原發票尚可折讓餘額");

        var note = new SalesCreditNote
        {
            CreditNoteNumber = DocumentNumberGenerator.Generate("SCN"),
            IdempotencyKey = idempotencyKey,
            SalesInvoiceId = invoice.Id,
            SalesInvoice = invoice,
            DeliveryOrderId = delivery.Id,
            DeliveryOrder = delivery,
            Reason = request.Reason.Trim(),
            NetAmount = request.NetAmount,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            BaseTotalAmount = Math.Round(totalAmount * invoice.ExchangeRate, 2, MidpointRounding.AwayFromZero),
            Currency = invoice.Currency,
            ExchangeRate = invoice.ExchangeRate,
            CreatedBy = actorId,
            CreatedAt = DateTime.UtcNow,
            Status = "PendingApproval"
        };

        _db.SalesCreditNotes.Add(note);
        await _db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return Map(note);
    }

    public async Task<SalesCreditNoteDto> ApproveAsync(
        Guid creditNoteId,
        ApproveSalesCreditNoteRequest request,
        Guid actorId,
        IReadOnlyCollection<string> actorRoles,
        CancellationToken cancellationToken)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("無效的簽核者身分", nameof(actorId));

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var note = await LoadAsync(creditNoteId, cancellationToken)
            ?? throw new KeyNotFoundException("找不到銷貨折讓單");
        if (note.Status != "PendingApproval")
            throw new InvalidOperationException("只有待簽核折讓單可以簽核");
        if (note.CreatedBy == actorId || note.Approvals.Any(item => item.ApproverId == actorId))
            throw new InvalidOperationException("建立者不得簽核，且同一人不可重複完成多個簽核角色");

        var requestedRole = request.ApprovalRole?.Trim();
        var isSmallCredit = note.BaseTotalAmount <= _policy.ApprovalThresholdTwd;
        var approvalRole = isSmallCredit &&
                           string.Equals(requestedRole, _policy.DepartmentManagerRole, StringComparison.OrdinalIgnoreCase)
            ? _policy.DepartmentManagerRole
            : !isSmallCredit && string.Equals(requestedRole, _policy.CfoRole, StringComparison.OrdinalIgnoreCase)
                ? _policy.CfoRole
                : !isSmallCredit && string.Equals(requestedRole, _policy.GeneralManagerRole, StringComparison.OrdinalIgnoreCase)
                    ? _policy.GeneralManagerRole
                    : null;
        if (approvalRole is null ||
            !actorRoles.Contains(approvalRole, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("目前帳號不具備此折讓單所需的簽核角色");
        if (note.Approvals.Any(item =>
                item.ApprovalRole.Equals(approvalRole, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("此簽核角色已完成簽核");
        if (request.Comments?.Length > 500)
            throw new ArgumentException("簽核備註不可超過 500 個字元", nameof(request.Comments));

        var approval = new SalesCreditNoteApproval
        {
            SalesCreditNoteId = note.Id,
            ApprovalRole = approvalRole,
            ApproverId = actorId,
            Comments = string.IsNullOrWhiteSpace(request.Comments) ? null : request.Comments.Trim(),
            ApprovedAt = DateTime.UtcNow
        };
        _db.SalesCreditNoteApprovals.Add(approval);
        note.Approvals.Add(approval);

        var approvedRoles = note.Approvals.Select(item => item.ApprovalRole).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isApproved = isSmallCredit
            ? approvedRoles.Contains(_policy.DepartmentManagerRole)
            : approvedRoles.Contains(_policy.CfoRole) && approvedRoles.Contains(_policy.GeneralManagerRole);
        if (isApproved)
            note.Status = "Approved";

        await _db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return Map(note);
    }

    public async Task<SalesCreditNoteDto> PostAsync(
        Guid creditNoteId,
        Guid actorId,
        IReadOnlyCollection<string> actorRoles,
        CancellationToken cancellationToken)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("無效的過帳者身分", nameof(actorId));
        if (!actorRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase)
            && !actorRoles.Contains(_policy.FinancePostingRole, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("目前帳號不具備財務過帳角色");

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var note = await LoadAsync(creditNoteId, cancellationToken)
            ?? throw new KeyNotFoundException("找不到銷貨折讓單");
        if (note.Status == "Posted")
        {
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return Map(note);
        }
        if (note.Status != "Approved")
            throw new InvalidOperationException("折讓單尚未完成必要簽核");

        var receivableOutstanding = Math.Max(
            0m,
            note.SalesInvoice.TotalAmount -
            note.SalesInvoice.PaidAmount -
            note.SalesInvoice.DiscountedAmount);
        note.ReceivableAmount = Math.Min(note.TotalAmount, receivableOutstanding);
        note.CustomerAdvanceAmount = note.TotalAmount - note.ReceivableAmount;
        await _accountingService.CreateVoucherFromSalesCreditNoteAsync(note.Id);
        var invoice = await _db.SalesInvoices
            .SingleAsync(item => item.Id == note.SalesInvoiceId, cancellationToken);
        invoice.DiscountedAmount += note.ReceivableAmount;
        note.Status = "Posted";
        note.PostedBy = actorId;
        note.PostedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return Map(note);
    }

    public async Task<SalesCreditNoteDto?> GetAsync(Guid creditNoteId, CancellationToken cancellationToken)
    {
        var note = await LoadAsync(creditNoteId, cancellationToken);
        return note is null ? null : Map(note);
    }

    private Task<SalesCreditNote?> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        _db.SalesCreditNotes
            .Include(note => note.SalesInvoice)
            .Include(note => note.DeliveryOrder)
            .Include(note => note.Approvals)
            .SingleOrDefaultAsync(note => note.Id == id, cancellationToken);

    private static SalesCreditNoteDto Map(SalesCreditNote note) =>
        new(
            note.Id,
            note.CreditNoteNumber,
            note.SalesInvoiceId,
            note.SalesInvoice.InvoiceNumber,
            note.DeliveryOrderId,
            note.DeliveryOrder.DeliveryNumber,
            note.NetAmount,
            note.TaxAmount,
            note.TotalAmount,
            note.BaseTotalAmount,
            note.ReceivableAmount,
            note.CustomerAdvanceAmount,
            note.Currency,
            note.ExchangeRate,
            note.Status,
            note.CreatedAt,
            note.Approvals
                .OrderBy(approval => approval.ApprovedAt)
                .Select(approval => new SalesCreditNoteApprovalDto(
                    approval.ApprovalRole,
                    approval.ApproverId,
                    approval.Comments,
                    approval.ApprovedAt))
                .ToList());
}
