using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ErpBackend.Data;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;

namespace ErpBackend.Services;

public interface IPlannedOrderEngine
{
    Task<MrpPlannedOrder> GeneratePlannedOrderAsync(MrpRequirement requirement);
    Task ExplodeBomAsync(MrpPlannedOrder parentOrder, Guid runId, int depth, HashSet<Guid> visitedSkus);
    decimal CalculateOrderQuantity(decimal netRequirement, Sku? sku);
    Task<int> CalculateProductionLeadTimeAsync(Guid skuId, decimal qty);
}

public class PlannedOrderEngine : IPlannedOrderEngine
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<PlannedOrderEngine> _logger;

    public PlannedOrderEngine(ErpGeneralDbContext db, ILogger<PlannedOrderEngine> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<MrpPlannedOrder> GeneratePlannedOrderAsync(MrpRequirement requirement)
    {
        var sku = await _db.Skus.AsNoTracking().FirstOrDefaultAsync(s => s.Id == requirement.SkuId);
        var orderType = sku?.MakeOrBuy == "buy" ? "purchase_order" : "work_order";

        // 計算提前期
        var leadTimeDays = orderType == "purchase_order"
            ? (sku?.SupplierLeadTime ?? sku?.LeadTimeDays ?? 7)
            : await CalculateProductionLeadTimeAsync(requirement.SkuId, requirement.NetRequirement);

        var plannedEndDate = requirement.RequiredDate;
        var plannedStartDate = plannedEndDate.AddDays(-leadTimeDays);

        // 計算經濟批量
        var orderQty = CalculateOrderQuantity(requirement.NetRequirement, sku);

        return new MrpPlannedOrder
        {
            SkuId = requirement.SkuId,
            OrderType = orderType,
            OrderQty = orderQty,
            PlannedStartDate = plannedStartDate,
            PlannedEndDate = plannedEndDate,
            SourceRequirementId = requirement.Id,
            Status = "suggested",
            Notes = $"由MRP運行自動生成，需求日期：{requirement.RequiredDate:yyyy/MM/dd}"
        };
    }

    /// <summary>
    /// TICKET-002: BOM 展開邏輯，包含循環偵測與深度限制
    /// </summary>
    public async Task ExplodeBomAsync(MrpPlannedOrder parentOrder, Guid runId, int depth, HashSet<Guid> visitedSkus)
    {
        const int MAX_DEPTH = 20; // 支援 20 層 BOM，超過則中止以保安全

        if (depth > MAX_DEPTH)
        {
            _logger.LogError($"BOM 展開超過最大深度 {MAX_DEPTH}，可能是資料異常。父料號 ID: {parentOrder.SkuId}");
            return;
        }

        var bom = await _db.Boms.AsNoTracking()
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.ProductSkuId == parentOrder.SkuId && b.Status == "active");

        if (bom == null || !bom.Items.Any()) return;

        foreach (var item in bom.Items)
        {
            // 循環偵測
            if (visitedSkus.Contains(item.ChildSkuId))
            {
                _logger.LogCritical($"偵測到 BOM 循環依賴！料號 {item.ChildSkuId} 已在路徑中。");
                continue;
            }

            var childRequirement = new MrpRequirement
            {
                MrpRunId = runId,
                SkuId = item.ChildSkuId,
                RequirementType = "dependent",
                SourceDocumentId = parentOrder.Id,
                SourceDocumentNumber = $"PO-{parentOrder.SkuId.ToString().Substring(0, 8)}", // 簡化關聯編號
                RequiredDate = parentOrder.PlannedStartDate, // 子料需要在父料開工前備齊
                RequiredQty = item.Quantity * parentOrder.OrderQty,
                ActionCode = "none",
                Notes = $"來自父工單建議的需求 (層級: {depth + 1})"
            };

            _db.MrpRequirements.Add(childRequirement);

            // 如果需要深度展開（實務上 MRP 通常按 LLC 處理，故此處只需存下需求，LLC 會負責下一層的計算）
            // 但若要即時檢查或進行特定模擬，可遞迴。在 LLC-based MRP 中，此處 Add 完畢即可。
        }
    }

    public decimal CalculateOrderQuantity(decimal netRequirement, Sku? sku)
    {
        if (sku == null) return netRequirement;

        var moq = sku.Moq;
        if (moq > 0 && netRequirement < moq)
        {
            var multiples = Math.Ceiling(netRequirement / moq);
            return moq * multiples;
        }

        return netRequirement;
    }

    public async Task<int> CalculateProductionLeadTimeAsync(Guid skuId, decimal qty)
    {
        var routing = await _db.Routings.AsNoTracking()
            .Include(r => r.Operations)
            .ThenInclude(ro => ro.Operation)
            .FirstOrDefaultAsync(r => r.ProductSkuId == skuId && r.IsActive);

        if (routing == null) return 7;

        decimal totalHours = 0;
        foreach (var op in routing.Operations.OrderBy(o => o.OperationSeq))
        {
            var setupTime = op.Operation?.SetupTime ?? 0;
            var runTime = (op.Operation?.RunTime ?? 0) * qty;
            totalHours += setupTime + runTime;
        }

        return (int)Math.Ceiling(totalHours / 8);
    }
}
