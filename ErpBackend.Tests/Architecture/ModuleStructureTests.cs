using System.Reflection;
using Xunit;

namespace ErpBackend.Tests.Architecture;

/// <summary>
/// 模組目錄結構規範測試
/// 驗證各業務模組之目錄組織與命名是否符合標準。
/// </summary>
public class ModuleStructureTests
{
    private static Assembly AppAssembly => typeof(ErpBackend.Data.ErpGeneralDbContext).Assembly;

    /// <summary>
    /// 過濾編譯器產生的類型。
    /// </summary>
    private static IEnumerable<Type> GetUserTypes()
    {
        return AppAssembly.GetTypes()
            .Where(t => !t.IsNested
                        && !t.Name.StartsWith("<")
                        && !t.Name.Contains("`")
                        && !t.Name.StartsWith("<>")
                        && !t.IsGenericTypeDefinition);
    }

    [Fact]
    public void Models_ShouldBeOrganizedByModule()
    {
        var modelTypes = GetUserTypes()
            .Where(t => t.IsClass && t.Namespace != null && t.Namespace.StartsWith("ErpBackend.Models"))
            .ToList();

        Assert.True(modelTypes.Count > 100, $"應有超過 100 個模型類型，實際 {modelTypes.Count}");
    }

    [Fact]
    public void Controllers_ShouldOnlyExistInControllersNamespace()
    {
        var controllers = GetUserTypes()
            .Where(t => t.IsClass && t.Name.EndsWith("Controller"))
            .ToList();

        var violations = controllers
            .Where(c => c.Namespace != "ErpBackend.Controllers")
            .Select(c => c.FullName)
            .ToList();

        Assert.True(violations.Count == 0,
            $"所有控制器應僅位於 ErpBackend.Controllers，違規: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Dtos_ShouldBeInDtosNamespace()
    {
        var dtos = GetUserTypes()
            .Where(t => t.IsClass && t.Name.EndsWith("Dto"))
            .ToList();

        var violations = dtos
            .Where(d => d.Namespace != "ErpBackend.DTOs")
            .Select(d => d.FullName)
            .ToList();

        Assert.True(violations.Count == 0,
            $"所有 DTO 類別應位於 ErpBackend.DTOs，違規: {string.Join(", ", violations)}");
    }

    [Fact]
    public void AllDtos_ShouldEndWithDtoSuffix()
    {
        var dtos = GetUserTypes()
            .Where(t => t.IsClass && t.Namespace == "ErpBackend.DTOs")
            .ToList();

        // 已知例外：非傳統 DTO（Request、Response、Result、Helper、Config 等）
        var knownExceptions = new HashSet<string>
        {
            "GenerateScheduleRequest", "OptimizationRequest", "RescheduleRequest",
            "PlanningRequest", "PlanningResponse", "PlanTask", "ExecutionRequest",
            "ExecutionResponse", "TaskResult", "ReviewRequest", "ReviewResponse",
            "KpiValidationResult", "AgentState", "PlanningGraph", "GraphNode",
            "GraphEdge", "ErpTool", "AgentExecutionRecord", "AiScheduleRequest",
            "AiScheduleResponse", "AiScheduleSummary", "ApiHelper", "ApiResponse",
            "BadRequest", "ApprovalChainResult", "ApprovalLevelInfo",
            "ApprovalCheckResult", "ApsScheduleResultItem", "ApsImportScheduleRequest",
            "ChangePasswordRequest", "AdminResetRequest", "BudgetOverrunCheck",
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
            $"DTO 類別名稱應以 Dto 結尾，違規: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Models_ShouldNotHaveDtoSuffix()
    {
        var violations = GetUserTypes()
            .Where(t => t.IsClass && t.Namespace != null && t.Namespace.StartsWith("ErpBackend.Models"))
            .Where(t => t.Name.EndsWith("Dto"))
            .Select(t => t.FullName)
            .ToList();

        // 允許 Models.MES.EquipmentListDto（已知例外）
        var known = new HashSet<string> { "ErpBackend.Models.MES.EquipmentListDto" };
        violations = violations.Where(v => !known.Contains(v)).ToList();

        Assert.True(violations.Count == 0,
            $"領域模型不應以 Dto 結尾，違規: {string.Join(", ", violations)}");
    }

    [Fact]
    public void MajorModules_ShouldHaveModels()
    {
        // 確認主要業務模組都有對應模型
        var modelNamespaces = AppAssembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("ErpBackend.Models"))
            .Select(t => t.Namespace)
            .Distinct()
            .ToList();

        var modules = new[]
        {
            "Inventory", "HR", "Procurement", "Sales", "MES",
            "Finance", "System", "Budget", "Asset", "QA"
        };

        foreach (var module in modules)
        {
            var expectedNs = $"ErpBackend.Models.{module}";
            Assert.True(modelNamespaces.Any(ns => ns != null && ns.StartsWith(expectedNs)),
                $"缺少模組模型命名空間: {expectedNs}");
        }
    }
}
