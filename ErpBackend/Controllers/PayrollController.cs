using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Filters;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize]
public class PayrollController : BaseController
{
    private readonly IPayrollService _payrollService;
    private readonly IPayrollPolicyService _payrollPolicyService;

    public PayrollController(IPayrollService payrollService, IPayrollPolicyService payrollPolicyService)
    {
        _payrollService = payrollService;
        _payrollPolicyService = payrollPolicyService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PayrollDto>>>> GetPayrollRecords(
        [FromQuery] string? employeeId,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] string? status)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var (items, totalCount) = await _payrollService.GetPayrollRecordsAsync(employeeId, startDate, endDate, status, page, pageSize);
        return Ok(items, totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PayrollDto>>> GetPayrollRecord(Guid id)
    {
        var dto = await _payrollService.GetPayrollRecordAsync(id);
        if (dto == null) return NotFound(ApiResponse<PayrollDto>.Fail("Payroll record not found"));
        return Ok(dto);
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<ApiResponse<PayrollDto>>> CalculatePayroll([FromBody] CalculatePayrollRequest req)
    {
        var (dto, status) = await _payrollService.CalculatePayrollAsync(req);
        if (status == "calculation_not_configured")
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse<PayrollDto>.Fail(
                    "Payroll calculation is unavailable until approved rules and statutory tables are configured"));
        if (dto == null) return BadRequest(ApiResponse<PayrollDto>.Fail("Employee not found"));
        return Ok(dto);
    }

    [HttpPatch("{id}/approve")]
    public async Task<ActionResult<ApiResponse>> ApprovePayroll(Guid id)
    {
        var success = await _payrollService.ApprovePayrollAsync(id);
        if (!success) return NotFound(new ApiResponse { Success = false, Message = "Payroll not found" });
        return new OkObjectResult(new ApiResponse { Success = true, Message = "Payroll approved" });
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<PayrollSummaryDto>>> GetPayrollSummary([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
    {
        var summary = await _payrollService.GetPayrollSummaryAsync(periodStart, periodEnd);
        if (summary == null) return NotFound(ApiResponse<PayrollSummaryDto>.Fail("No payroll records found"));
        return Ok(summary);
    }

    [HttpGet("policies")]
    [PermissionAuthorize("hr", "view")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PayrollPolicyVersionDto>>>> GetPayrollPolicies(
        CancellationToken cancellationToken)
        => Ok(await _payrollPolicyService.GetPoliciesAsync(cancellationToken));

    [HttpGet("policies/pending-finance-review")]
    [PermissionAuthorize("finance", "approve")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PayrollPolicyVersionDto>>>> GetPendingFinanceReview(
        CancellationToken cancellationToken)
        => Ok(await _payrollPolicyService.GetPendingFinanceReviewAsync(cancellationToken));

    [HttpGet("policies/{id:guid}")]
    [PermissionAuthorize("hr", "view")]
    public async Task<ActionResult<ApiResponse<PayrollPolicyVersionDto>>> GetPayrollPolicy(
        Guid id,
        CancellationToken cancellationToken)
    {
        var policy = await _payrollPolicyService.GetPolicyAsync(id, cancellationToken);
        return policy == null
            ? NotFound(ApiResponse<PayrollPolicyVersionDto>.Fail("Payroll policy not found"))
            : Ok(policy);
    }

    [HttpPost("policies")]
    [PermissionAuthorize("hr", "create")]
    public async Task<ActionResult<ApiResponse<PayrollPolicyVersionDto>>> CreatePayrollPolicyDraft(
        [FromBody] SavePayrollPolicyRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _payrollPolicyService.CreateDraftAsync(request, GetUserId(), cancellationToken);
        if (result.Policy == null)
            return PolicyOperationError(result);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<PayrollPolicyVersionDto>.Ok(result.Policy, "Draft saved; payroll calculation remains disabled"));
    }

    [HttpPut("policies/{id:guid}")]
    [PermissionAuthorize("hr", "edit")]
    public async Task<ActionResult<ApiResponse<PayrollPolicyVersionDto>>> UpdatePayrollPolicyDraft(
        Guid id,
        [FromBody] SavePayrollPolicyRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _payrollPolicyService.UpdateDraftAsync(id, request, GetUserId(), cancellationToken);
        return result.Policy == null
            ? PolicyOperationError(result)
            : Ok(result.Policy, "Draft updated; payroll calculation remains disabled");
    }

    [HttpPost("policies/{id:guid}/submit")]
    [PermissionAuthorize("hr", "approve")]
    public async Task<ActionResult<ApiResponse<PayrollPolicyVersionDto>>> SubmitPayrollPolicy(
        Guid id,
        [FromBody] SubmitPayrollPolicyRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _payrollPolicyService.SubmitAsync(
            id,
            GetUserId(),
            request.ConfirmedOfficialSources,
            cancellationToken);
        return result.Policy == null
            ? PolicyOperationError(result)
            : Ok(result.Policy, "Submitted for independent Finance review; payroll calculation remains disabled");
    }

    [HttpPost("policies/{id:guid}/approve")]
    [PermissionAuthorize("finance", "approve")]
    public async Task<ActionResult<ApiResponse<PayrollPolicyVersionDto>>> ApprovePayrollPolicy(
        Guid id,
        [FromBody] ReviewPayrollPolicyRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _payrollPolicyService.ApproveAsync(
            id,
            GetUserId(),
            request.ConfirmedOfficialSources,
            request.Comment,
            cancellationToken);
        return result.Policy == null
            ? PolicyOperationError(result)
            : Ok(result.Policy, "Policy approved for recordkeeping only; payroll calculation remains disabled");
    }

    [HttpPost("policies/{id:guid}/reject")]
    [PermissionAuthorize("finance", "approve")]
    public async Task<ActionResult<ApiResponse<PayrollPolicyVersionDto>>> RejectPayrollPolicy(
        Guid id,
        [FromBody] ReviewPayrollPolicyRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _payrollPolicyService.RejectAsync(id, GetUserId(), request.Comment ?? string.Empty, cancellationToken);
        return result.Policy == null
            ? PolicyOperationError(result)
            : Ok(result.Policy, "Policy returned; payroll calculation remains disabled");
    }

    private ActionResult<ApiResponse<PayrollPolicyVersionDto>> PolicyOperationError(PayrollPolicyOperationResultDto result)
    {
        if (result.Error == "invalid_actor")
            return Unauthorized(ApiResponse<PayrollPolicyVersionDto>.Fail("An authenticated user ID is required"));
        if (result.Error == "not_found")
            return NotFound(ApiResponse<PayrollPolicyVersionDto>.Fail("Payroll policy not found"));
        if (result.Error == "policy_incomplete")
            return Conflict(new
            {
                success = false,
                message = "Required policy sections are missing",
                missingFields = result.MissingFields
            });
        if (result.Error == "policy_json_must_be_a_json_object")
            return BadRequest(ApiResponse<PayrollPolicyVersionDto>.Fail("Policy JSON must be a valid JSON object"));
        if (result.Error == "effective_date_required")
            return BadRequest(ApiResponse<PayrollPolicyVersionDto>.Fail("Payroll policy effective date is required"));
        if (result.Error == "approval_document_reference_required")
            return BadRequest(ApiResponse<PayrollPolicyVersionDto>.Fail("HR approval document reference is required"));
        if (result.Error == "reviewer_must_be_independent")
            return Conflict(ApiResponse<PayrollPolicyVersionDto>.Fail("Finance reviewer must be different from the creator and HR submitter"));
        if (result.Error == "review_comment_required")
            return BadRequest(ApiResponse<PayrollPolicyVersionDto>.Fail("A review comment is required for Finance approval or rejection"));
        if (result.Error == "official_sources_not_confirmed")
            return Conflict(ApiResponse<PayrollPolicyVersionDto>.Fail("Official policy source documents must be confirmed before review"));
        if (result.Error == "policy_is_immutable")
            return Conflict(ApiResponse<PayrollPolicyVersionDto>.Fail("Only draft payroll policies can be edited"));
        if (result.Error == "version_code_already_exists")
            return Conflict(ApiResponse<PayrollPolicyVersionDto>.Fail("Payroll policy version code already exists"));
        return Conflict(ApiResponse<PayrollPolicyVersionDto>.Fail("Payroll policy is not in a state that allows this action"));
    }
}
