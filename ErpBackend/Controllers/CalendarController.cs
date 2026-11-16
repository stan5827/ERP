using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize]
public class CalendarController : BaseController
{
    private readonly ICalendarService _calendarService;

    public CalendarController(ICalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    [HttpGet("events")]
    public async Task<ActionResult<ApiResponse<List<CalendarEventDto>>>> GetEvents()
    {
        var parameters = new Dictionary<string, string>();
        foreach (var key in Request.Query.Keys)
        {
            var value = Request.Query[key].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(value))
                parameters[key] = value;
        }

        var events = await _calendarService.GetEventsAsync(GetUserId(), parameters.Count > 0 ? parameters : null);
        return Ok(events, totalCount: events.Count);
    }

    [HttpGet("range")]
    public async Task<ActionResult<ApiResponse<List<CalendarEventDto>>>> GetRange()
    {
        var startDateStr = Request.Query["startDate"].FirstOrDefault();
        var endDateStr = Request.Query["endDate"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(startDateStr) || string.IsNullOrWhiteSpace(endDateStr))
            return BadRequest(ApiResponse.Fail("startDate ? endDate ??????"));

        if (!DateTime.TryParse(startDateStr, out var startDate) || !DateTime.TryParse(endDateStr, out var endDate))
            return BadRequest(ApiResponse.Fail("???????"));

        var events = await _calendarService.GetRangeAsync(startDate, endDate, GetUserId());
        return Ok(events, totalCount: events.Count);
    }

    [HttpGet("events/{id:guid}")]
    public async Task<ActionResult<ApiResponse<CalendarEventDto>>> GetEvent(Guid id)
    {
        var ev = await _calendarService.GetEventAsync(id, GetUserId());
        return ev == null ? NotFound(ApiResponse<CalendarEventDto>.Fail("????????")) : Ok(ev);
    }

