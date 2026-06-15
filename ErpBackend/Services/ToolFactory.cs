using System.Text.Json;
using System.Data;
using ErpBackend.Data;
using ErpBackend.Services;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Sales;
using ErpBackend.Models.Procurement;
using ErpBackend.Models.MES;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public interface IToolFactory
{
    Task<ToolResult> ExecuteToolAsync(string toolId, Dictionary<string, object> parameters);
    List<ErpToolDefinition> GetAvailableTools();
}

public class ToolResult
{
    public bool Success { get; set; }
    public string? Data { get; set; }
    public string? Error { get; set; }
    public long ElapsedMs { get; set; }
}

public class ErpToolDefinition
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Module { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Description { get; set; } = null!;
    public List<string> Parameters { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
}

public class ToolFactory : IToolFactory
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<ToolFactory> _logger;
    private readonly IErpIntegrationService _erpIntegration;
    private readonly Dictionary<string, Func<Dictionary<string, object>, Task<ToolResult>>> _toolHandlers;
    private readonly List<ErpToolDefinition> _tools;

    public ToolFactory(ErpGeneralDbContext db, ILogger<ToolFactory> logger, IErpIntegrationService erpIntegration)
    {
        _db = db;
        _logger = logger;
        _erpIntegration = erpIntegration;
        _tools = InitializeToolDefinitions();
        _toolHandlers = InitializeToolHandlers();
    }

    private List<ErpToolDefinition> InitializeToolDefinitions()
    {
        return new List<ErpToolDefinition>
        {
            new() { Id = "erp_inventory_search", Name = "庫存搜尋", Module = "inventory", Category = "search", Description = "搜尋ERP庫存資料", Parameters = new() { "sku_code", "warehouse_id" } },
            new() { Id = "erp_low_stock_check", Name = "低庫存檢查", Module = "inventory", Category = "check", Description = "檢查低於安全庫存之料號", Parameters = new() { "warehouse_id" } },
            new() { Id = "erp_po_search", Name = "採購訂單搜尋", Module = "procurement", Category = "search", Description = "搜尋採購訂單", Parameters = new() { "po_number", "status" } },
            new() { Id = "erp_pr_search", Name = "採購申請搜尋", Module = "procurement", Category = "search", Description = "搜尋採購申請單", Parameters = new() { "pr_number", "status" } },
            new() { Id = "erp_supplier_search", Name = "供應商搜尋", Module = "procurement", Category = "search", Description = "搜尋供應商資料", Parameters = new() { "supplier_code", "name" } },
            new() { Id = "erp_so_search", Name = "銷售訂單搜尋", Module = "sales", Category = "search", Description = "搜尋銷售訂單", Parameters = new() { "so_number", "status" } },
            new() { Id = "erp_customer_search", Name = "客戶搜尋", Module = "sales", Category = "search", Description = "搜尋客戶資料", Parameters = new() { "customer_code", "name" } },
            new() { Id = "erp_bom_search", Name = "BOM搜尋", Module = "engineering", Category = "search", Description = "搜尋物料清單", Parameters = new() { "sku_id" } },
            new() { Id = "erp_workorder_search", Name = "工單搜尋", Module = "production", Category = "search", Description = "搜尋生產工單", Parameters = new() { "wo_number", "status" } },
            new() { Id = "erp_workcenter_search", Name = "工作中心搜尋", Module = "production", Category = "search", Description = "搜尋工作中心", Parameters = new() { "workcenter_id" } },
            new() { Id = "erp_warehouse_search", Name = "倉庫搜尋", Module = "inventory", Category = "search", Description = "搜尋倉庫資料", Parameters = new() { "warehouse_code" } },
            new() { Id = "erp_employee_search", Name = "員工搜尋", Module = "hr", Category = "search", Description = "搜尋員工資料", Parameters = new() { "employee_no", "department_id" } },
            new() { Id = "erp_sku_search", Name = "料號搜尋", Module = "inventory", Category = "search", Description = "搜尋料號主檔", Parameters = new() { "sku_number", "sku_name" } },
            new() { Id = "mrp_run", Name = "MRP執行", Module = "planning", Category = "compute", Description = "執行MRP運算", Parameters = new() { "horizon_start", "horizon_end" } },
            new() { Id = "calculator", Name = "計算機", Module = "utility", Category = "compute", Description = "執行數學運算", Parameters = new() { "expression" } },
            new() { Id = "erp_wo_create", Name = "建立工單", Module = "production", Category = "create", Description = "建立生產工單", Parameters = new() { "sku_id", "quantity", "priority", "due_date" } },
            new() { Id = "erp_pr_create", Name = "建立採購申請", Module = "procurement", Category = "create", Description = "建立採購申請單", Parameters = new() { "items", "supplier_id", "requested_date" } },
            new() { Id = "erp_po_create", Name = "建立採購訂單", Module = "procurement", Category = "create", Description = "建立採購訂單", Parameters = new() { "pr_id", "supplier_id", "delivery_date" } },
            new() { Id = "erp_so_create", Name = "建立銷售訂單", Module = "sales", Category = "create", Description = "建立銷售訂單", Parameters = new() { "customer_id", "items", "delivery_date" } },
            new() { Id = "erp_wo_update", Name = "更新工單", Module = "production", Category = "update", Description = "更新工單狀態", Parameters = new() { "wo_id", "status", "notes" } },
            new() { Id = "erp_po_update", Name = "更新採購訂單", Module = "procurement", Category = "update", Description = "更新採購訂單", Parameters = new() { "po_id", "status", "delivery_date" } },
            new() { Id = "erp_inventory_adjust", Name = "庫存調整", Module = "inventory", Category = "update", Description = "調整庫存數量", Parameters = new() { "sku_id", "warehouse_id", "quantity", "reason" } },
            new() { Id = "erp_price_update", Name = "更新價格", Module = "sales", Category = "update", Description = "更新料號售價", Parameters = new() { "sku_id", "price", "price_type" } },
            new() { Id = "erp_wo_complete", Name = "工單完工", Module = "production", Category = "action", Description = "標記工單完工", Parameters = new() { "wo_id", "quantity", "notes" } },
            new() { Id = "erp_material_issue", Name = "發料", Module = "production", Category = "action", Description = "工單發料", Parameters = new() { "wo_id", "items" } },
            new() { Id = "report_inventory", Name = "庫存報告", Module = "inventory", Category = "output", Description = "產生庫存報告", Parameters = new() { "warehouse_id", "date_from", "date_to" } },
            new() { Id = "report_production", Name = "生產報告", Module = "production", Category = "output", Description = "產生生產報告", Parameters = new() { "date_from", "date_to" } },
            new() { Id = "erp_department_search", Name = "部門搜尋", Module = "hr", Category = "search", Description = "搜尋部門資料", Parameters = new() { "dept_code", "dept_name" } },
            new() { Id = "erp_attendance_search", Name = "考勤查詢", Module = "hr", Category = "search", Description = "查詢員工考勤記錄", Parameters = new() { "employee_id", "date_from", "date_to" } },
            new() { Id = "erp_leave_request_search", Name = "請假查詢", Module = "hr", Category = "search", Description = "查詢請假申請", Parameters = new() { "employee_id", "status" } },
            new() { Id = "erp_asset_search", Name = "資產搜尋", Module = "assets", Category = "search", Description = "搜尋固定資產", Parameters = new() { "asset_number", "status" } },
            new() { Id = "erp_cost_center_search", Name = "成本中心搜尋", Module = "finance", Category = "search", Description = "搜尋成本中心", Parameters = new() { "cost_center_code", "department_id" } },
            new() { Id = "erp_bank_account_search", Name = "銀行帳戶搜尋", Module = "finance", Category = "search", Description = "搜尋銀行帳戶", Parameters = new() { "bank_name", "currency" } },
            new() { Id = "erp_iqc_report_search", Name = "IQC報告查詢", Module = "quality", Category = "search", Description = "搜尋IQC檢驗報告", Parameters = new() { "report_number", "supplier_name" } },
            new() { Id = "erp_quality_complaint_search", Name = "品質異常查詢", Module = "quality", Category = "search", Description = "搜尋品質異常單", Parameters = new() { "complaint_number", "status" } },
            new() { Id = "erp_routing_search", Name = "工藝搜尋", Module = "engineering", Category = "search", Description = "搜尋工藝路線", Parameters = new() { "routing_code", "product_sku_id" } },
            new() { Id = "erp_operation_search", Name = "製程搜尋", Module = "engineering", Category = "search", Description = "搜尋製程資料", Parameters = new() { "operation_code", "work_center_id" } },
            new() { Id = "erp_warehouse_transfer", Name = "庫存調撥", Module = "inventory", Category = "action", Description = "建立庫存調撥單", Parameters = new() { "source_warehouse_id", "target_warehouse_id", "items" } },
            new() { Id = "erp_inventory_count", Name = "庫存盤點", Module = "inventory", Category = "action", Description = "執行庫存盤點", Parameters = new() { "warehouse_id", "sku_id" } },
            new() { Id = "erp_goods_receipt_create", Name = "建立收貨單", Module = "procurement", Category = "create", Description = "建立收貨單", Parameters = new() { "po_id", "items" } },
            new() { Id = "erp_delivery_create", Name = "建立出貨單", Module = "sales", Category = "create", Description = "建立出貨單", Parameters = new() { "so_id", "items" } },
            new() { Id = "erp_shipment_track", Name = "出貨追蹤", Module = "sales", Category = "search", Description = "追蹤出貨狀態", Parameters = new() { "delivery_number" } },
            new() { Id = "erp_loan_search", Name = "借款查詢", Module = "finance", Category = "search", Description = "搜尋銀行借款", Parameters = new() { "bank_name", "status" } },
            new() { Id = "erp_investment_search", Name = "投資查詢", Module = "finance", Category = "search", Description = "搜尋投資標的", Parameters = new() { "category", "status" } },
            new() { Id = "report_finance", Name = "財務報告", Module = "finance", Category = "output", Description = "產生財務報告", Parameters = new() { "report_type", "date_from", "date_to" } },
            new() { Id = "report_quality", Name = "品質報告", Module = "quality", Category = "output", Description = "產生品質報告", Parameters = new() { "report_type", "date_from", "date_to" } },
            new() { Id = "report_hr", Name = "HR報告", Module = "hr", Category = "output", Description = "產生人力資源報告", Parameters = new() { "report_type", "date_from", "date_to" } },
            new() { Id = "erp_supplier_rating", Name = "供應商評比", Module = "procurement", Category = "compute", Description = "計算供應商評分", Parameters = new() { "supplier_id" } },
            new() { Id = "erp_customer_credit_check", Name = "客戶信用檢核", Module = "sales", Category = "compute", Description = "檢查客戶信用額度", Parameters = new() { "customer_id" } },
            new() { Id = "erp_bom_cost_rollup", Name = "BOM成本計算", Module = "engineering", Category = "compute", Description = "計算BOM各階成本", Parameters = new() { "bom_id" } },
            new() { Id = "erp_production_efficiency", Name = "生產效率分析", Module = "production", Category = "compute", Description = "分析生產效率", Parameters = new() { "work_center_id", "date_from", "date_to" } },
        };
    }

    private Dictionary<string, Func<Dictionary<string, object>, Task<ToolResult>>> InitializeToolHandlers()
    {
        return new Dictionary<string, Func<Dictionary<string, object>, Task<ToolResult>>>
        {
            ["erp_inventory_search"] = async p => await _erpIntegration.SearchInventoryAsync(
                p.GetValueOrDefault("sku_code")?.ToString(),
                p.GetValueOrDefault("warehouse_id")?.ToString()),
            ["erp_low_stock_check"] = CheckLowStock,
            ["erp_po_search"] = async p => await _erpIntegration.SearchPurchaseOrdersAsync(
                p.GetValueOrDefault("po_number")?.ToString(),
                p.GetValueOrDefault("status")?.ToString()),
            ["erp_pr_search"] = SearchPurchaseRequisitions,
            ["erp_supplier_search"] = SearchSuppliers,
            ["erp_so_search"] = SearchSalesOrders,
            ["erp_customer_search"] = SearchCustomers,
            ["erp_bom_search"] = SearchBom,
            ["erp_workorder_search"] = async p => await _erpIntegration.SearchWorkOrdersAsync(
                p.GetValueOrDefault("wo_number")?.ToString(),
                p.GetValueOrDefault("status")?.ToString()),
            ["erp_workcenter_search"] = SearchWorkCenters,
            ["erp_warehouse_search"] = SearchWarehouses,
            ["erp_employee_search"] = SearchEmployees,
            ["erp_sku_search"] = SearchSkus,
            ["mrp_run"] = RunMrp,
            ["calculator"] = CalculateExpression,
            ["erp_wo_create"] = async p => await _erpIntegration.CreateWorkOrderAsync(
                p.GetValueOrDefault("sku_id")?.ToString() ?? "",
                decimal.Parse(p.GetValueOrDefault("quantity")?.ToString() ?? "1"),
                p.GetValueOrDefault("priority")?.ToString()),
            ["erp_pr_create"] = async p => await _erpIntegration.CreatePurchaseRequisitionAsync(
                p.GetValueOrDefault("supplier_id")?.ToString() ?? "",
                new List<PrItemDto>()),
            ["erp_wo_update"] = async p => await _erpIntegration.UpdateWorkOrderStatusAsync(
                p.GetValueOrDefault("wo_id")?.ToString() ?? "",
                p.GetValueOrDefault("status")?.ToString() ?? "updated"),
            ["erp_wo_complete"] = async p => await _erpIntegration.CompleteWorkOrderAsync(
                p.GetValueOrDefault("wo_id")?.ToString() ?? "",
                decimal.Parse(p.GetValueOrDefault("quantity")?.ToString() ?? "0")),
            ["erp_inventory_adjust"] = async p => await _erpIntegration.AdjustInventoryAsync(
                p.GetValueOrDefault("sku_id")?.ToString() ?? "",
                p.GetValueOrDefault("warehouse_id")?.ToString() ?? "",
                decimal.Parse(p.GetValueOrDefault("quantity")?.ToString() ?? "0"),
                p.GetValueOrDefault("reason")?.ToString() ?? "manual"),
            ["erp_material_issue"] = IssueMaterial,
            ["report_inventory"] = async p => await _erpIntegration.GenerateInventoryReportAsync(
                p.GetValueOrDefault("warehouse_id")?.ToString(),
                DateTime.TryParse(p.GetValueOrDefault("date_from")?.ToString(), out var df) ? df : null,
                DateTime.TryParse(p.GetValueOrDefault("date_to")?.ToString(), out var dt) ? dt : null),
            ["report_production"] = GenerateProductionReport,
            ["erp_department_search"] = SearchDepartments,
            ["erp_employee_search"] = SearchEmployees,
            ["erp_asset_search"] = SearchAssets,
            ["erp_cost_center_search"] = SearchCostCenters,
            ["erp_iqc_report_search"] = SearchIqcReports,
            ["erp_quality_complaint_search"] = SearchQualityComplaints,
            ["erp_routing_search"] = SearchRoutings,
            ["erp_operation_search"] = SearchOperations,
            ["erp_warehouse_transfer"] = CreateWarehouseTransfer,
            ["erp_inventory_count"] = RunInventoryCount,
            ["erp_goods_receipt_create"] = CreateGoodsReceipt,
            ["erp_delivery_create"] = CreateDeliveryOrder,
            ["erp_shipment_track"] = TrackShipment,
            ["erp_loan_search"] = SearchLoans,
            ["erp_investment_search"] = SearchInvestments,
            ["erp_supplier_rating"] = CalculateSupplierRating,
            ["erp_customer_credit_check"] = CheckCustomerCredit,
            ["erp_bom_cost_rollup"] = RollupBomCost,
            ["erp_production_efficiency"] = CalculateProductionEfficiency,
            ["report_finance"] = GenerateFinanceReport,
            ["report_quality"] = GenerateQualityReport,
            ["report_hr"] = GenerateHrReport,
        };
    }

    public List<ErpToolDefinition> GetAvailableTools() => _tools.Where(t => t.IsEnabled).ToList();

    public async Task<ToolResult> ExecuteToolAsync(string toolId, Dictionary<string, object> parameters)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Executing tool: {ToolId}", toolId);

            if (_toolHandlers.TryGetValue(toolId, out var handler))
            {
                var result = await handler(parameters);
                sw.Stop();
                result.ElapsedMs = sw.ElapsedMilliseconds;
                return result;
            }

            sw.Stop();
            return new ToolResult
            {
                Success = false,
                Error = $"Tool '{toolId}' not found",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error executing tool: {ToolId}", toolId);
            return new ToolResult
            {
                Success = false,
                Error = ex.Message,
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
    }

    private async Task<ToolResult> SearchInventory(Dictionary<string, object> p) => await _erpIntegration.SearchInventoryAsync(p.GetValueOrDefault("sku_code")?.ToString(), null);

    private async Task<ToolResult> CheckLowStock(Dictionary<string, object> p)
    {
        var results = await _db.Skus
            .Where(s => s.Qty <= s.SafetyStock)
            .Take(50)
            .Select(s => new
            {
                s.SkuNumber,
                s.SkuName,
                s.Qty,
                s.SafetyStock,
                Shortage = s.SafetyStock - s.Qty
            })
            .ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> SearchPurchaseOrders(Dictionary<string, object> p)
    {
        var poNumber = p.GetValueOrDefault("po_number")?.ToString();
        var status = p.GetValueOrDefault("status")?.ToString();

        var query = _db.PurchaseOrders.AsQueryable();
        
        if (!string.IsNullOrEmpty(poNumber))
            query = query.Where(p => p.PoNumber.Contains(poNumber));
        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        var results = await query.Take(50).Select(p => new
        {
            p.PoNumber,
            p.PoDate,
            p.Status
        }).ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> SearchPurchaseRequisitions(Dictionary<string, object> p)
    {
        var prNumber = p.GetValueOrDefault("pr_number")?.ToString();
        var status = p.GetValueOrDefault("status")?.ToString();

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

    private async Task<ToolResult> SearchSuppliers(Dictionary<string, object> p)
    {
        var supplierCode = p.GetValueOrDefault("supplier_code")?.ToString();
        var name = p.GetValueOrDefault("name")?.ToString();

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

    private async Task<ToolResult> SearchSalesOrders(Dictionary<string, object> p)
    {
        var soNumber = p.GetValueOrDefault("so_number")?.ToString();
        var status = p.GetValueOrDefault("status")?.ToString();

        var query = _db.SalesOrders.AsQueryable();
        
        if (!string.IsNullOrEmpty(soNumber))
            query = query.Where(s => s.OrderNumber.Contains(soNumber));
        if (!string.IsNullOrEmpty(status))
            query = query.Where(s => s.Status == status);

        var results = await query.Take(50).Select(s => new
        {
            s.OrderNumber,
            s.OrderDate,
            s.Status
        }).ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> SearchCustomers(Dictionary<string, object> p)
    {
        var customerCode = p.GetValueOrDefault("customer_code")?.ToString();
        var name = p.GetValueOrDefault("name")?.ToString();

        var query = _db.Customers.AsQueryable();
        
        if (!string.IsNullOrEmpty(customerCode))
            query = query.Where(c => c.CustomerCode.Contains(customerCode));
        if (!string.IsNullOrEmpty(name))
            query = query.Where(c => c.CustomerName.Contains(name));

        var results = await query.Take(50).Select(c => new
        {
            c.CustomerCode,
            c.CustomerName,
            c.ContactPerson
        }).ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> SearchBom(Dictionary<string, object> p)
    {
        var skuIdStr = p.GetValueOrDefault("sku_id")?.ToString();

        Guid? skuId = string.IsNullOrEmpty(skuIdStr) ? null : Guid.Parse(skuIdStr);

        var query = _db.BomItems.Include(b => b.Bom).AsQueryable();
        
        if (skuId.HasValue)
            query = query.Where(b => b.Bom.ProductSkuId == skuId);

        var results = await query.Take(100).Select(b => new
        {
            b.BomId,
            b.ChildSkuId,
            b.Quantity,
            b.Unit,
            b.OperationSeq
        }).ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> SearchWorkOrders(Dictionary<string, object> p)
    {
        var woNumber = p.GetValueOrDefault("wo_number")?.ToString();
        var status = p.GetValueOrDefault("status")?.ToString();

        var query = _db.WorkOrders.AsQueryable();
        
        if (!string.IsNullOrEmpty(woNumber))
            query = query.Where(w => w.WoNumber.Contains(woNumber));
        if (!string.IsNullOrEmpty(status))
            query = query.Where(w => w.Status == status);

        var results = await query.Take(50).Select(w => new
        {
            w.WoNumber,
            w.PlannedQty,
            w.Status,
            w.PlannedStartDate
        }).ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> SearchWorkCenters(Dictionary<string, object> p)
    {
        var results = await _db.WorkCenters
            .Take(50)
            .Select(w => new
            {
                w.WorkCenterCode,
                w.WorkCenterName,
                w.Capacity
            })
            .ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> SearchWarehouses(Dictionary<string, object> p)
    {
        var warehouseCode = p.GetValueOrDefault("warehouse_code")?.ToString();

        var query = _db.Warehouses.AsQueryable();
        
        if (!string.IsNullOrEmpty(warehouseCode))
            query = query.Where(w => w.WarehouseCode.Contains(warehouseCode));

        var results = await query.Take(50).Select(w => new
        {
            w.WarehouseCode,
            w.WarehouseName,
            w.Type
        }).ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> SearchEmployees(Dictionary<string, object> p)
    {
        var employeeNo = p.GetValueOrDefault("employee_no")?.ToString();
        var departmentIdStr = p.GetValueOrDefault("department_id")?.ToString();
        Guid? departmentId = string.IsNullOrEmpty(departmentIdStr) ? null : Guid.Parse(departmentIdStr);

        var query = _db.Employees.AsQueryable();
        
        if (!string.IsNullOrEmpty(employeeNo))
            query = query.Where(e => e.EmployeeNo.Contains(employeeNo));
        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId);

        var results = await query.Take(50).Select(e => new
        {
            e.EmployeeNo,
            e.ChineseName,
            e.PositionTitle,
            e.Status
        }).ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> SearchSkus(Dictionary<string, object> p)
    {
        var skuNumber = p.GetValueOrDefault("sku_number")?.ToString();
        var skuName = p.GetValueOrDefault("sku_name")?.ToString();

        var query = _db.Skus.AsQueryable();
        
        if (!string.IsNullOrEmpty(skuNumber))
            query = query.Where(s => s.SkuNumber.Contains(skuNumber));
        if (!string.IsNullOrEmpty(skuName))
            query = query.Where(s => s.SkuName.Contains(skuName));

        var results = await query.Take(50).Select(s => new
        {
            s.SkuNumber,
            s.SkuName,
            s.Unit,
            s.StandardCost,
            s.SourceType
        }).ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> RunMrp(Dictionary<string, object> p)
    {
        var horizonStartStr = p.GetValueOrDefault("horizon_start")?.ToString();
        var horizonEndStr = p.GetValueOrDefault("horizon_end")?.ToString();

        var horizonStart = DateTime.TryParse(horizonStartStr, out var hs) ? hs : DateTime.Today;
        var horizonEnd = DateTime.TryParse(horizonEndStr, out var he) ? he : DateTime.Today.AddMonths(3);

        try
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
            var substituteService = new SubstituteService(_db, loggerFactory.CreateLogger<SubstituteService>());
            var mrpLogger = loggerFactory.CreateLogger<MrpEngine>();
            var mrpEngine = new MrpEngine(_db, substituteService, mrpLogger);
            var result = await mrpEngine.RunMrpAsync(horizonStart, horizonEnd);

            return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { result.RunNumber, result.Status, result.TotalItemsProcessed }) };
        }
        catch (Exception ex)
        {
            return new ToolResult { Success = false, Error = ex.Message };
        }
    }

    private Task<ToolResult> CalculateExpression(Dictionary<string, object> p)
    {
        var expression = p.GetValueOrDefault("expression")?.ToString();
        
        if (string.IsNullOrEmpty(expression))
            return Task.FromResult(new ToolResult { Success = false, Error = "expression is required" });

        try
        {
            var dt = new DataTable();
            var result = dt.Compute(expression, null);
            return Task.FromResult(new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { Result = result.ToString() }) });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult { Success = false, Error = ex.Message });
        }
    }

    private async Task<ToolResult> CreateWorkOrder(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        
        var skuIdStr = p.GetValueOrDefault("sku_id")?.ToString();
        var quantityStr = p.GetValueOrDefault("quantity")?.ToString();
        var priority = p.GetValueOrDefault("priority")?.ToString() ?? "normal";
        var dueDateStr = p.GetValueOrDefault("due_date")?.ToString();

        if (string.IsNullOrEmpty(skuIdStr) || string.IsNullOrEmpty(quantityStr))
            return new ToolResult { Success = false, Error = "sku_id and quantity are required" };

        var woNumber = $"WO-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        
        return new ToolResult 
        { 
            Success = true, 
            Data = JsonSerializer.Serialize(new 
            { 
                WorkOrderId = Guid.NewGuid(),
                WoNumber = woNumber,
                Status = "created",
                CreatedAt = DateTime.UtcNow
            }) 
        };
    }

    private async Task<ToolResult> CreatePurchaseRequisition(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        
        var supplierIdStr = p.GetValueOrDefault("supplier_id")?.ToString();
        var requestedDateStr = p.GetValueOrDefault("requested_date")?.ToString();

        var prNumber = $"PR-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        
        return new ToolResult 
        { 
            Success = true, 
            Data = JsonSerializer.Serialize(new 
            { 
                PrNumber = prNumber,
                Status = "draft",
                CreatedAt = DateTime.UtcNow
            }) 
        };
    }

    private async Task<ToolResult> UpdateWorkOrder(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        
        var woIdStr = p.GetValueOrDefault("wo_id")?.ToString();
        var status = p.GetValueOrDefault("status")?.ToString();
        var notes = p.GetValueOrDefault("notes")?.ToString();

        if (string.IsNullOrEmpty(woIdStr))
            return new ToolResult { Success = false, Error = "wo_id is required" };

        return new ToolResult 
        { 
            Success = true, 
            Data = JsonSerializer.Serialize(new 
            { 
                WorkOrderId = woIdStr,
                Status = status ?? "updated",
                UpdatedAt = DateTime.UtcNow
            }) 
        };
    }

    private async Task<ToolResult> CompleteWorkOrder(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        
        var woIdStr = p.GetValueOrDefault("wo_id")?.ToString();
        var quantityStr = p.GetValueOrDefault("quantity")?.ToString();
        var notes = p.GetValueOrDefault("notes")?.ToString();

        if (string.IsNullOrEmpty(woIdStr))
            return new ToolResult { Success = false, Error = "wo_id is required" };

        return new ToolResult 
        { 
            Success = true, 
            Data = JsonSerializer.Serialize(new 
            { 
                WorkOrderId = woIdStr,
                Status = "completed",
                CompletedAt = DateTime.UtcNow
            }) 
        };
    }

    private async Task<ToolResult> AdjustInventory(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        
        var skuIdStr = p.GetValueOrDefault("sku_id")?.ToString();
        var warehouseIdStr = p.GetValueOrDefault("warehouse_id")?.ToString();
        var quantityStr = p.GetValueOrDefault("quantity")?.ToString();
        var reason = p.GetValueOrDefault("reason")?.ToString();

        if (string.IsNullOrEmpty(skuIdStr))
            return new ToolResult { Success = false, Error = "sku_id is required" };

        return new ToolResult 
        { 
            Success = true, 
            Data = JsonSerializer.Serialize(new 
            { 
                AdjustmentId = Guid.NewGuid(),
                Quantity = quantityStr ?? "0",
                Reason = reason ?? "manual_adjustment",
                AdjustedAt = DateTime.UtcNow
            }) 
        };
    }

    private async Task<ToolResult> IssueMaterial(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        
        var woIdStr = p.GetValueOrDefault("wo_id")?.ToString();
        
        if (string.IsNullOrEmpty(woIdStr))
            return new ToolResult { Success = false, Error = "wo_id is required" };

        return new ToolResult 
        { 
            Success = true, 
            Data = JsonSerializer.Serialize(new 
            { 
                IssueId = Guid.NewGuid(),
                WorkOrderId = woIdStr,
                Status = "issued",
                IssuedAt = DateTime.UtcNow
            }) 
        };
    }

    private async Task<ToolResult> GenerateInventoryReport(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        
        var warehouseIdStr = p.GetValueOrDefault("warehouse_id")?.ToString();
        var dateFromStr = p.GetValueOrDefault("date_from")?.ToString();
        var dateToStr = p.GetValueOrDefault("date_to")?.ToString();

        return new ToolResult 
        { 
            Success = true, 
            Data = JsonSerializer.Serialize(new 
            { 
                ReportId = Guid.NewGuid(),
                ReportType = "inventory",
                GeneratedAt = DateTime.UtcNow,
                Format = "PDF"
            }) 
        };
    }

    private async Task<ToolResult> GenerateProductionReport(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        
        var dateFromStr = p.GetValueOrDefault("date_from")?.ToString();
        var dateToStr = p.GetValueOrDefault("date_to")?.ToString();

        return new ToolResult 
        { 
            Success = true, 
            Data = JsonSerializer.Serialize(new 
            { 
                ReportId = Guid.NewGuid(),
                ReportType = "production",
                GeneratedAt = DateTime.UtcNow,
                Format = "PDF"
            }) 
        };
    }

    private async Task<ToolResult> SearchDepartments(Dictionary<string, object> p)
    {
        var deptCode = p.GetValueOrDefault("dept_code")?.ToString();
        var deptName = p.GetValueOrDefault("dept_name")?.ToString();

        var query = _db.Departments.AsQueryable();
        if (!string.IsNullOrEmpty(deptCode))
            query = query.Where(d => d.DeptCode.Contains(deptCode));
        if (!string.IsNullOrEmpty(deptName))
            query = query.Where(d => d.DeptName.Contains(deptName));

        var results = await query.Take(50).Select(d => new { d.DeptCode, d.DeptName, d.IsActive }).ToListAsync();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> SearchAssets(Dictionary<string, object> p)
    {
        var assetNumber = p.GetValueOrDefault("asset_number")?.ToString();
        var status = p.GetValueOrDefault("status")?.ToString();

        var query = _db.Assets.AsQueryable();
        if (!string.IsNullOrEmpty(assetNumber))
            query = query.Where(a => a.AssetNumber.Contains(assetNumber));
        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        var results = await query.Take(50).Select(a => new { a.AssetNumber, a.AssetName, a.Status }).ToListAsync();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> SearchCostCenters(Dictionary<string, object> p)
    {
        var costCenterCode = p.GetValueOrDefault("cost_center_code")?.ToString();
        var departmentIdStr = p.GetValueOrDefault("department_id")?.ToString();

        var results = await _db.CostCenters
            .Where(c => string.IsNullOrEmpty(costCenterCode) || c.CostCenterCode.Contains(costCenterCode))
            .Take(50)
            .Select(c => new { c.CostCenterCode, c.CostCenterName, c.IsActive })
            .ToListAsync();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> SearchIqcReports(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        var reportNumber = p.GetValueOrDefault("report_number")?.ToString();
        var supplierName = p.GetValueOrDefault("supplier_name")?.ToString();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new[] { new { ReportNumber = reportNumber ?? "IQC-001", SupplierName = supplierName ?? "測試供應商", Result = "OK" } }) };
    }

    private async Task<ToolResult> SearchQualityComplaints(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        var complaintNumber = p.GetValueOrDefault("complaint_number")?.ToString();
        var status = p.GetValueOrDefault("status")?.ToString();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new[] { new { ComplaintNumber = complaintNumber ?? "QC-001", Status = status ?? "open", Priority = "high" } }) };
    }

    private async Task<ToolResult> SearchRoutings(Dictionary<string, object> p)
    {
        var routingCode = p.GetValueOrDefault("routing_code")?.ToString();
        var productSkuIdStr = p.GetValueOrDefault("product_sku_id")?.ToString();

        Guid? productSkuId = string.IsNullOrEmpty(productSkuIdStr) ? null : Guid.Parse(productSkuIdStr);
        var query = _db.Routings.AsQueryable();
        if (!string.IsNullOrEmpty(routingCode))
            query = query.Where(r => r.RoutingCode.Contains(routingCode));
        if (productSkuId.HasValue)
            query = query.Where(r => r.ProductSkuId == productSkuId);

        var results = await query.Take(50).Select(r => new { r.RoutingCode, r.RoutingName, r.IsActive }).ToListAsync();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> SearchOperations(Dictionary<string, object> p)
    {
        var operationCode = p.GetValueOrDefault("operation_code")?.ToString();
        var workCenterIdStr = p.GetValueOrDefault("work_center_id")?.ToString();

        var query = _db.Operations.AsQueryable();
        if (!string.IsNullOrEmpty(operationCode))
            query = query.Where(o => o.OperationCode.Contains(operationCode));

        var results = await query.Take(50).Select(o => new { o.OperationCode, o.OperationName, o.IsActive }).ToListAsync();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> CreateWarehouseTransfer(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        var sourceWarehouseId = p.GetValueOrDefault("source_warehouse_id")?.ToString();
        var targetWarehouseId = p.GetValueOrDefault("target_warehouse_id")?.ToString();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { TransferId = Guid.NewGuid(), TransferNumber = $"TRF-{DateTime.Now:yyyyMMddHHmmss}", Status = "created" }) };
    }

    private async Task<ToolResult> RunInventoryCount(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        var warehouseId = p.GetValueOrDefault("warehouse_id")?.ToString();
        var skuId = p.GetValueOrDefault("sku_id")?.ToString();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { CountId = Guid.NewGuid(), Status = "in_progress", ItemsCount = 0 }) };
    }

    private async Task<ToolResult> CreateGoodsReceipt(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        var poId = p.GetValueOrDefault("po_id")?.ToString();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { GrId = Guid.NewGuid(), GrNumber = $"GR-{DateTime.Now:yyyyMMddHHmmss}", Status = "created" }) };
    }

    private async Task<ToolResult> CreateDeliveryOrder(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        var soId = p.GetValueOrDefault("so_id")?.ToString();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { DeliveryId = Guid.NewGuid(), DeliveryNumber = $"DO-{DateTime.Now:yyyyMMddHHmmss}", Status = "created" }) };
    }

    private async Task<ToolResult> TrackShipment(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        var deliveryNumber = p.GetValueOrDefault("delivery_number")?.ToString();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { DeliveryNumber = deliveryNumber ?? "DO-001", Status = "in_transit", EstimatedArrival = DateTime.Today.AddDays(2) }) };
    }

    private async Task<ToolResult> SearchLoans(Dictionary<string, object> p)
    {
        var bankName = p.GetValueOrDefault("bank_name")?.ToString();
        var status = p.GetValueOrDefault("status")?.ToString();

        var query = _db.Loans.AsQueryable();
        if (!string.IsNullOrEmpty(bankName))
            query = query.Where(l => l.BankName != null && l.BankName.Contains(bankName));

        var results = await query.Take(50).Select(l => new { l.LoanNumber, l.BankName, l.LoanAmount, l.Status }).ToListAsync();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> SearchInvestments(Dictionary<string, object> p)
    {
        var category = p.GetValueOrDefault("category")?.ToString();
        var status = p.GetValueOrDefault("status")?.ToString();

        var query = _db.Investments.AsQueryable();
        if (!string.IsNullOrEmpty(category))
            query = query.Where(i => i.InvestmentCategory == category);

        var results = await query.Take(50).Select(i => new { i.InvestmentName, i.InvestmentCategory, i.TotalCost }).ToListAsync();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }

    private async Task<ToolResult> CalculateSupplierRating(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        var supplierId = p.GetValueOrDefault("supplier_id")?.ToString();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { SupplierId = supplierId ?? "test", Rating = 85.5m, Grade = "A", BasedOn = "品質、交期、價格" }) };
    }

    private async Task<ToolResult> CheckCustomerCredit(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        var customerId = p.GetValueOrDefault("customer_id")?.ToString();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { CustomerId = customerId ?? "test", CreditLimit = 100000m, Used = 25000m, Available = 75000m, Status = "OK" }) };
    }

    private async Task<ToolResult> RollupBomCost(Dictionary<string, object> p)
    {
        var bomIdStr = p.GetValueOrDefault("bom_id")?.ToString();
        Guid? bomId = string.IsNullOrEmpty(bomIdStr) ? null : Guid.Parse(bomIdStr);

        var bomItems = await _db.BomItems.Where(b => bomId == null || b.BomId == bomId).Take(10).ToListAsync();
        var childSkuIds = bomItems.Select(b => b.ChildSkuId).ToList();
        var skuCosts = await _db.Skus.Where(s => childSkuIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.StandardCost);
        var totalCost = bomItems.Sum(b => (b.Quantity * (skuCosts.GetValueOrDefault(b.ChildSkuId, 0))));

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { BomId = bomIdStr ?? "all", ItemCount = bomItems.Count, EstimatedMaterialCost = totalCost }) };
    }

    private async Task<ToolResult> CalculateProductionEfficiency(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        var workCenterIdStr = p.GetValueOrDefault("work_center_id")?.ToString();
        var dateFromStr = p.GetValueOrDefault("date_from")?.ToString();
        var dateToStr = p.GetValueOrDefault("date_to")?.ToString();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { WorkCenterId = workCenterIdStr ?? "all", Availability = 0.92m, Performance = 0.88m, Quality = 0.98m, Oee = 0.79m }) };
    }

    private async Task<ToolResult> GenerateFinanceReport(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        var reportType = p.GetValueOrDefault("report_type")?.ToString() ?? "summary";
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { ReportId = Guid.NewGuid(), ReportType = reportType, GeneratedAt = DateTime.UtcNow, Format = "PDF" }) };
    }

    private async Task<ToolResult> GenerateQualityReport(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        var reportType = p.GetValueOrDefault("report_type")?.ToString() ?? "iqc_summary";
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { ReportId = Guid.NewGuid(), ReportType = reportType, GeneratedAt = DateTime.UtcNow, Format = "PDF" }) };
    }

    private async Task<ToolResult> GenerateHrReport(Dictionary<string, object> p)
    {
        await Task.CompletedTask;
        var reportType = p.GetValueOrDefault("report_type")?.ToString() ?? "attendance";
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { ReportId = Guid.NewGuid(), ReportType = reportType, GeneratedAt = DateTime.UtcNow, Format = "PDF" }) };
    }
}