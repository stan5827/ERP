using ErpBackend.Data;
using ErpBackend.Models.System;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public class SystemJobService : ISystemJobService
{
    private readonly ErpGeneralDbContext _context;
    private readonly ILogger<SystemJobService> _logger;

    public SystemJobService(ErpGeneralDbContext context, ILogger<SystemJobService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SystemJob> CreateJobAsync(string jobType, string? parameters = null, Guid? triggeredBy = null)
    {
        var job = new SystemJob
        {
            JobName = jobType, // Use JobType as JobName for now
            JobType = jobType,
            Parameters = parameters,
            CreatedBy = triggeredBy,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.SystemJobs.Add(job);
        await _context.SaveChangesAsync();
        return job;
    }

    public async Task StartJobAsync(Guid jobId)
    {
        var job = await _context.SystemJobs.FindAsync(jobId);
        if (job != null)
        {
            job.Status = "Processing";
            job.StartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateProgressAsync(Guid jobId, int progress, string? message = null)
    {
        var job = await _context.SystemJobs.FindAsync(jobId);
        if (job != null)
        {
            job.Progress = progress;
            job.CurrentStep = message;
            await _context.SaveChangesAsync();
        }
    }

    public async Task CompleteJobAsync(Guid jobId, string? resultData = null)
    {
        var job = await _context.SystemJobs.FindAsync(jobId);
        if (job != null)
        {
            job.Status = "Completed";
            job.Progress = 100;
            job.CompletedAt = DateTime.UtcNow;
            job.Result = resultData;
            await _context.SaveChangesAsync();
        }
    }

    public async Task FailJobAsync(Guid jobId, string errorMessage)
    {
        var job = await _context.SystemJobs.FindAsync(jobId);
        if (job != null)
        {
            job.Status = "Failed";
            job.ErrorMessage = errorMessage;
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<SystemJob?> GetJobAsync(Guid jobId)
    {
        return await _context.SystemJobs.FindAsync(jobId);
    }

    public async Task<List<SystemJob>> GetRecentJobsAsync(int count = 50)
    {
        return await _context.SystemJobs
            .OrderByDescending(j => j.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}
