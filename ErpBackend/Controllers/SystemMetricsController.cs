using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;

namespace ErpBackend.Controllers;

[Route("api/erp/system-metrics")]
[Authorize]
public class SystemMetricsController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    private readonly IConfiguration _config;

    public SystemMetricsController(ErpGeneralDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse>> GetOverview()
    {
        try
        {
            var overview = new
            {
                DatabaseStatus = "Online",
                Provider = _db.Database.ProviderName,
                TotalAuditLogs = await _db.AuditLogs.CountAsync(),
                RecentLogsCount = await _db.AuditLogs.CountAsync(l => l.CreatedAt >= DateTime.UtcNow.AddHours(-24)),
                Environment = GetEnvMetrics()
            };
            return OkObject(overview);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    [HttpGet("audit")]
    public async Task<ActionResult<ApiResponse>> GetAuditMetrics()
    {
        var topUsers = await _db.AuditLogs
            .GroupBy(l => l.UserId)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync();

        return OkObject(new
        {
            TopActiveUsers = topUsers
        });
    }

    private dynamic GetEnvMetrics()
    {
        return new
        {
            OS = Environment.OSVersion.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            WorkingSetMB = Environment.WorkingSet / 1024 / 1024,
            DotNetVersion = Environment.Version.ToString()
        };
    }
}