    [HttpPost("events")]
    public async Task<ActionResult<ApiResponse<CalendarEventDto>>> CreateEvent(
        [FromBody] CreateCalendarEventRequestDto request)
    {
        try
        {
            var ev = await _calendarService.CreateEventAsync(request, GetUserId());
            return Ok(ev, "????????");
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<CalendarEventDto>.Fail(exception.Message));
        }
    }

    [HttpPut("events/{id:guid}")]
    public async Task<ActionResult<ApiResponse<CalendarEventDto>>> UpdateEvent(Guid id, [FromBody] UpdateCalendarEventRequestDto request)
    {
        try
        {
            var ev = await _calendarService.UpdateEventAsync(id, request, GetUserId());
            return ev == null ? NotFound(ApiResponse<CalendarEventDto>.Fail("????????")) : Ok(ev, "????????");
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<CalendarEventDto>.Fail(exception.Message));
        }
    }

    [HttpDelete("events/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteEvent(Guid id)
    {
        var deleted = await _calendarService.DeleteEventAsync(id, GetUserId());
        return deleted ? Ok("????????") : NotFound(ApiHelper.Fail("????????"));
    }

    [HttpPatch("events/{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateEventStatus(Guid id, [FromBody] UpdateEventStatusRequestDto request)
    {
        var updated = await _calendarService.UpdateEventStatusAsync(id, request.Status, GetUserId());
        return updated ? Ok("?????") : NotFound(ApiHelper.Fail("????????"));
    }

    [HttpGet("conflicts")]
    public async Task<ActionResult<ApiResponse<List<CalendarEventDto>>>> GetConflicts()
    {
        var startDateStr = Request.Query["startDate"].FirstOrDefault();
        var endDateStr = Request.Query["endDate"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(startDateStr) || string.IsNullOrWhiteSpace(endDateStr))
            return BadRequest(ApiResponse.Fail("startDate ? endDate ??????"));

        if (!DateTime.TryParse(startDateStr, out var startDate) || !DateTime.TryParse(endDateStr, out var endDate))
            return BadRequest(ApiResponse.Fail("???????"));

        var excludeIdStr = Request.Query["excludeEventId"].FirstOrDefault();
        Guid? excludeId = null;
        if (!string.IsNullOrWhiteSpace(excludeIdStr) && Guid.TryParse(excludeIdStr, out var parsedId))
            excludeId = parsedId;

        var conflicts = await _calendarService.GetConflictsAsync(GetUserId(), startDate, endDate, excludeId);
        return Ok(conflicts, totalCount: conflicts.Count);
    }

    [HttpGet("export/ics")]
    public async Task<IActionResult> ExportIcs()
    {
        var startDateStr = Request.Query["startDate"].FirstOrDefault();
        var endDateStr = Request.Query["endDate"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(startDateStr) || string.IsNullOrWhiteSpace(endDateStr))
            return BadRequest("startDate ? endDate ??????");

        if (!DateTime.TryParse(startDateStr, out var startDate) || !DateTime.TryParse(endDateStr, out var endDate))
            return BadRequest("???????");

        var events = await _calendarService.GetRangeAsync(startDate, endDate, GetUserId());
        var ics = BuildIcs(events, startDate, endDate);
        return File(System.Text.Encoding.UTF8.GetBytes(ics), "text/calendar", $"calendar-{startDate:yyyyMMdd}-to-{endDate:yyyyMMdd}.ics");
    }

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<CalendarCategoryDto>>>> GetCategories()
    {
        var categories = await _calendarService.GetCategoriesAsync();
        return Ok(categories, totalCount: categories.Count);
    }

    [HttpPost("categories")]
    public async Task<ActionResult<ApiResponse<CalendarCategoryDto>>> CreateCategory(
        [FromBody] CreateCalendarCategoryRequestDto request)
    {
        try
        {
            var category = await _calendarService.CreateCategoryAsync(request);
            return Ok(category, "????????");
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<CalendarCategoryDto>.Fail(exception.Message));
        }
    }

    [HttpPut("categories/{id:guid}")]
    public async Task<ActionResult<ApiResponse<CalendarCategoryDto>>> UpdateCategory(Guid id, [FromBody] UpdateCalendarCategoryRequestDto request)
    {
        try
        {
            var updated = await _calendarService.UpdateCategoryAsync(id, request);
            if (!updated) return NotFound(ApiResponse<CalendarCategoryDto>.Fail("?????"));
            var category = await _calendarService.GetCategoriesAsync();
            var dto = category.FirstOrDefault(c => c.Id == id);
            return dto == null ? NotFound(ApiResponse<CalendarCategoryDto>.Fail("?????")) : Ok(dto, "?????");
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<CalendarCategoryDto>.Fail(exception.Message));
        }
    }

    [HttpDelete("categories/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteCategory(Guid id)
    {
        var deleted = await _calendarService.DeleteCategoryAsync(id);
        return deleted ? Ok("?????") : NotFound(ApiHelper.Fail("?????"));
    }

    [HttpGet("holidays")]
    public async Task<ActionResult<ApiResponse<List<CompanyHolidayDto>>>> GetHolidays()
    {
        var parameters = new Dictionary<string, string>();
        var year = Request.Query["year"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(year)) parameters["year"] = year;

        var holidays = await _calendarService.GetHolidaysAsync(parameters.Count > 0 ? parameters : null);
        return Ok(holidays, totalCount: holidays.Count);
    }

    [HttpPost("holidays")]
    public async Task<ActionResult<ApiResponse<CompanyHolidayDto>>> CreateHoliday(
        [FromBody] CreateCompanyHolidayRequestDto request)
    {
        try
        {
            var holiday = await _calendarService.CreateHolidayAsync(request);
            return Ok(holiday, "?????");
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<CompanyHolidayDto>.Fail(exception.Message));
        }
    }

    [HttpPut("holidays/{id:guid}")]
    public async Task<ActionResult<ApiResponse<CompanyHolidayDto>>> UpdateHoliday(Guid id, [FromBody] UpdateCompanyHolidayRequestDto request)
    {
        try
        {
            var updated = await _calendarService.UpdateHolidayAsync(id, request);
            if (!updated) return NotFound(ApiResponse<CompanyHolidayDto>.Fail("?????"));
            return Ok(new CompanyHolidayDto(), "?????");
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<CompanyHolidayDto>.Fail(exception.Message));
        }
    }

    [HttpDelete("holidays/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteHoliday(Guid id)
    {
        var deleted = await _calendarService.DeleteHolidayAsync(id);
        return deleted ? Ok("?????") : NotFound(ApiHelper.Fail("?????"));
    }

    [HttpGet("shares")]
    public async Task<ActionResult<ApiResponse<List<CalendarShareDto>>>> GetShares()
    {
        var parameters = new Dictionary<string, string>();
        var ownerId = Request.Query["ownerId"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(ownerId)) parameters["ownerId"] = ownerId;

        var shares = await _calendarService.GetSharesAsync(parameters.Count > 0 ? parameters : null);
        return Ok(shares, totalCount: shares.Count);
    }

    [HttpPost("shares")]
    public async Task<ActionResult<ApiResponse<CalendarShareDto>>> CreateShare(
        [FromBody] CreateCalendarShareRequestDto request)
    {
        try
        {
            var share = await _calendarService.CreateShareAsync(request, GetUserId());
            return Ok(share, "????????");
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<CalendarShareDto>.Fail(exception.Message));
        }
    }

    [HttpDelete("shares/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteShare(Guid id)
    {
        var deleted = await _calendarService.DeleteShareAsync(id, GetUserId());
        return deleted ? Ok("?????") : NotFound(ApiHelper.Fail("?????"));
    }

    private static string BuildIcs(List<CalendarEventDto> events, DateTime startDate, DateTime endDate)
    {
        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//ERP System//Calendar//EN",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            $"X-WR-CALNAME:ERP Calendar ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd})",
            "X-WR-TIMEZONE:UTC"
        };

        foreach (var ev in events)
        {
            lines.Add("BEGIN:VEVENT");
            lines.Add($"UID:{ev.Id}@erp.local");
            lines.Add($"DTSTAMP:{ev.CreatedAt:yyyyMMddTHHmmssZ}");
            lines.Add($"DTSTART:{FormatIcsDate(ev.StartDate, ev.IsAllDay)}");
            lines.Add($"DTEND:{FormatIcsDate(ev.EndDate, ev.IsAllDay)}");
            lines.Add($"SUMMARY:{EscapeIcsText(ev.Title)}");
            if (!string.IsNullOrWhiteSpace(ev.Description))
                lines.Add($"DESCRIPTION:{EscapeIcsText(ev.Description)}");
            if (!string.IsNullOrWhiteSpace(ev.Location))
                lines.Add($"LOCATION:{EscapeIcsText(ev.Location)}");
            lines.Add($"STATUS:{ev.Status}");
            lines.Add("END:VEVENT");
        }

        lines.Add("END:VCALENDAR");
        return string.Join("\r\n", lines);
    }

    private static string FormatIcsDate(DateTime date, bool isAllDay)
    {
        var utc = DateTime.SpecifyKind(date, DateTimeKind.Utc);
        return isAllDay ? utc.ToString("yyyyMMdd") : utc.ToString("yyyyMMddTHHmmssZ");
    }

    private static string EscapeIcsText(string text)
    {
        return text.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n");
    }
}
