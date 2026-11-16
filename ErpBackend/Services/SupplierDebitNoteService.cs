using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Helpers;
using ErpBackend.Models.Finance;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Procurement;

namespace ErpBackend.Services;

public interface ISupplierDebitNoteService
{
    Task<SupplierDebitNoteDto> CreateAsync(
        CreateSupplierDebitNoteRequest request,
        string idempotencyKey,
        Guid actorId,
        CancellationToken cancellationToken);

    Task<SupplierDebitNoteDto> ApproveAsync(
        Guid debitNoteId,
        ApproveSupplierDebitNoteRequest request,
        Guid actorId,
        IReadOnlyCollection<string> actorRoles,
        CancellationToken cancellationToken);

    Task<SupplierDebitNoteDto> PostAsync(
        Guid debitNoteId,
        Guid actorId,
        IReadOnlyCollection<string> actorRoles,
        CancellationToken cancellationToken);

    Task<SupplierDebitNoteDto?> GetAsync(Guid debitNoteId, CancellationToken cancellationToken);
}

public sealed class SupplierDebitNoteService : ISupplierDebitNoteService
{
    private readonly ErpGeneralDbContext _db;
    private readonly IAccountingService _accountingService;
    private readonly AccountingPolicyOptions _policy;

    public SupplierDebitNoteService(
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
            throw new InvalidOperationException("採購借項簽核政策設定無效");
    }

