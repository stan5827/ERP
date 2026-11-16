using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize]
[Route("api/erp/leave-requests")]
public class LeaveRequestsController : BaseController
{
    private readonly ILeaveRequestService _leaveRequestService;

    public LeaveRequestsController(ILeaveRequestService leaveRequestService) => _leaveRequestService = leaveRequestService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<LeaveRequestDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? employeeId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var dtos = await _leaveRequestService.GetAllAsync(search, employeeId, status, page, pageSize);
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> Get(Guid id)
    {
        var dto = await _leaveRequestService.GetByIdAsync(id);
        return dto == null ? NotFound(ApiHelper.Fail("Leave request not found")) : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> Create([FromBody] CreateLeaveRequestRequest request)
    {
        var (dto, status) = await _leaveRequestService.CreateAsync(request);
        if (status == "invalid") return BadRequest(ApiHelper.FailOf<LeaveRequestDto>("Invalid request"));
        if (status == "not_found") return BadRequest(ApiHelper.FailOf<LeaveRequestDto>("Employee not found"));
        if (status == "schedule_not_configured")
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiHelper.FailOf<LeaveRequestDto>(
                    "Leave requests are unavailable until work schedules and holidays are configured"));
        return Ok(dto!, "Leave request created");
    }

    [HttpPatch("{id}/approve")]
    public async Task<ActionResult<ApiResponse>> Approve(Guid id, [FromBody] ApproveLeaveRequestRequest request)
    {
        var (ok, status) = await _leaveRequestService.ApproveAsync(id, request);
        if (status == "approval_not_configured")
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiHelper.Fail("Leave approvals are unavailable until leave-hour calculation and balance transactions are configured"));
        if (status == "not_found") return NotFound(ApiHelper.Fail("Leave request not found"));
        if (status == "guarded") return BadRequest(ApiHelper.Fail("Only pending requests can be approved"));
        return Ok("Leave request approved");
    }

    [HttpPatch("{id}/reject")]
    public async Task<ActionResult<ApiResponse>> Reject(Guid id, [FromBody] RejectLeaveRequestRequest request)
    {
        var (ok, status) = await _leaveRequestService.RejectAsync(id, request);
        if (status == "not_found") return NotFound(ApiHelper.Fail("Leave request not found"));
        if (status == "guarded") return BadRequest(ApiHelper.Fail("Only pending requests can be rejected"));
        return Ok("Leave request rejected");
    }
}
