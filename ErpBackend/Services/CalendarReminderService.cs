using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.Collaboration;

namespace ErpBackend.Services;

public class CalendarReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CalendarReminderService> _logger;

    public CalendarReminderService(IServiceProvider serviceProvider, ILogger<CalendarReminderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessReminders();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing calendar reminders");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessReminders()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpGeneralDbContext>();

        var now = DateTime.UtcNow;
        var pendingReminders = await db.CalendarReminderLogs
            .Include(r => r.CalendarEvent)
            .Where(r => r.Status == "pending" && r.ScheduledAt <= now)
            .ToListAsync();

        foreach (var reminder in pendingReminders)
        {
            try
            {
                if (reminder.Channel == "system")
                {
                    db.NotificationQueues.Add(new Models.System.NotificationQueue
                    {
                        Type = "calendar_reminder",
                        TargetUserId = reminder.EmployeeId,
                        Title = $"行事曆提醒: {reminder.CalendarEvent.Title}",
                        Content = $"事件「{reminder.CalendarEvent.Title}」將於 {reminder.CalendarEvent.StartDate:yyyy/MM/dd HH:mm} 開始",
                        Channel = "system",
                        Status = "pending",
                    });
                }

                reminder.Status = "sent";
                reminder.SentAt = now;
            }
            catch (Exception ex)
            {
                reminder.Status = "failed";
                reminder.ErrorMessage = ex.Message[..Math.Min(ex.Message.Length, 500)];
                _logger.LogWarning(ex, "Failed to send calendar reminder {ReminderId}", reminder.Id);
            }
        }

        if (pendingReminders.Count > 0)
        {
            await db.SaveChangesAsync();
            _logger.LogInformation("Processed {Count} calendar reminders", pendingReminders.Count);
        }
    }
}
