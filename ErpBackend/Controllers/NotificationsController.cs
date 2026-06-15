using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace ErpBackend.Controllers;

[Route("api/erp/notifications")]
[Authorize]
public class NotificationsController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public NotificationsController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<SystemNotification>>>> GetNotifications([FromQuery] string? role = "all")
    {
        var query = _db.SystemNotifications.AsQueryable();

        if (!string.IsNullOrEmpty(role) && role != "all")
        {
            query = query.Where(n => n.TargetRole == "all" || n.TargetRole == role);
        }

        var list = await query.OrderByDescending(n => n.CreatedAt).Take(50).ToListAsync();
        return Ok<IEnumerable<SystemNotification>>(list);
    }

    [HttpPost("{id}/read")]
    public async Task<ActionResult<ApiResponse>> MarkAsRead(Guid id)
    {
        var n = await _db.SystemNotifications.FindAsync(id);
        if (n == null) return Fail("Notification not found");

        n.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok("Operation successful");
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse>> MarkAllAsRead([FromQuery] string? role = "all")
    {
        var query = _db.SystemNotifications.Where(n => !n.IsRead);

        if (!string.IsNullOrEmpty(role) && role != "all")
        {
            query = query.Where(n => n.TargetRole == "all" || n.TargetRole == role);
        }

        await query.ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return Ok("Operation successful");
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount([FromQuery] string? role = "all")
    {
        var query = _db.SystemNotifications.Where(n => !n.IsRead);

        if (!string.IsNullOrEmpty(role) && role != "all")
        {
            query = query.Where(n => n.TargetRole == "all" || n.TargetRole == role);
        }

        var count = await query.CountAsync();
        return Ok(count);
    }
}
