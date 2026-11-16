using ErpBackend.Data;
using ErpBackend.Models.System;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public static class PermissionCatalog
{
    public sealed record PermissionModuleDefinition(
        string Id,
        string Name,
        string PageName,
        string[] Controllers,
        bool IncludeReverse = false);

    public static readonly IReadOnlyList<PermissionModuleDefinition> Modules =
    [
        new("dashboard", "總覽與監控", "全廠戰情室", ["DashboardController", "WeatherForecastController"]),
        new("kpi", "總覽與監控", "KPI 指標看板", ["KpisController"]),
        new("systemsettings", "主資料與基礎", "系統與權限", ["SystemSettingsController", "UsersController", "RolesController", "PermissionsController", "AuthController"]),
        new("uom", "主資料與基礎", "單位量測", ["UomsController", "ActivityTypesController"]),
        new("engineering", "主資料與基礎", "工藝與 BOM", ["BomsController", "EcnsController", "OperationsController", "RoutingsController", "WorkCentersController", "WorkCenterCostsController"]),
        new("org", "主資料與基礎", "組織架構", ["DepartmentsController"]),
        new("sales", "銷售管理", "銷售與 CRM", ["CustomersController", "QuotesController", "SalesOrdersController", "InvoicesController", "DeliveryOrdersController", "CashReceiptsController", "SalesReportsController", "SalesCreditNotesController"]),
        new("mps", "生產計劃 (MPS)", "MPS 主排程", ["MpsController", "PlanningController"]),
        new("rccp", "生產計劃 (MPS)", "RCCP 粗產能", ["RccpResourcesController"]),
        new("mrp", "物料計畫 (MRP)", "MRP 需求規劃", ["MrpController", "AllocationController"]),
        new("procurement", "採購供應鏈", "採購管理", ["PurchaseOrdersController", "PurchaseRequisitionsController", "GoodsReceiptsController", "RfqsController", "SupplierQuotesController"], IncludeReverse: true),
        new("suppliers", "採購供應鏈", "供應商管理", ["SuppliersController", "SupplierDebitNotesController"]),
        new("supplierportal", "採購供應鏈", "供應商入口", ["SupplierAuthController", "SupplierAsnController"]),
        new("inventory", "庫存物流", "庫存管理", ["InventoryController", "BinLocationsController", "InventoryTransfersController", "SkusController", "WarehousesController", "SubstituteController"]),
        new("barcode", "庫存物流", "條碼系統", ["BarcodeController"]),
        new("manufacturing", "生產執行", "生產工單", ["WorkOrdersController", "MaterialIssuesController", "CompletionOrdersController", "ProductionReceiptsController", "WipReportsController", "AgentWipController"]),
        new("aps", "生產執行", "APS 細排程", ["ApsController", "ApsIntegrationController"]),
        new("tpm", "生產執行", "設備維護 TPM", ["EquipmentController"]),
        new("finance", "財務與預算", "財務會計", ["FinanceController", "AccountsPayableController", "CurrenciesController", "CostPostingsController"]),
        new("budget", "財務與預算", "預算規劃", ["BudgetsController"]),
        new("actual_costing", "財務與預算", "實際成本結轉", ["CostElementsController", "WorkOrdersCostingController"]),
        new("costcenter", "財務與預算", "成本中心", ["CostCentersController"]),
        new("hr", "人力資源", "人事薪資", ["EmployeesController", "AttendanceController", "AttendanceRecordsController", "LeaveRequestsController", "PayrollController", "JobGradesController", "JobTitlesController", "EmployeeEssController"]),
        new("quality", "品質管理", "品質與稽核", ["QualityController", "ProcessInspectionsController", "FqcWorkflowController", "IqcReportsController", "SupplierComplaintsController"]),
        new("ai", "AI 智能應用", "AI 監控中心", ["AiController", "LlmController", "DigitalController"]),
        new("agentflows", "AI 智能應用", "智慧代理流程", ["AgentController"]),
        new("aidecision", "AI 智能應用", "AI 決策助手", ["AiDecisionController"]),
        new("aischeduling", "AI 智能應用", "AI 智能排班", ["AiSchedulingController"]),
        new("assets", "資產管理", "資產管理", ["AssetsController"]),
        new("rd", "研發管理", "研發專案", ["RndController"]),
        new("esg", "永續管理", "碳盤查與 ESG", ["CarbonController", "EsgController"]),
        new("collaboration", "協同作業", "跨部門協作", ["CollaborationController", "ApprovalWorkflowController"]),
        new("calendar", "協同作業", "行事曆", ["CalendarController"]),
        new("ediexport", "數位整合", "EDI 匯出", ["EdiController"])
    ];

    public static readonly IReadOnlyList<(string Code, string Name)> Actions =
    [
        ("view", "檢閱"),
        ("create", "新增"),
        ("edit", "編輯"),
        ("delete", "刪除"),
        ("approve", "審核"),
        ("export", "匯出"),
        ("execute", "執行")
    ];

    private static readonly IReadOnlyDictionary<string, string> ControllerModules =
        Modules.SelectMany(module => module.Controllers.Select(controller => (controller, module.Id)))
            .ToDictionary(item => item.controller, item => item.Id, StringComparer.OrdinalIgnoreCase);

    public static bool TryGetModule(string controllerName, out string moduleId)
        => ControllerModules.TryGetValue(controllerName, out moduleId!);

    public static string GetActionCode(string moduleId, string action)
        => $"{moduleId}:{action}";

    public static string ResolveAction(string method, string actionName)
    {
        var name = actionName.ToLowerInvariant();
        if (ContainsAny(name, "export", "download"))
            return "export";
        if (ContainsAny(name, "approve", "reject", "review", "authorize"))
            return "approve";
        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            return "view";
        if (method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
            return "delete";
        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            || method.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
            return "edit";
        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            if (ContainsAny(name, "create", "add", "register"))
                return "create";
            if (ContainsAny(
                    name,
                    "execute", "run", "release", "issue", "receive", "submit", "complete",
                    "close", "reopen", "post", "revert", "reverse", "recalculate", "generate",
                    "refresh", "import", "sync", "schedule", "dispatch", "assign", "confirm",
                    "cancel", "adjust", "depreciate", "dispose", "transfer", "stocktake",
                    "calculate", "process", "publish", "unlock", "reset"))
                return "execute";
            return "create";
        }

        return "execute";
    }

    public static async Task SeedAsync(ErpGeneralDbContext db, CancellationToken cancellationToken = default)
    {
        var existing = await db.Permissions
            .AsNoTracking()
            .ToDictionaryAsync(
                permission => permission.PermissionCode,
                permission => permission.Id,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);
        var sortOrder = 0;
        var additions = new List<Permission>();

        foreach (var module in Modules)
        {
            var moduleId = GetOrAdd(
                existing, additions, module.Id, module.Name, "module", "module", null, sortOrder++);
            var pageId = GetOrAdd(
                existing, additions, $"{module.Id}:page", module.PageName, "view", "page", moduleId, sortOrder++);

            var actions = module.IncludeReverse ? Actions.Append(("reverse", "沖銷")) : Actions;
            foreach (var (code, name) in actions)
            {
                GetOrAdd(
                    existing,
                    additions,
                    GetActionCode(module.Id, code),
                    name,
                    code,
                    "action",
                    pageId,
                    sortOrder++);
            }
        }

        if (additions.Count > 0)
        {
            db.Permissions.AddRange(additions);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static Guid GetOrAdd(
        IDictionary<string, Guid> existing,
        ICollection<Permission> additions,
        string code,
        string name,
        string actionType,
        string permissionType,
        Guid? parentId,
        int sortOrder)
    {
        if (existing.TryGetValue(code, out var existingId))
            return existingId;

        var created = new Permission
        {
            Id = Guid.NewGuid(),
            PermissionCode = code,
            ModuleName = name,
            ActionType = actionType,
            PermissionType = permissionType,
            ParentId = parentId,
            SortOrder = sortOrder,
            IsActive = true
        };
        additions.Add(created);
        existing.Add(code, created.Id);
        return created.Id;
    }

    private static bool ContainsAny(string value, params string[] fragments)
        => fragments.Any(value.Contains);
}
