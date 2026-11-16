using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Procurement;
using ErpBackend.Helpers;

namespace ErpBackend.Services;

public interface IRfqService
{
    Task<List<RfqDto>> GetAllAsync(string? status = null, string? searchTerm = null, int page = 1, int pageSize = 20);
    Task<RfqDto?> GetByIdAsync(Guid id);
    Task<RfqDto?> CreateAsync(CreateRfqRequest req);
    Task<bool> AddSupplierResponseAsync(Guid id, AddRfqResponseRequest req);
    Task<bool> UpdateStatusAsync(Guid id, UpdateRfqStatusRequest req);
    Task<int> AssignSuppliersAsync(
        Guid rfqId,
        AssignRfqSuppliersRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);
}

public class RfqService : IRfqService
{
    private readonly ErpGeneralDbContext _db;

    public RfqService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<RfqDto>> GetAllAsync(string? status = null, string? searchTerm = null, int page = 1, int pageSize = 20)
    {
        var query = _db.Rfqs.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(r => r.RfqNumber.Contains(searchTerm));

        return await query
            .OrderByDescending(r => r.RfqDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RfqDto
            {
                Id = r.Id,
                RfqNumber = r.RfqNumber,
                PrId = r.PrId,
                RfqDate = r.RfqDate,
                Deadline = r.Deadline,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                ItemCount = r.Items.Count,
                ResponseCount = r.SupplierResponses.Count
            })
            .ToListAsync();
    }

    public async Task<RfqDto?> GetByIdAsync(Guid id)
    {
        var r = await _db.Rfqs
            .Include(r => r.Items).ThenInclude(i => i.Sku)
            .Include(r => r.SupplierResponses).ThenInclude(s => s.Supplier)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (r == null) return null;

        return new RfqDto
        {
            Id = r.Id,
            RfqNumber = r.RfqNumber,
            PrId = r.PrId,
            RfqDate = r.RfqDate,
            Deadline = r.Deadline,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            Items = r.Items.Select(i => new RfqItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                SkuNumber = i.Sku?.SkuNumber,
                SkuName = i.Sku?.SkuName,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Specifications = i.Specifications
            }).ToList(),
            SupplierResponses = r.SupplierResponses.Select(s => new RfqSupplierResponseDto
            {
                Id = s.Id,
                SupplierId = s.SupplierId,
                SupplierName = s.Supplier?.SupplierName,
                SkuId = s.SkuId,
                UnitPrice = s.UnitPrice,
                LeadTimeDays = s.LeadTimeDays,
                Moq = s.Moq,
                TradeTerms = s.TradeTerms,
                Notes = s.Notes,
                IsSelected = s.IsSelected
            }).ToList()
        };
    }

    public async Task<RfqDto?> CreateAsync(CreateRfqRequest req)
    {
        var rfq = new Rfq
        {
            Id = Guid.NewGuid(),
            RfqNumber = DocumentNumberGenerator.Generate("RFQ"),
            PrId = req.PrId,
            RfqDate = req.RfqDate ?? DateTime.UtcNow,
            Deadline = req.Deadline,
            Status = "open"
        };

        if (req.Items != null)
        {
            foreach (var item in req.Items)
            {
                rfq.Items.Add(new RfqItem
                {
                    Id = Guid.NewGuid(),
                    RfqId = rfq.Id,
                    SkuId = item.SkuId,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    Specifications = item.Specifications
                });
            }
        }

        _db.Rfqs.Add(rfq);
        await _db.SaveChangesAsync();

        return new RfqDto
        {
            Id = rfq.Id,
            RfqNumber = rfq.RfqNumber,
            RfqDate = rfq.RfqDate,
            Deadline = rfq.Deadline,
            Status = rfq.Status
        };
    }

    public async Task<bool> AddSupplierResponseAsync(Guid id, AddRfqResponseRequest req)
    {
        var response = new RfqSupplierResponse
        {
            Id = Guid.NewGuid(),
            RfqId = id,
            SupplierId = req.SupplierId,
            SkuId = req.SkuId,
            UnitPrice = req.UnitPrice,
            LeadTimeDays = req.LeadTimeDays ?? 0,
            Moq = req.Moq ?? 0m,
            TradeTerms = req.TradeTerms,
            Notes = req.Notes
        };

        _db.RfqSupplierResponses.Add(response);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, UpdateRfqStatusRequest req)
    {
        var rfq = await _db.Rfqs.FirstOrDefaultAsync(r => r.Id == id);
        if (rfq == null) return false;

        rfq.Status = req.Status;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<int> AssignSuppliersAsync(
        Guid rfqId,
        AssignRfqSuppliersRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var supplierIds = request.SupplierIds.Distinct().ToHashSet();
        if (supplierIds.Count == 0)
            throw new ArgumentException("至少需指定一間供應商。");

        var rfq = await _db.Rfqs
            .Include(item => item.SupplierInvitations)
            .SingleOrDefaultAsync(item => item.Id == rfqId, cancellationToken)
            ?? throw new KeyNotFoundException("RFQ 不存在。");
        if (!string.Equals(rfq.Status, "open", StringComparison.OrdinalIgnoreCase)
            || rfq.Deadline is DateTime deadline && deadline.ToUniversalTime() < DateTime.UtcNow)
            throw new InvalidOperationException("只有收件期間內的開放 RFQ 可以指派供應商。");

        var suppliers = await _db.Suppliers
            .Where(supplier => supplierIds.Contains(supplier.Id))
            .ToListAsync(cancellationToken);
        if (suppliers.Count != supplierIds.Count)
            throw new KeyNotFoundException("一或多間指定供應商不存在。");
        if (suppliers.Any(supplier =>
            !string.Equals(supplier.ApprovalStatus, "APPROVED", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(supplier.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("只能指派給已核准且啟用中的供應商。");

        foreach (var invitation in rfq.SupplierInvitations)
        {
            if (!supplierIds.Contains(invitation.SupplierId))
            {
                invitation.Status = "Withdrawn";
                continue;
            }

            if (invitation.Status == "Withdrawn")
            {
                invitation.Status = "Invited";
                invitation.InvitedAt = DateTime.UtcNow;
            }
        }

        var existingSupplierIds = rfq.SupplierInvitations
            .Select(invitation => invitation.SupplierId)
            .ToHashSet();
        foreach (var supplierId in supplierIds.Where(id => !existingSupplierIds.Contains(id)))
        {
            rfq.SupplierInvitations.Add(new RfqSupplierInvitation
            {
                RfqId = rfq.Id,
                SupplierId = supplierId,
                InvitedAt = DateTime.UtcNow,
                Status = "Invited"
            });
        }

        _db.AuditLogs.Add(new ErpBackend.Models.System.AuditLog
        {
            UserId = actorUserId == Guid.Empty ? null : actorUserId,
            Action = "RFQ_SUPPLIERS_ASSIGNED",
            EntityType = nameof(Rfq),
            EntityId = rfq.Id,
            TableName = "RfqSupplierInvitations",
            Details = $"SupplierCount={supplierIds.Count}",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return supplierIds.Count;
    }
}
