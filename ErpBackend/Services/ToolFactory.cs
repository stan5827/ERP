using System.Diagnostics;
using ErpBackend.Data;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<ToolFactory> _logger;
    private readonly IMrpEngine _mrpEngine;
    private readonly List<ErpToolDefinition> _tools;
    private readonly Dictionary<string, IToolHandler> _toolHandlers;

    public ToolFactory(ErpGeneralDbContext db, ILogger<ToolFactory> logger, IErpIntegrationService erpIntegration, IMrpEngine mrpEngine)
    {
        _logger = logger;
        _mrpEngine = mrpEngine;
        _tools = InitializeToolDefinitions();
        _toolHandlers = InitializeToolHandlers(db, erpIntegration, mrpEngine);
    }

    private static List<ErpToolDefinition> InitializeToolDefinitions()
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
            new() { Id = "erp_employee_search", Name = "員工搜尋", Module = "hr", Category = "search", Description = "搜尋員工資料", Parameters = new() { "employee_no", "department_id" } },
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

    private static Dictionary<string, IToolHandler> InitializeToolHandlers(ErpGeneralDbContext db, IErpIntegrationService erpIntegration, IMrpEngine mrpEngine)
    {
        return new Dictionary<string, IToolHandler>
        {
            ["erp_inventory_search"] = new InventorySearchHandler(erpIntegration),
            ["erp_low_stock_check"] = new LowStockCheckHandler(db),
            ["erp_po_search"] = new PurchaseOrderSearchHandler(erpIntegration),
            ["erp_pr_search"] = new PurchaseRequisitionSearchHandler(db),
            ["erp_supplier_search"] = new SupplierSearchHandler(db),
            ["erp_so_search"] = new SalesOrderSearchHandler(db),
            ["erp_customer_search"] = new CustomerSearchHandler(db),
            ["erp_bom_search"] = new BomSearchHandler(db),
            ["erp_workorder_search"] = new WorkOrderSearchHandler(erpIntegration),
            ["erp_workcenter_search"] = new WorkCenterSearchHandler(db),
            ["erp_warehouse_search"] = new WarehouseSearchHandler(db),
            ["erp_employee_search"] = new EmployeeSearchHandler(db),
            ["erp_sku_search"] = new SkuSearchHandler(db),
            ["mrp_run"] = new MrpRunHandler(mrpEngine),
            ["calculator"] = new CalculatorHandler(),
            ["erp_wo_create"] = new CreateWorkOrderHandler(erpIntegration),
            ["erp_pr_create"] = new CreatePurchaseRequisitionHandler(erpIntegration),
            ["erp_wo_update"] = new UpdateWorkOrderHandler(erpIntegration),
            ["erp_wo_complete"] = new CompleteWorkOrderHandler(erpIntegration),
            ["erp_inventory_adjust"] = new AdjustInventoryHandler(erpIntegration),
            ["erp_material_issue"] = new IssueMaterialHandler(),
            ["report_inventory"] = new InventoryReportHandler(erpIntegration),
            ["report_production"] = new ProductionReportHandler(),
            ["erp_department_search"] = new DepartmentSearchHandler(db),
            ["erp_employee_search"] = new EmployeeSearchHandler(db),
            ["erp_asset_search"] = new AssetSearchHandler(db),
            ["erp_cost_center_search"] = new CostCenterSearchHandler(db),
            ["erp_iqc_report_search"] = new IqcReportSearchHandler(),
            ["erp_quality_complaint_search"] = new QualityComplaintSearchHandler(),
            ["erp_routing_search"] = new RoutingSearchHandler(db),
            ["erp_operation_search"] = new OperationSearchHandler(db),
            ["erp_warehouse_transfer"] = new WarehouseTransferHandler(),
            ["erp_inventory_count"] = new InventoryCountHandler(),
            ["erp_goods_receipt_create"] = new GoodsReceiptCreateHandler(),
            ["erp_delivery_create"] = new DeliveryOrderCreateHandler(),
            ["erp_shipment_track"] = new ShipmentTrackHandler(),
            ["erp_loan_search"] = new LoanSearchHandler(db),
            ["erp_investment_search"] = new InvestmentSearchHandler(db),
            ["erp_supplier_rating"] = new SupplierRatingHandler(),
            ["erp_customer_credit_check"] = new CustomerCreditCheckHandler(),
            ["erp_bom_cost_rollup"] = new BomCostRollupHandler(db),
            ["erp_production_efficiency"] = new ProductionEfficiencyHandler(),
            ["report_finance"] = new FinanceReportHandler(),
            ["report_quality"] = new QualityReportHandler(),
            ["report_hr"] = new HrReportHandler(),
        };
    }

    public List<ErpToolDefinition> GetAvailableTools()
        => _tools
            .Where(tool => tool.IsEnabled && AgentToolAccessPolicy.IsAllowed(tool.Id, tool.Category))
            .ToList();

    public async Task<ToolResult> ExecuteToolAsync(string toolId, Dictionary<string, object> parameters)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var definition = _tools.FirstOrDefault(tool =>
                string.Equals(tool.Id, toolId, StringComparison.OrdinalIgnoreCase));
            if (definition == null)
            {
                sw.Stop();
                return new ToolResult
                {
                    Success = false,
                    Error = $"Tool '{toolId}' not found",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }

            if (!definition.IsEnabled
                || !AgentToolAccessPolicy.IsAllowed(definition.Id, definition.Category))
            {
                _logger.LogWarning("Denied Agent execution for tool {ToolId}", toolId);
                sw.Stop();
                return new ToolResult
                {
                    Success = false,
                    Error = $"Tool '{toolId}' is not approved for Agent execution",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }

            _logger.LogInformation("Executing tool: {ToolId}", toolId);

            if (_toolHandlers.TryGetValue(toolId, out var handler))
            {
                var result = await handler.ExecuteAsync(parameters);
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
}
