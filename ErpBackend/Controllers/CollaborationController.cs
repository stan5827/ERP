using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize]
public class CollaborationController : BaseController
{
    private readonly IAnnouncementService _announcementService;
    private readonly ICollaborationService _collaborationService;

    public CollaborationController(IAnnouncementService announcementService, ICollaborationService collaborationService)
    {
        _announcementService = announcementService;
        _collaborationService = collaborationService;
    }

    [HttpGet("announcements")]
    public async Task<ActionResult<ApiResponse<List<AnnouncementDto>>>> GetAnnouncements()
    {
        var announcements = await _announcementService.GetAnnouncementsAsync(GetUserId());
        return Ok(announcements, totalCount: announcements.Count);
    }

    [HttpPost("announcements")]
    public async Task<ActionResult<ApiResponse<AnnouncementDto>>> CreateAnnouncement(
        [FromBody] CreateAnnouncementRequestDto request)
    {
        try
        {
            var announcement = await _announcementService.CreateAnnouncementAsync(request, GetUserId());
            return Ok(announcement, "?????");
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<AnnouncementDto>.Fail(exception.Message));
        }
    }

    [HttpPatch("announcements/{id:guid}/read")]
    public async Task<ActionResult<ApiResponse>> MarkAnnouncementRead(Guid id)
    {
        var marked = await _announcementService.MarkReadAsync(id, GetUserId());
        return marked ? Ok("????????") : NotFound(ApiHelper.Fail("???????????????"));
    }

    [HttpGet("meeting-rooms")]
    public async Task<ActionResult<ApiResponse<List<MeetingRoomDto>>>> GetMeetingRooms()
    {
        var rooms = await _collaborationService.GetMeetingRoomsAsync();
        return Ok(rooms, totalCount: rooms.Count);
    }

    [HttpPost("meeting-rooms")]
    public async Task<ActionResult<ApiResponse<MeetingRoomDto>>> CreateMeetingRoom(
        [FromBody] CreateMeetingRoomRequestDto request)
    {
        try
        {
            var room = await _collaborationService.CreateMeetingRoomAsync(request);
            return Ok(room, "??????");
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<MeetingRoomDto>.Fail(exception.Message));
        }
    }

    [HttpGet("meeting-reservations")]
    public async Task<ActionResult<ApiResponse<List<MeetingReservationDto>>>> GetReservations()
    {
        var parameters = new Dictionary<string, string>();
        var roomId = Request.Query["roomId"].FirstOrDefault();
        var startDate = Request.Query["startDate"].FirstOrDefault();
        var endDate = Request.Query["endDate"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(roomId)) parameters["roomId"] = roomId;
        if (!string.IsNullOrWhiteSpace(startDate)) parameters["startDate"] = startDate;
        if (!string.IsNullOrWhiteSpace(endDate)) parameters["endDate"] = endDate;

        var reservations = await _collaborationService.GetReservationsAsync(parameters.Count > 0 ? parameters : null);
        return Ok(reservations, totalCount: reservations.Count);
    }

    [HttpPost("meeting-reservations")]
    public async Task<ActionResult<ApiResponse<MeetingReservationDto>>> CreateReservation(
        [FromBody] CreateMeetingReservationRequestDto request)
    {
        try
        {
            var reservation = await _collaborationService.CreateReservationAsync(request, GetUserId());
            return Ok(reservation, "??????");
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<MeetingReservationDto>.Fail(exception.Message));
        }
    }

    [HttpGet("tasks")]
    public async Task<ActionResult<ApiResponse<List<CrossDeptTaskDto>>>> GetTasks()
    {
        var parameters = new Dictionary<string, string>();
        var status = Request.Query["status"].FirstOrDefault();
        var urgency = Request.Query["urgency"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(status)) parameters["status"] = status;
        if (!string.IsNullOrWhiteSpace(urgency)) parameters["urgency"] = urgency;

        var tasks = await _collaborationService.GetTasksAsync(parameters.Count > 0 ? parameters : null);
        return Ok(tasks, totalCount: tasks.Count);
    }

    [HttpPost("tasks")]
    public async Task<ActionResult<ApiResponse<CrossDeptTaskDto>>> CreateTask(
        [FromBody] CreateCrossDeptTaskRequestDto request)
    {
        try
        {
            var task = await _collaborationService.CreateTaskAsync(request, GetUserId());
            return Ok(task, "????????");
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<CrossDeptTaskDto>.Fail(exception.Message));
        }
    }

    [HttpPatch("tasks/{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateTaskStatus(Guid id, [FromBody] UpdateCrossDeptTaskStatusRequestDto request)
    {
        var updated = await _collaborationService.UpdateTaskStatusAsync(id, request.Status, GetUserId());
        return updated ? Ok("???????") : NotFound(ApiHelper.Fail("?????"));
    }

    [HttpPost("tasks/{id:guid}/replies")]
    public async Task<ActionResult<ApiResponse>> AddTaskReply(Guid id, [FromBody] CreateCrossDeptTaskReplyRequestDto request)
    {
        var added = await _collaborationService.AddTaskReplyAsync(id, request, GetUserId());
        return added ? Ok("?????") : NotFound(ApiHelper.Fail("?????"));
    }
}
