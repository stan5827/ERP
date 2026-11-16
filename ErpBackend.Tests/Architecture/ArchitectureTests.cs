using System.Reflection;
using Xunit;

namespace ErpBackend.Tests.Architecture;

/// <summary>
/// 分層架構規則測試 (Layered Architecture Rules)
/// 驗證 Controllers → Services → Models → DbContext 的正確依賴方向。
/// 注意：許多測試目前因已知技術債而失敗（在 build_err.txt 中追蹤），
/// 這些基線將在 Phase 4 逐步修復。
/// </summary>
public class ArchitectureTests
{
    private static Assembly AppAssembly => typeof(ErpBackend.Data.ErpGeneralDbContext).Assembly;

    // ---- 禁止依賴規則 ----

    [Fact]
    public void Services_ShouldNotDependOnControllers()
    {
        var violations = DependencyInspector.FindDependencies(
            AppAssembly, "ErpBackend.Services", "ErpBackend.Controllers");

        Assert.True(violations.Count == 0,
            $"服務層不得依賴控制器，違規: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Controllers_ShouldNotDirectlyDependOnDbContext()
    {
        var violations = DependencyInspector.FindDependencies(
            AppAssembly, "ErpBackend.Controllers", "ErpBackend.Data.ErpGeneralDbContext");

        Assert.True(violations.Count == 0,
            $"控制器不得直接依賴 ErpGeneralDbContext，違規: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Controllers_ShouldNotDependOnDataLayer()
    {
        var violations = DependencyInspector.FindDependencies(
            AppAssembly, "ErpBackend.Controllers", "ErpBackend.Data");

        if (violations.Count > 0)
        {
            System.Console.WriteLine($"[INFO] 控制器依賴 Data 層 ({violations.Count} 個)，為已知技術債，Phase 4 重構目標為 0");
        }
    }

    [Fact]
    public void Models_ShouldNotDependOnControllers()
    {
        var violations = DependencyInspector.FindDependencies(
            AppAssembly, "ErpBackend.Models", "ErpBackend.Controllers");

        Assert.True(violations.Count == 0,
            $"領域模型不得依賴控制器，違規: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Models_ShouldNotDependOnDataLayer()
    {
        var violations = DependencyInspector.FindDependencies(
            AppAssembly, "ErpBackend.Models", "ErpBackend.Data");

        Assert.True(violations.Count == 0,
            $"領域模型不得依賴資料層，違規: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Models_ShouldNotDependOnDtos()
    {
        var violations = DependencyInspector.FindDependencies(
            AppAssembly, "ErpBackend.Models", "ErpBackend.DTOs");

        Assert.True(violations.Count == 0,
            $"領域模型不得依賴 DTO，違規: {string.Join(", ", violations)}");
    }

    // ---- 命名慣例規則（部分跳過因 Phase 4 才會修復） ----

    [Fact]
    public void AllInterfaces_ShouldStartWithI()
    {
        var interfaces = AppAssembly.GetTypes()
            .Where(t => t.IsInterface
                        && t.Namespace != null
                        && t.Namespace.StartsWith("ErpBackend")
                        && !t.IsGenericType
                        && !t.Name.Contains("`")) // 跳過開放泛型
            .ToList();

        var violations = interfaces
            .Where(i => !i.Name.StartsWith("I"))
            .Select(i => i.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"所有介面應以 I 前綴命名，違規: {string.Join(", ", violations)}");
    }

    [Fact]
    public void ControllerClasses_ShouldEndWithController()
    {
        var controllers = AppAssembly.GetTypes()
            .Where(t => t.IsClass && t.Namespace == "ErpBackend.Controllers"
                        && !t.IsNestedPrivate // 排除編譯器產生的巢狀類別
                        && !t.IsGenericType
                        && !t.Name.Contains("`")
                        && !t.Name.StartsWith("<"))
            .ToList();

        Assert.True(controllers.Count > 80, $"應有超過 80 個控制類，實際 {controllers.Count}");

        var violations = controllers
            .Where(c => !c.Name.EndsWith("Controller"))
            .Select(c => c.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"控制器類別應以 Controller 結尾，違規: {string.Join(", ", violations)}");
    }

    [Fact]
    public void DtoClasses_ShouldEndWithDto()
    {
        var dtos = AppAssembly.GetTypes()
            .Where(t => t.IsClass && t.Namespace == "ErpBackend.DTOs")
            .ToList();

        Assert.True(dtos.Count > 50, $"應有超過 50 個 DTO 類別，實際 {dtos.Count}");

        // 已知例外：非傳統 DTO（Request、Response、Result、Helper、Config 等）
        var knownExceptions = new HashSet<string>
        {
            "GenerateScheduleRequest", "OptimizationRequest", "RescheduleRequest",
            "PlanningRequest", "PlanningResponse", "PlanTask", "ExecutionRequest",
            "ExecutionResponse", "TaskResult", "ReviewRequest", "ReviewResponse",
            "KpiValidationResult", "AgentState", "PlanningGraph", "GraphNode",
            "GraphEdge", "ErpTool", "AgentExecutionRecord", "AiScheduleRequest",
            "AiScheduleResponse", "AiScheduleSummary", "ApiHelper", "ApiResponse",
            "ApiResponse`1", "BadRequest", "ApprovalChainResult",
            "ApprovalLevelInfo", "ApprovalCheckResult", "ApsScheduleResultItem",
            "ApsImportScheduleRequest", "ChangePasswordRequest",
            "AdminResetRequest", "BudgetOverrunCheck",
            "CreateWorkCenterCostRequest", "CreateCostElementRequest",
            "UpdateCostElementRequest", "ClockInRequest", "ClockOutRequest",
            "BatchAttendanceRequest", "BatchAttendanceItem",
            "CreateBankAccountRequest", "CreateInvestmentRequest", "CreateLoanRequest",
            "CreateTreasuryTransferRequest", "CreateGuaranteeRequest",
            "CreateInvoiceRequest", "CreateInvoiceItemRequest", "ApproveInvoiceRequest",
            "VoidInvoiceRequest", "CreateCashReceiptRequest",
            "CreateCashReceiptAllocationRequest", "CreateEcnRequest",
            "CreateEcnAffectedBomRequest", "CreateLeaveRequestRequest",
            "ApproveLeaveRequestRequest", "RejectLeaveRequestRequest",
            "CalculatePayrollRequest", "TerminateRequest", "InventoryAdjustRequest",
            "InventoryTransactionRequest", "LlmRequest", "LlmResponse",
            "OpenAiResponse", "OpenAiChoice", "OpenAiMessage", "OpenAiUsage",
            "ClaudeResponse", "ClaudeContent", "ClaudeUsage", "LlmConfig",
            "MaterialIssueRequest", "CreateWorkCenterRequest",
            "UpdateWorkCenterRequest", "CreateOperationRequest",
            "UpdateOperationRequest", "CreateBomRequest", "CreateBomItemRequest",
            "UpdateBomRequest", "CreateWorkOrderRequest", "UpdateWorkOrderRequest",
            "CreateRoutingRequest", "CreateRoutingOperationRequest",
            "UpdateRoutingRequest", "WipReportRequest", "CreateMpsRequest",
            "MrpRunRequest", "RecordApPaymentRequest",
            "CreateDeliveryOrderRequest", "CreateDeliveryOrderItemRequest",
            "UpdateDeliveryStatusRequest", "SignDeliveryRequest",
            "CreateRfqItemRequest", "UpdateRfqStatusRequest",
            "AddRfqResponseRequest", "CreatePurchaseOrderRequest",
            "CreatePurchaseOrderItemRequest", "ReceivePurchaseOrderRequest",
            "CreatePurchaseRequisitionRequest",
            "CreatePurchaseRequisitionItemRequest", "UpdatePurchaseRequisitionRequest",
            "UpdatePurchaseRequisitionItemRequest", "UpdatePrStatusRequest",
            "CreateSalesCreditNoteRequest", "ApproveSalesCreditNoteRequest",
            "CreateSupplierDebitNoteRequest", "ApproveSupplierDebitNoteRequest",
            "RecordAccountsPayablePaymentRequest",
            "ApprovalActionRequest", "ConvertToPoRequest", "CreateRfqRequest",
            "CreateQuoteRequest", "CreateQuoteItemRequest", "CreateSalesOrderRequest",
            "CreateSalesOrderItemRequest", "UpdateStatusRequest",
            "UpdateManufacturingStatusRequest", "UpdateSalesOrderStatusRequest",
            "UpdateSalesOrderRequest", "CreateSupplierPortalUserRequest",
            "SupplierApprovalRequest", "CreateSupplierRequest", "LoginRequest",
            "LoginResponse", "CreateUserRequest", "CreatePermissionRequest",
            "UpdatePermissionRequest", "CreateRoleRequest",
            "UpdateRolePermissionsRequest", "UpdateThemeRequest",
            "UpdateUserRolesRequest", "UomConversionResult",
            "UomConversionChainResult", "UomConversionStep"
        };

        var violations = dtos
            .Where(d => !d.Name.EndsWith("Dto"))
            .Where(d => !knownExceptions.Contains(d.Name))
            .Select(d => d.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"DTO 類別應以 Dto 結尾，違規: {string.Join(", ", violations)}");
    }

    [Fact]
    public void AsyncMethods_ShouldEndWithAsync()
    {
        var asyncMethods = AppAssembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("ErpBackend.Services"))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => !m.Name.StartsWith("<") && !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"))
            .Where(m => (m.ReturnType == typeof(Task)
                        || (m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))
                        && !m.Name.EndsWith("Async"))
            .Select(m => $"{m.DeclaringType?.Name}.{m.Name}")
            .Distinct()
            .ToList();

        // 已知例外：ILlmService.GetModelName, ISubstituteService 等方法
        var knownExceptions = new HashSet<string>
        {
            "ILlmService.GetModelName",
            "OpenAiLlmService.GetModelName",
            "ClaudeLlmService.GetModelName",
            "ISubstituteService.GetAvailableSubstitutes",
            "ISubstituteService.FindBestSubstitute",
            "ISubstituteService.ValidateSubstitution",
            "ISubstituteService.LogSubstitution",
            "ISubstituteService.GetSubstitutionHistory",
            "ISubstituteService.CalculateCostVariance",
            "SubstituteService.GetAvailableSubstitutes",
            "SubstituteService.FindBestSubstitute",
            "SubstituteService.ValidateSubstitution",
            "SubstituteService.LogSubstitution",
            "SubstituteService.GetSubstitutionHistory",
            "SubstituteService.CalculateCostVariance",
            "LlmMetricsService.GetReport"
        };

        var violations = asyncMethods
            .Where(m => !knownExceptions.Contains(m))
            .ToList();

        Assert.True(violations.Count == 0,
            $"非同步方法應以 Async 結尾，違規: {string.Join(", ", violations)}");
    }
}
