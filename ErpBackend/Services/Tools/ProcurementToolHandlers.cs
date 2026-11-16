using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;

namespace ErpBackend.Services;

public sealed class PurchaseOrderSearchHandler : IToolHandler
{
    private readonly IErpIntegrationService _erpIntegration;

    public PurchaseOrderSearchHandler(IErpIntegrationService erpIntegration) => _erpIntegration = erpIntegration;

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters) =>
        _erpIntegration.SearchPurchaseOrdersAsync(
            parameters.GetValueOrDefault("po_number")?.ToString(),
            parameters.GetValueOrDefault("status")?.ToString());
}

public sealed class PurchaseRequisitionSearchHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public PurchaseRequisitionSearchHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var prNumber = parameters.GetValueOrDefault("pr_number")?.ToString();
        var status = parameters.GetValueOrDefault("status")?.ToString();

        var query = _db.PurchaseRequisitions.AsQueryable();

        if (!string.IsNullOrEmpty(prNumber))
            query = query.Where(p => p.PrNumber.Contains(prNumber));
        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        var results = await query.Take(50).Select(p => new
        {
            p.PrNumber,
            p.Status
        }).ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class SupplierSearchHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public SupplierSearchHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var supplierCode = parameters.GetValueOrDefault("supplier_code")?.ToString();
        var name = parameters.GetValueOrDefault("name")?.ToString();

        var query = _db.Suppliers.AsQueryable();

        if (!string.IsNullOrEmpty(supplierCode))
            query = query.Where(s => s.SupplierCode.Contains(supplierCode));
        if (!string.IsNullOrEmpty(name))
            query = query.Where(s => s.SupplierName.Contains(name));

        var results = await query.Take(50).Select(s => new
        {
            s.SupplierCode,
            s.SupplierName,
            s.ContactPerson
        }).ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class CreatePurchaseRequisitionHandler : IToolHandler
{
    private readonly IErpIntegrationService _erpIntegration;

    public CreatePurchaseRequisitionHandler(IErpIntegrationService erpIntegration) => _erpIntegration = erpIntegration;

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters) =>
        _erpIntegration.CreatePurchaseRequisitionAsync(
            parameters.GetValueOrDefault("supplier_id")?.ToString() ?? "",
            new List<PrItemDto>());
}

public sealed class GoodsReceiptCreateHandler : IToolHandler
{
    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        await Task.CompletedTask;
        var poId = parameters.GetValueOrDefault("po_id")?.ToString();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { GrId = Guid.NewGuid(), GrNumber = $"GR-{DateTime.Now:yyyyMMddHHmmss}", Status = "created" }) };
    }
}

public sealed class SupplierRatingHandler : IToolHandler
{
    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        await Task.CompletedTask;
        var supplierId = parameters.GetValueOrDefault("supplier_id")?.ToString();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { SupplierId = supplierId ?? "test", Rating = 85.5m, Grade = "A", BasedOn = "品質、交期、價格" }) };
    }
}