    public async Task<SupplierDebitNoteDto> CreateAsync(
        CreateSupplierDebitNoteRequest request,
        string idempotencyKey,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        ValidateCreateRequest(request, idempotencyKey, actorId);
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var existing = await LoadByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.AccountsPayableId != request.AccountsPayableId ||
                existing.IqcReportId != request.IqcReportId ||
                existing.ReturnedQty != request.ReturnedQty ||
                existing.Reason != request.Reason.Trim() ||
                existing.CreatedBy != actorId)
                throw new InvalidOperationException("Idempotency-Key 已用於不同的採購借項內容");
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return Map(existing);
        }

        var iqc = await _db.IqcReports
            .Include(item => item.GoodsReceipt)
            .Include(item => item.GoodsReceiptItem)
            .Include(item => item.Supplier)
            .Include(item => item.Sku)
            .SingleOrDefaultAsync(item => item.Id == request.IqcReportId, cancellationToken)
            ?? throw new KeyNotFoundException("找不到 IQC 檢驗單");

        if (!iqc.Result.Equals("fail", StringComparison.OrdinalIgnoreCase) ||
            iqc.GoodsReceipt is null ||
            !iqc.GoodsReceipt.Status.Equals("received", StringComparison.OrdinalIgnoreCase) ||
            iqc.GoodsReceiptItem is null ||
            !iqc.GoodsReceiptItem.IqcResult.Equals("fail", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("採購借項必須關聯已完成且判定不合格的收貨 IQC");
        if (iqc.SkuId is null || iqc.Sku is null ||
            iqc.GoodsReceiptItem.SkuId != iqc.SkuId ||
            iqc.GoodsReceiptItem.GoodsReceiptId != iqc.GoodsReceiptId ||
            iqc.SupplierId != iqc.GoodsReceipt.SupplierId)
            throw new InvalidOperationException("IQC、收貨明細、物料或供應商資料不一致");
        if (string.IsNullOrWhiteSpace(iqc.LotNumber) ||
            !string.Equals(iqc.LotNumber, iqc.GoodsReceiptItem.LotNumber, StringComparison.Ordinal))
            throw new InvalidOperationException("IQC 與收貨明細必須具有相同的批號才能退料");
        if (iqc.GoodsReceiptItem.RejectedQty <= 0 ||
            iqc.GoodsReceiptItem.InventoryQty <= 0 ||
            iqc.GoodsReceiptItem.LocalCurrencyPrice <= 0 ||
            iqc.GoodsReceiptItem.ForeignCurrencyPrice <= 0)
            throw new InvalidOperationException("收貨拒收數量或成本資料無效");

        var ap = await _db.AccountsPayables
            .SingleOrDefaultAsync(item => item.Id == request.AccountsPayableId, cancellationToken)
            ?? throw new KeyNotFoundException("找不到應付帳款");
        if (ap.GoodsReceiptId != iqc.GoodsReceiptId ||
            ap.SupplierId != iqc.SupplierId ||
            !ap.Currency.Equals(iqc.GoodsReceiptItem.Currency, StringComparison.OrdinalIgnoreCase) ||
            ap.ExchangeRate != iqc.GoodsReceiptItem.ExchangeRate)
            throw new InvalidOperationException("應付帳款必須屬於相同供應商、收貨單，且幣別與匯率須一致");
        if (ap.InvoiceAmount <= 0 || ap.TaxAmount < 0 ||
            ap.TotalAmount < ap.InvoiceAmount || ap.BaseInvoiceAmount <= 0 ||
            ap.BaseTaxAmount < 0 || ap.BaseTotalAmount < ap.BaseInvoiceAmount ||
            ap.ExchangeRate <= 0)
            throw new InvalidOperationException("應付帳款金額或匯率資料無效");

        var sku = await LockSkuAsync(iqc.SkuId.Value, cancellationToken)
            ?? throw new InvalidOperationException("IQC 物料不存在");
        var warehouseId = iqc.GoodsReceipt.WarehouseId;
        var binLocationId = iqc.GoodsReceiptItem.BinLocationId;
        var lot = await _db.InventoryLots.SingleOrDefaultAsync(
            item => item.SkuId == iqc.SkuId &&
                    item.LotNumber == iqc.LotNumber &&
                    item.WarehouseId == warehouseId &&
                    item.BinLocationId == binLocationId,
            cancellationToken);
        if (lot is null || !lot.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("找不到 IQC 隔離中的拒收批號庫存");

        var lotBalance = await GetLotBinBalanceAsync(
            iqc.SkuId.Value, iqc.LotNumber, warehouseId, binLocationId, cancellationToken);
        if (lotBalance <= 0 || lotBalance != lot.Qty)
            throw new InvalidOperationException("批號台帳與庫存交易數量不一致，請先人工核對");
        if (request.ReturnedQty > iqc.GoodsReceiptItem.RejectedQty)
            throw new InvalidOperationException("退料數量超過 IQC 拒收數量");

        var priorNotes = await _db.SupplierDebitNotes
            .Where(note => note.IqcReportId == iqc.Id)
            .Select(note => new
            {
                note.Status,
                note.ReturnedQty,
                note.TotalAmount
            })
            .ToListAsync(cancellationToken);
        var activeNotes = priorNotes
            .Where(note => note.Status is not ("Rejected" or "Cancelled"))
            .ToList();
        if (request.ReturnedQty > iqc.GoodsReceiptItem.RejectedQty -
                                  activeNotes.Sum(note => note.ReturnedQty))
            throw new InvalidOperationException("退料數量超過尚未建立借項的 IQC 拒收餘額");
        if (request.ReturnedQty > lotBalance -
                                  await GetReservedReturnQtyAsync(iqc.Id, cancellationToken))
            throw new InvalidOperationException("退料數量超過批號尚存庫存");

        var netAmount = Math.Round(
            request.ReturnedQty * iqc.GoodsReceiptItem.ForeignCurrencyPrice,
            2,
            MidpointRounding.AwayFromZero);
        var baseNetAmount = Math.Round(
            request.ReturnedQty * iqc.GoodsReceiptItem.LocalCurrencyPrice,
            2,
            MidpointRounding.AwayFromZero);
        var taxRate = ap.InvoiceAmount > 0 ? ap.TaxAmount / ap.InvoiceAmount : 0m;
        var baseTaxRate = ap.BaseInvoiceAmount > 0 ? ap.BaseTaxAmount / ap.BaseInvoiceAmount : 0m;
        var taxAmount = Math.Round(netAmount * taxRate, 2, MidpointRounding.AwayFromZero);
        var baseTaxAmount = Math.Round(baseNetAmount * baseTaxRate, 2, MidpointRounding.AwayFromZero);
        var totalAmount = netAmount + taxAmount;
        var baseTotalAmount = baseNetAmount + baseTaxAmount;
        if (netAmount <= 0 || totalAmount > ap.TotalAmount)
            throw new InvalidOperationException("退料折讓金額無效或超過應付帳款金額");

        var alreadyPaid = await GetPaidAmountAsync(ap.Id, cancellationToken);
        var alreadyDebited = activeNotes.Sum(note => note.TotalAmount);
        if (totalAmount + alreadyPaid + alreadyDebited > ap.TotalAmount)
            throw new InvalidOperationException("退料借項與已付款合計超過原應付帳款餘額");

        var inventoryRatio = Math.Clamp(lotBalance / iqc.GoodsReceiptItem.InventoryQty, 0m, 1m);
        var inventoryAllocation = Math.Round(netAmount * inventoryRatio, 2, MidpointRounding.AwayFromZero);
        var cogsAllocation = netAmount - inventoryAllocation;
        var baseInventoryAllocation = Math.Round(
            baseNetAmount * inventoryRatio,
            2,
            MidpointRounding.AwayFromZero);
        var baseCogsAllocation = baseNetAmount - baseInventoryAllocation;

        var note = new SupplierDebitNote
        {
            DebitNoteNumber = DocumentNumberGenerator.Generate("SDN"),
            IdempotencyKey = idempotencyKey,
            AccountsPayableId = ap.Id,
            AccountsPayable = ap,
            GoodsReceiptId = iqc.GoodsReceiptId,
            GoodsReceipt = iqc.GoodsReceipt,
            GoodsReceiptItemId = iqc.GoodsReceiptItemId!.Value,
            GoodsReceiptItem = iqc.GoodsReceiptItem,
            IqcReportId = iqc.Id,
            IqcReport = iqc,
            SupplierId = iqc.SupplierId,
            Supplier = iqc.Supplier!,
            SkuId = iqc.SkuId.Value,
            Sku = sku,
            LotNumber = iqc.LotNumber,
            ReturnedQty = request.ReturnedQty,
            NetAmount = netAmount,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            BaseNetAmount = baseNetAmount,
            BaseTaxAmount = baseTaxAmount,
            BaseTotalAmount = baseTotalAmount,
            InventoryAllocation = inventoryAllocation,
            CogsAllocation = cogsAllocation,
            BaseInventoryAllocation = baseInventoryAllocation,
            BaseCogsAllocation = baseCogsAllocation,
            Currency = ap.Currency,
            ExchangeRate = ap.ExchangeRate,
            Reason = request.Reason.Trim(),
            Status = "PendingApproval",
            CreatedBy = actorId,
            CreatedAt = DateTime.UtcNow
        };
        _db.SupplierDebitNotes.Add(note);
        await _db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return Map(note);
    }

    public async Task<SupplierDebitNoteDto> ApproveAsync(
        Guid debitNoteId,
        ApproveSupplierDebitNoteRequest request,
        Guid actorId,
        IReadOnlyCollection<string> actorRoles,
        CancellationToken cancellationToken)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("無效的簽核者身分", nameof(actorId));
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var note = await LoadAsync(debitNoteId, cancellationToken)
            ?? throw new KeyNotFoundException("找不到採購借項單");
        if (note.Status != "PendingApproval")
            throw new InvalidOperationException("只有待簽核採購借項單可以簽核");
        if (note.CreatedBy == actorId || note.Approvals.Any(item => item.ApproverId == actorId))
            throw new InvalidOperationException("建立者不得簽核，且同一人不可重複完成多個簽核角色");

        var approvalRole = ResolveApprovalRole(note.BaseTotalAmount, request.ApprovalRole);
        if (approvalRole is null ||
            !actorRoles.Contains(approvalRole, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("目前帳號不具備此採購借項所需的簽核角色");
        if (note.Approvals.Any(item =>
                item.ApprovalRole.Equals(approvalRole, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("此簽核角色已完成簽核");
        if (request.Comments?.Length > 500)
            throw new ArgumentException("簽核備註不可超過 500 個字元", nameof(request.Comments));

        var approval = new SupplierDebitNoteApproval
        {
            SupplierDebitNoteId = note.Id,
            ApprovalRole = approvalRole,
            ApproverId = actorId,
            Comments = string.IsNullOrWhiteSpace(request.Comments) ? null : request.Comments.Trim(),
            ApprovedAt = DateTime.UtcNow
        };
        _db.SupplierDebitNoteApprovals.Add(approval);
        note.Approvals.Add(approval);
        var roles = note.Approvals
            .Select(item => item.ApprovalRole)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (HasRequiredApprovals(note.BaseTotalAmount, roles))
            note.Status = "Approved";

        await _db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return Map(note);
    }

    public async Task<SupplierDebitNoteDto> PostAsync(
        Guid debitNoteId,
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
        var note = await LoadAsync(debitNoteId, cancellationToken)
            ?? throw new KeyNotFoundException("找不到採購借項單");
        if (note.Status == "Posted")
        {
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return Map(note);
        }
        if (note.Status != "Approved")
            throw new InvalidOperationException("採購借項單尚未完成必要簽核");

        var sku = await LockSkuAsync(note.SkuId, cancellationToken)
            ?? throw new InvalidOperationException("採購借項物料不存在");
        var receipt = note.GoodsReceipt;
        var receiptItem = note.GoodsReceiptItem;
        var iqc = note.IqcReport;
        if (!iqc.Result.Equals("fail", StringComparison.OrdinalIgnoreCase) ||
            !receiptItem.IqcResult.Equals("fail", StringComparison.OrdinalIgnoreCase) ||
            !receipt.Status.Equals("received", StringComparison.OrdinalIgnoreCase) ||
            receiptItem.GoodsReceiptId != receipt.Id ||
            iqc.GoodsReceiptItemId != receiptItem.Id ||
            !string.Equals(receiptItem.LotNumber, note.LotNumber, StringComparison.Ordinal))
            throw new InvalidOperationException("IQC 或收貨資料已變更，採購借項未過帳");

        var lot = await _db.InventoryLots.SingleOrDefaultAsync(
            item => item.SkuId == note.SkuId &&
                    item.LotNumber == note.LotNumber &&
                    item.WarehouseId == receipt.WarehouseId &&
                    item.BinLocationId == receiptItem.BinLocationId,
            cancellationToken);
        if (lot is null || !lot.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("拒收批號不再處於可退料的隔離狀態");

        var lotBalance = await GetLotBinBalanceAsync(
            note.SkuId, note.LotNumber, receipt.WarehouseId, receiptItem.BinLocationId, cancellationToken);
        if (lotBalance != lot.Qty || lotBalance < note.ReturnedQty || sku.Qty < note.ReturnedQty)
            throw new InvalidOperationException("拒收批號或物料可用庫存不足，借項未過帳");

        var alreadyPaid = await GetPaidAmountAsync(note.AccountsPayableId, cancellationToken);
        var otherDebits = await _db.SupplierDebitNotes
            .Where(item => item.AccountsPayableId == note.AccountsPayableId &&
                           item.Id != note.Id &&
                           item.Status != "Rejected" &&
                           item.Status != "Cancelled")
            .Select(item => item.TotalAmount)
            .ToListAsync(cancellationToken);
        if (alreadyPaid + otherDebits.Sum() + note.TotalAmount >
            note.AccountsPayable.TotalAmount)
            throw new InvalidOperationException("採購借項與已付款合計超過應付帳款金額");

        var now = DateTime.UtcNow;
        var postingDate = DateOnly.FromDateTime(now);
        if (await _db.AccountingPeriods.AnyAsync(period =>
                period.IsClosed &&
                period.StartDate <= postingDate &&
                period.EndDate >= postingDate,
                cancellationToken))
            throw new InvalidOperationException("傳票日期所屬會計期間已關帳，禁止過帳");

        var binBalance = await GetBinBalanceAsync(
            note.SkuId, receipt.WarehouseId, receiptItem.BinLocationId, cancellationToken);
        var inventoryTransaction = new InventoryTransaction
        {
            TrxId = $"SDR-{Guid.NewGuid():N}"[..30],
            SkuId = note.SkuId,
            Type = "supplier_return",
            QtyChange = -note.ReturnedQty,
            QtyBefore = binBalance,
            QtyAfter = binBalance - note.ReturnedQty,
            LotNumber = note.LotNumber,
            RelatedDocType = "SupplierDebitNote",
            RelatedDocId = note.Id,
            WarehouseId = receipt.WarehouseId,
            BinLocationId = receiptItem.BinLocationId,
            HandlerId = actorId,
            UnitCost = note.BaseNetAmount / note.ReturnedQty,
            TotalCost = -note.BaseNetAmount,
            Notes = $"IQC 驗退退料 {iqc.ReportNumber}；借項 {note.DebitNoteNumber}",
            CreatedAt = now
        };
        _db.InventoryTransactions.Add(inventoryTransaction);
        sku.Qty -= note.ReturnedQty;
        lot.Qty -= note.ReturnedQty;
        if (lot.Qty == 0)
            lot.Status = "returned";

        note.AccountsPayable.AllowanceAmount =
            (note.AccountsPayable.AllowanceAmount ?? 0m) + note.TotalAmount;
        note.AccountsPayable.DeductionReason = note.Reason;
        await _accountingService.CreateVoucherFromSupplierDebitNoteAsync(note.Id, now);
        note.Status = "Posted";
        note.PostedBy = actorId;
        note.PostedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return Map(note);
    }

    public async Task<SupplierDebitNoteDto?> GetAsync(Guid debitNoteId, CancellationToken cancellationToken)
    {
        var note = await LoadAsync(debitNoteId, cancellationToken);
        return note is null ? null : Map(note);
    }

    private async Task<SupplierDebitNote?> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.SupplierDebitNotes
            .Include(item => item.AccountsPayable)
            .Include(item => item.GoodsReceipt)
            .Include(item => item.GoodsReceiptItem)
            .Include(item => item.IqcReport)
            .Include(item => item.Supplier)
            .Include(item => item.Sku)
            .Include(item => item.Approvals)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    private async Task<SupplierDebitNote?> LoadByIdempotencyKeyAsync(
        string key,
        CancellationToken cancellationToken) =>
        await _db.SupplierDebitNotes
            .Include(item => item.AccountsPayable)
            .Include(item => item.GoodsReceipt)
            .Include(item => item.GoodsReceiptItem)
            .Include(item => item.IqcReport)
            .Include(item => item.Supplier)
            .Include(item => item.Sku)
            .Include(item => item.Approvals)
            .SingleOrDefaultAsync(item => item.IdempotencyKey == key, cancellationToken);

    private async Task<decimal> GetReservedReturnQtyAsync(
        Guid iqcReportId,
        CancellationToken cancellationToken)
    {
        var quantities = await _db.SupplierDebitNotes
            .Where(note => note.IqcReportId == iqcReportId &&
                           note.Status != "Rejected" &&
                           note.Status != "Cancelled" &&
                           note.Status != "Posted")
            .Select(note => note.ReturnedQty)
            .ToListAsync(cancellationToken);
        return quantities.Sum();
    }

    private async Task<decimal> GetPaidAmountAsync(Guid apId, CancellationToken cancellationToken)
    {
        var payments = await (
            from item in _db.PaymentRecords
            join batch in _db.PaymentBatches on item.PaymentBatchId equals batch.Id
            where item.AccountsPayableId == apId &&
                  batch.Status.ToLower() == "posted"
            select new { item.PaidAmount, item.DiscountAmount }
        )
            .ToListAsync(cancellationToken);
        return payments.Sum(item => item.PaidAmount + item.DiscountAmount);
    }

    private async Task<decimal> GetLotBinBalanceAsync(
        Guid skuId,
        string lotNumber,
        Guid warehouseId,
        Guid? binLocationId,
        CancellationToken cancellationToken)
    {
        var quantities = await _db.InventoryTransactions
            .Where(item => item.SkuId == skuId &&
                           item.LotNumber == lotNumber &&
                           item.WarehouseId == warehouseId &&
                           item.BinLocationId == binLocationId)
            .Select(item => item.QtyChange)
            .ToListAsync(cancellationToken);
        return quantities.Sum();
    }

    private async Task<decimal> GetBinBalanceAsync(
        Guid skuId,
        Guid warehouseId,
        Guid? binLocationId,
        CancellationToken cancellationToken)
    {
        var quantities = await _db.InventoryTransactions
            .Where(item => item.SkuId == skuId &&
                           item.WarehouseId == warehouseId &&
                           item.BinLocationId == binLocationId)
            .Select(item => item.QtyChange)
            .ToListAsync(cancellationToken);
        return quantities.Sum();
    }

    private async Task<Sku?> LockSkuAsync(Guid skuId, CancellationToken cancellationToken)
    {
        if (_db.Database.IsRelational() &&
            _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var sku = await _db.Skus
                .FromSqlInterpolated($"SELECT * FROM \"Skus\" WHERE \"Id\" = {skuId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (sku is not null)
                await _db.Entry(sku).ReloadAsync(cancellationToken);
            return sku;
        }

        return await _db.Skus.SingleOrDefaultAsync(item => item.Id == skuId, cancellationToken);
    }

    private string? ResolveApprovalRole(decimal baseAmount, string? requestedRole)
    {
        var role = requestedRole?.Trim();
        var isSmallAmount = baseAmount <= _policy.ApprovalThresholdTwd;
        if (isSmallAmount &&
            string.Equals(role, _policy.DepartmentManagerRole, StringComparison.OrdinalIgnoreCase))
            return _policy.DepartmentManagerRole;
        if (!isSmallAmount && string.Equals(role, _policy.CfoRole, StringComparison.OrdinalIgnoreCase))
            return _policy.CfoRole;
        if (!isSmallAmount &&
            string.Equals(role, _policy.GeneralManagerRole, StringComparison.OrdinalIgnoreCase))
            return _policy.GeneralManagerRole;
        return null;
    }

    private bool HasRequiredApprovals(decimal baseAmount, IReadOnlySet<string> roles) =>
        baseAmount <= _policy.ApprovalThresholdTwd
            ? roles.Contains(_policy.DepartmentManagerRole)
            : roles.Contains(_policy.CfoRole) && roles.Contains(_policy.GeneralManagerRole);

    private static void ValidateCreateRequest(
        CreateSupplierDebitNoteRequest request,
        string idempotencyKey,
        Guid actorId)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("無效的建立者身分", nameof(actorId));
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100)
            throw new ArgumentException("Idempotency-Key 必須為 1 至 100 個字元", nameof(idempotencyKey));
        if (request.AccountsPayableId == Guid.Empty || request.IqcReportId == Guid.Empty)
            throw new ArgumentException("必須指定應付帳款及 IQC 檢驗單");
        if (request.ReturnedQty <= 0 || decimal.Round(request.ReturnedQty, 2) != request.ReturnedQty)
            throw new ArgumentOutOfRangeException(nameof(request.ReturnedQty), "退料數量必須大於零且最多兩位小數");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500)
            throw new ArgumentException("退料原因必須為 1 至 500 個字元", nameof(request.Reason));
    }

    private static SupplierDebitNoteDto Map(SupplierDebitNote note) =>
        new(
            note.Id,
            note.DebitNoteNumber,
            note.AccountsPayableId,
            note.AccountsPayable.ApNumber,
            note.GoodsReceiptId,
            note.GoodsReceipt.GrNumber,
            note.GoodsReceiptItemId,
            note.IqcReportId,
            note.IqcReport.ReportNumber,
            note.SupplierId,
            note.Supplier.SupplierName,
            note.SkuId,
            note.Sku.SkuNumber,
            note.LotNumber,
            note.ReturnedQty,
            note.NetAmount,
            note.TaxAmount,
            note.TotalAmount,
            note.BaseNetAmount,
            note.BaseTaxAmount,
            note.BaseTotalAmount,
            note.InventoryAllocation,
            note.CogsAllocation,
            note.BaseInventoryAllocation,
            note.BaseCogsAllocation,
            note.Currency,
            note.ExchangeRate,
            note.Reason,
            note.Status,
            note.CreatedAt,
            note.Approvals
                .OrderBy(item => item.ApprovedAt)
                .Select(item => new SupplierDebitNoteApprovalDto(
                    item.ApprovalRole,
                    item.ApproverId,
                    item.Comments,
                    item.ApprovedAt))
                .ToList());
}
