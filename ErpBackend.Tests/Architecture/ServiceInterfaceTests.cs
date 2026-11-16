using System.Reflection;
using Xunit;

namespace ErpBackend.Tests.Architecture;

/// <summary>
/// 服務層介面規則測試
/// 驗證所有服務類別是否遵循命名慣例並實作對應介面。
/// </summary>
public class ServiceInterfaceTests
{
    private static Assembly AppAssembly => typeof(ErpBackend.Data.ErpGeneralDbContext).Assembly;
    private const string ServicesNamespace = "ErpBackend.Services";

    /// <summary>
    /// 過濾編譯器產生的巢狀類別（如 <>c, <MethodName>d__N 等）。
    /// </summary>
    private static IEnumerable<Type> GetRealServiceTypes()
    {
        return AppAssembly.GetTypes()
            .Where(t => t.IsClass
                        && !t.IsAbstract
                        && t.Namespace == ServicesNamespace
                        && !t.IsNested // 排除編譯器產生的巢狀類別
                        && !t.Name.StartsWith("<")
                        && !t.Name.Contains("`"));
    }

    [Fact]
    public void AllServices_ShouldBeInServicesNamespace()
    {
        var services = AppAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Service") && t.Namespace != null)
            .Where(t => !t.IsNested && !t.Name.StartsWith("<") && !t.Name.Contains("`"))
            .ToList();

        var violations = services
            .Where(s => s.Namespace != ServicesNamespace)
            .Select(s => s.FullName)
            .ToList();

        Assert.True(violations.Count == 0,
            $"所有服務類別應位於 {ServicesNamespace}，違規: {string.Join(", ", violations)}");
    }

