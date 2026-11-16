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

    public sealed record PermissionResourceDefinition(
        string ModuleId,
        string Id,
        string Name,
        string Controller,
        string[]? ActionNames = null);

    public static readonly IReadOnlyList<PermissionModuleDefinition> Modules =
    [
        new("dashboard", "總覽與監控", "全廠戰情室", ["DashboardController", "WeatherForecastController"]),
        new("kpi", "總覽與監控", "KPI 指標看板", ["KpisController"]),
        new("systemsettings", "主資料與基礎", "系統與權限", ["SystemSettingsController", "UsersController", "RolesController", "PermissionsController", "AuthController"]),
        new("uom", "主資料與基礎", "單位量測", ["UomsController", "ActivityTypesController"]),
        new("engineering", "主資料與基礎", "工藝與 BOM", []),
        new("productengineering", "主資料與基礎", "BOM 管理", ["BomsController"]),
        new("processengineering", "主資料與基礎", "製程工程", ["EcnsController", "OperationsController", "RoutingsController", "WorkCentersController", "WorkCenterCostsController"]),
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
        new("finance", "財務與預算", "財務會計", ["FinanceController", "AccountsPayableController", "CurrenciesController", "CostPostingsController", "SupplierApSubmissionsController"]),
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

    private static readonly IReadOnlyDictionary<string, string> ResourceNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DashboardController"] = "全廠戰情室",
            ["KpisController"] = "KPI 指標",
            ["SystemSettingsController"] = "系統設定（整體功能）",
            ["UsersController"] = "使用者帳號",
            ["RolesController"] = "角色",
            ["PermissionsController"] = "權限",
            ["AuthController"] = "登入與帳號",
            ["UomsController"] = "單位",
            ["ActivityTypesController"] = "作業類型",
            ["BomsController"] = "產品 BOM",
            ["EcnsController"] = "工程變更單",
            ["OperationsController"] = "工序",
            ["RoutingsController"] = "工藝路線",
            ["WorkCentersController"] = "工作中心",
            ["WorkCenterCostsController"] = "工作中心成本",
            ["DepartmentsController"] = "部門",
            ["CustomersController"] = "客戶",
            ["QuotesController"] = "報價單",
            ["SalesOrdersController"] = "銷售訂單",
            ["InvoicesController"] = "發票",
            ["DeliveryOrdersController"] = "出貨單",
            ["CashReceiptsController"] = "收款",
            ["SalesReportsController"] = "銷售報表",
            ["SalesCreditNotesController"] = "銷貨折讓",
            ["MpsController"] = "主生產排程",
            ["PlanningController"] = "規劃作業",
            ["RccpResourcesController"] = "粗產能資源",
            ["MrpController"] = "物料需求規劃",
            ["AllocationController"] = "物料分配",
            ["PurchaseOrdersController"] = "採購單",
            ["PurchaseRequisitionsController"] = "請購單",
            ["GoodsReceiptsController"] = "收貨單",
            ["RfqsController"] = "詢價單",
            ["SupplierQuotesController"] = "供應商報價",
            ["SuppliersController"] = "供應商",
            ["SupplierDebitNotesController"] = "供應商折讓",
            ["SupplierAuthController"] = "供應商帳號",
            ["SupplierAsnController"] = "供應商出貨通知",
            ["InventoryController"] = "庫存（整體功能）",
            ["BinLocationsController"] = "儲位",
            ["InventoryTransfersController"] = "庫存調撥",
            ["SkusController"] = "商品／品碼（SKU）",
            ["WarehousesController"] = "倉庫",
            ["SubstituteController"] = "替代料",
            ["BarcodeController"] = "條碼",
            ["WorkOrdersController"] = "生產工單",
            ["MaterialIssuesController"] = "領料單",
            ["CompletionOrdersController"] = "完工單",
            ["ProductionReceiptsController"] = "生產入庫",
            ["WipReportsController"] = "報工",
            ["AgentWipController"] = "代理報工",
            ["ApsController"] = "APS 排程",
            ["ApsIntegrationController"] = "APS 整合",
            ["EquipmentController"] = "設備",
            ["FinanceController"] = "財務（整體功能）",
            ["AccountsPayableController"] = "應付帳款",
            ["CurrenciesController"] = "幣別",
            ["CostPostingsController"] = "成本過帳",
            ["SupplierApSubmissionsController"] = "供應商應付申請",
            ["BudgetsController"] = "預算",
            ["CostElementsController"] = "成本要素",
            ["WorkOrdersCostingController"] = "工單成本",
            ["CostCentersController"] = "成本中心",
            ["EmployeesController"] = "員工",
            ["AttendanceController"] = "出勤",
            ["AttendanceRecordsController"] = "出勤紀錄",
            ["LeaveRequestsController"] = "請假申請",
            ["PayrollController"] = "薪資",
            ["JobGradesController"] = "職等",
            ["JobTitlesController"] = "職稱",
            ["EmployeeEssController"] = "員工自助服務",
            ["QualityController"] = "品質管理",
            ["ProcessInspectionsController"] = "製程檢驗",
            ["FqcWorkflowController"] = "成品檢驗流程",
            ["IqcReportsController"] = "進料檢驗",
            ["SupplierComplaintsController"] = "供應商品質客訴",
            ["AiController"] = "AI 服務",
            ["LlmController"] = "大型語言模型",
            ["DigitalController"] = "數位分身",
            ["AgentController"] = "智慧代理",
            ["AiDecisionController"] = "AI 決策",
            ["AiSchedulingController"] = "AI 排程",
            ["AssetsController"] = "資產",
            ["RndController"] = "研發專案",
            ["CarbonController"] = "碳盤查",
            ["EsgController"] = "ESG",
            ["CollaborationController"] = "協同作業",
            ["ApprovalWorkflowController"] = "核准流程",
            ["CalendarController"] = "行事曆",
            ["EdiController"] = "EDI 匯出"
        };

    private static readonly IReadOnlyList<PermissionResourceDefinition> ControllerResources =
        Modules.SelectMany(module => module.Controllers.Select(controller =>
        {
            var resourceId = controller.EndsWith("Controller", StringComparison.Ordinal)
                ? controller[..^"Controller".Length].ToLowerInvariant()
                : controller.ToLowerInvariant();
            var name = ResourceNames.TryGetValue(controller, out var displayName)
                ? displayName
                : resourceId;
            return new PermissionResourceDefinition(module.Id, resourceId, name, controller);
        })).ToList();

    private static readonly IReadOnlyList<PermissionResourceDefinition> SpecializedResources =
    [
        new("finance", "accounting", "會計與財報", "FinanceController",
        [
            "GetChartOfAccounts", "CreateGLAccount", "GetAccountingMappings", "SetAccountingMapping",
            "GetAccountingPeriods", "CreateAccountingPeriod", "CloseAccountingPeriod",
            "ReverseVoucher", "GetJournalEntries", "GetJournal", "GetGeneralLedger",
            "GetTrialBalance", "GetFinancialStatements", "GetVouchers", "GetVoucherById",
            "CreateVoucher", "UpdateVoucher", "SubmitVoucher", "ApproveVoucher",
            "GetPostingSetups", "SeedAccounts"
        ]),
        new("finance", "treasury", "資金與銀行帳戶", "FinanceController",
        [
            "SeedBankAccounts", "GetLoans", "CreateLoan", "GetBankAccounts",
            "CreateBankAccount", "UpdateBankAccount", "SetBankAccountStatus",
            "SetBankAccountGlMapping", "GetInvestments", "CreateInvestment",
            "GetTreasuryTransfers", "CreateTreasuryTransfer", "GetGuarantees",
            "CreateGuarantee"
        ]),
        new("inventory", "stock", "庫存查詢與異動", "InventoryController",
        [
            "GetSkuBalanceFrontend", "GetSkuBalance", "CheckATP", "GetLowStock",
            "GetValuation", "AdjustBalance", "PostTransaction", "GetTransactions",
            "GetSummary"
        ]),
        new("inventory", "stocktake", "盤點作業", "InventoryController",
        [
            "GetStocktakeBalance", "PostStocktake"
        ]),
        new("sales", "workordertransfer", "銷售訂單轉發工單", "WorkOrdersController",
        [
            "GetSalesOrderWorkOrderTransferCandidates", "CreateWorkOrdersFromSalesOrder"
        ]),
        new("systemsettings", "access", "使用者與權限管理", "SystemSettingsController",
        [
            "GetPermissionTree", "CreatePermission", "UpdatePermission", "DeletePermission",
            "GetUsers", "GetLinkedEmployeeIds", "GetRoles", "CreateRole",
            "UpdateRolePermissions", "DeleteRole", "GetRoleModules"
        ]),
        new("systemsettings", "themes", "主題設定", "SystemSettingsController",
        [
            "GetThemes"
        ])
    ];

    public static readonly IReadOnlyList<PermissionResourceDefinition> Resources =
        ControllerResources.Concat(SpecializedResources).ToList();

    private static readonly IReadOnlyDictionary<string, PermissionResourceDefinition> ControllerResourceLookup =
        ControllerResources.ToDictionary(
            resource => resource.Controller,
            resource => resource,
            StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, PermissionModuleDefinition> ControllerModules =
        Modules.SelectMany(module => module.Controllers.Select(controller => (controller, module)))
            .ToDictionary(item => item.controller, item => item.module, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, PermissionResourceDefinition> ActionResourceLookup =
        SpecializedResources
            .SelectMany(resource => resource.ActionNames!.Select(actionName =>
                new KeyValuePair<string, PermissionResourceDefinition>(
                    GetActionResourceKey(resource.Controller, actionName),
                    resource)))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    public static bool TryGetModule(string controllerName, out string moduleId)
    {
        if (ControllerModules.TryGetValue(controllerName, out var module))
        {
            moduleId = module.Id;
            return true;
        }

        moduleId = string.Empty;
        return false;
    }

    public static bool TryGetResource(string controllerName, out PermissionResourceDefinition resource)
    {
        return ControllerResourceLookup.TryGetValue(controllerName, out resource!);
    }

    public static IReadOnlyList<PermissionResourceDefinition> GetResourcesForAction(
        string controllerName,
        string actionName)
    {
        var resources = new List<PermissionResourceDefinition>();
        if (TryGetResource(controllerName, out var controllerResource))
            resources.Add(controllerResource);

        if (ActionResourceLookup.TryGetValue(GetActionResourceKey(controllerName, actionName), out var specializedResource))
            resources.Add(specializedResource);

        return resources;
    }

    public static string GetActionCode(string moduleId, string action)
        => $"{moduleId}:{action}";

    public static string GetResourcePageCode(PermissionResourceDefinition resource)
        => $"{resource.ModuleId}:{resource.Id}:page";

    public static string GetResourceActionCode(PermissionResourceDefinition resource, string action)
        => $"{resource.ModuleId}:{resource.Id}:{action}";

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
            .ToDictionaryAsync(
                permission => permission.PermissionCode,
                permission => permission,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);
        var sortOrder = 0;
        var additions = new List<Permission>();

        foreach (var module in Modules)
        {
            var moduleId = GetOrAdd(
                existing, additions, module.Id, module.Name, "module", "module", null, sortOrder++,
                $"{module.Name}模組權限群組，供角色授權管理。");
            var pageId = GetOrAdd(
                existing, additions, $"{module.Id}:page", module.PageName, "view", "page", moduleId, sortOrder++,
                $"進入「{module.PageName}」功能頁面並檢視資料。");

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
                    sortOrder++,
                    $"對「{module.PageName}」具備「{name}」權限。");
            }

            foreach (var resource in Resources.Where(item => item.ModuleId == module.Id))
            {
                var resourceDescription = resource.Name.Contains("整體功能", StringComparison.Ordinal)
                    ? $"授予此項會開放「{resource.Controller}」的全部表單與流程，包含下方細分資源。"
                    : $"進入「{resource.Name}」表單並檢視資料。";
                var resourcePageId = GetOrAdd(
                    existing,
                    additions,
                    GetResourcePageCode(resource),
                    resource.Name,
                    "view",
                    "page",
                    pageId,
                    sortOrder++,
                    resourceDescription);

                foreach (var (code, name) in actions)
                {
                    GetOrAdd(
                        existing,
                        additions,
                        GetResourceActionCode(resource, code),
                        name,
                        code,
                        "action",
                        resourcePageId,
                        sortOrder++,
                        $"對「{resource.Name}」具備「{name}」權限。");
                }
            }
        }

        if (additions.Count > 0)
        {
            db.Permissions.AddRange(additions);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static Guid GetOrAdd(
        IDictionary<string, Permission> existing,
        ICollection<Permission> additions,
        string code,
        string name,
        string actionType,
        string permissionType,
        Guid? parentId,
        int sortOrder,
        string description)
    {
        if (existing.TryGetValue(code, out var existingPermission))
        {
            if (string.IsNullOrWhiteSpace(existingPermission.Description))
                existingPermission.Description = description;
            return existingPermission.Id;
        }

        var created = new Permission
        {
            Id = Guid.NewGuid(),
            PermissionCode = code,
            ModuleName = name,
            ActionType = actionType,
            PermissionType = permissionType,
            ParentId = parentId,
            SortOrder = sortOrder,
            IsActive = true,
            Description = description
        };
        additions.Add(created);
        existing.Add(code, created);
        return created.Id;
    }

    private static bool ContainsAny(string value, params string[] fragments)
        => fragments.Any(value.Contains);

    private static string GetActionResourceKey(string controllerName, string actionName)
        => $"{controllerName}:{actionName}";
}
