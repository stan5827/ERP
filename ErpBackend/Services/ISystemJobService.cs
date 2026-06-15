using ErpBackend.Models.System;

namespace ErpBackend.Services;

public interface ISystemJobService
{
    Task<SystemJob> CreateJobAsync(string jobType, string? parameters = null, Guid? triggeredBy = null);
    Task StartJobAsync(Guid jobId);
    Task UpdateProgressAsync(Guid jobId, int progress, string? message = null);
    Task CompleteJobAsync(Guid jobId, string? resultData = null);
    Task FailJobAsync(Guid jobId, string errorMessage);
    Task<SystemJob?> GetJobAsync(Guid jobId);
    Task<List<SystemJob>> GetRecentJobsAsync(int count = 50);
}