    [Fact]
    public void ServiceNames_ShouldEndWithService()
    {
        var services = GetRealServiceTypes().ToList();

        // 已知例外：非標準命名的服務類型（Handler、Engine、Factory、Result 等）
        var knownExceptions = new HashSet<string>
        {
            "ApsScheduler", "AuditEntry", "InventoryReservation", "ReservationResult",
            "AccountingPolicyOptions",
            "KeycloakSupplierIdentityStatus", "KeycloakSupplierIdentityClient",
            "KeycloakSupplierIdentityException",
            "LlmFactory", "DemandDataCache", "ScheduledReceipt", "MrpDataReader",
            "MrpEngine", "MrpNettingEngine", "OeeMetrics", "PlannedOrderEngine",
            "ProductionEventHub", "SupplierRatingResult", "SupplierRating", "ToolResult",
            "ErpToolDefinition", "ToolFactory", "RoutingSearchHandler",
            "OperationSearchHandler", "BomCostRollupHandler", "CostCenterSearchHandler",
            "AssetSearchHandler", "LoanSearchHandler", "InvestmentSearchHandler",
            "FinanceReportHandler", "EmployeeSearchHandler", "DepartmentSearchHandler",
            "HrReportHandler", "InventorySearchHandler", "LowStockCheckHandler",
            "BomSearchHandler", "WarehouseSearchHandler", "SkuSearchHandler",
            "AdjustInventoryHandler", "WarehouseTransferHandler", "InventoryCountHandler",
            "InventoryReportHandler", "PurchaseOrderSearchHandler",
            "PurchaseRequisitionSearchHandler", "SupplierSearchHandler",
            "CreatePurchaseRequisitionHandler", "GoodsReceiptCreateHandler",
            "SupplierRatingHandler", "WorkOrderSearchHandler", "WorkCenterSearchHandler",
            "CreateWorkOrderHandler", "UpdateWorkOrderHandler", "CompleteWorkOrderHandler",
            "IssueMaterialHandler", "ProductionReportHandler", "ProductionEfficiencyHandler",
            "IqcReportSearchHandler", "QualityComplaintSearchHandler", "QualityReportHandler",
            "SalesOrderSearchHandler", "CustomerSearchHandler", "CustomerCreditCheckHandler",
            "DeliveryOrderCreateHandler", "ShipmentTrackHandler", "CalculatorHandler",
            "MrpRunHandler", "OpenAiLlmService", "ClaudeLlmService", "LlmMetricsService",
            "LlmFactory", "MrpEngine", "ToolFactory", "ProductionEventHub",
            "InMemoryCacheService", "UomConversionService", "ApprovalWorkflowService",
            "AgentMetricsService", "CalendarReminderService", "StateMachineService",
            "ExchangeRateSyncService", "SupplierRatingSchedulerService",
            "AssetDepreciationSchedulerService"
        };

        var violations = services
            .Where(s => !s.Name.EndsWith("Service"))
            .Where(s => !knownExceptions.Contains(s.Name))
            .Select(s => s.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"服務類別名稱應以 Service 結尾，違規: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Interfaces_ShouldBeInServicesNamespace()
    {
        var interfaces = AppAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.StartsWith("I") && t.Name.EndsWith("Service"))
            .Where(t => !t.IsNested)
            .ToList();

        var violations = interfaces
            .Where(i => i.Namespace != ServicesNamespace)
            .Select(i => i.FullName)
            .ToList();

        Assert.True(violations.Count == 0,
            $"服務介面應位於 {ServicesNamespace}，違規: {string.Join(", ", violations)}");
    }

    [Fact]
    public void ServiceImplementations_ShouldImplementInterfaceByName()
    {
        // 僅檢查名稱以 Service 結尾的類別（DTO 等其他類型不在此規則內）
        var services = GetRealServiceTypes()
            .Where(t => t.Name.EndsWith("Service"))
            .ToList();

        // 已知例外：非標準命名的服務
        var knownExceptions = new HashSet<string>
        {
            "OpenAiLlmService",  // 實作 ILlmService
            "ClaudeLlmService",  // 實作 ILlmService
            "LlmMetricsService", // 實作 IMetricsService (自訂)
            "LlmFactory",        // 工廠
            "MrpEngine",         // 引擎
            "ToolFactory",       // 工廠
            "ProductionEventHub", // Hub
            "InMemoryCacheService", // 實作 ICacheService
            // ARCHITECTURE_REVIEW.md 已記錄的「未實現接口」服務（Phase 4 補齊）
            "UomConversionService",
            "ApprovalWorkflowService",
            "AgentMetricsService",
            "CalendarReminderService",
            "StateMachineService",
            "ExchangeRateSyncService",
            "SupplierRatingSchedulerService",
            "AssetDepreciationSchedulerService"
        };

        var violations = new List<string>();
        foreach (var service in services)
        {
            if (knownExceptions.Contains(service.Name))
                continue;

            var expectedInterface = $"I{service.Name}";
            var implementedInterfaces = service.GetInterfaces().Select(i => i.Name).ToList();

            if (!implementedInterfaces.Contains(expectedInterface))
            {
                violations.Add(service.Name);
            }
        }

        Assert.True(violations.Count == 0,
            $"以下服務未實作對應介面 IXxxService: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Services_ShouldNotDependOnPresentationLayer()
    {
        var violations = DependencyInspector.FindDependencies(
            AppAssembly, "ErpBackend.Services", "ErpBackend.Controllers");

        Assert.True(violations.Count == 0,
            $"服務層不得依賴控制器層，違規: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Services_ShouldUseAsyncNaming()
    {
        var services = GetRealServiceTypes().ToList();

        var suspicious = new List<string>();
        foreach (var svc in services)
        {
            var methods = svc.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.Name.StartsWith("<") && !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"));
            foreach (var method in methods)
            {
                bool isAsync = method.ReturnType == typeof(Task) ||
                               (method.ReturnType.IsGenericType &&
                                method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
                if (isAsync && !method.Name.EndsWith("Async"))
                {
                    suspicious.Add($"{svc.Name}.{method.Name}");
                }
            }
        }

        // 過濾掉已知例外
        var knownExceptions = new HashSet<string>
        {
            "OpenAiLlmService.GetModelName",
            "ClaudeLlmService.GetModelName",
            "SubstituteService.GetAvailableSubstitutes",
            "SubstituteService.FindBestSubstitute",
            "SubstituteService.ValidateSubstitution",
            "SubstituteService.LogSubstitution",
            "SubstituteService.GetSubstitutionHistory",
            "SubstituteService.CalculateCostVariance"
        };

        suspicious = suspicious
            .Where(s => !knownExceptions.Contains(s))
            .Distinct()
            .ToList();

        Assert.True(suspicious.Count == 0,
            $"以下非同步方法未以 Async 結尾: {string.Join(", ", suspicious)}");
    }
}
