using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;

namespace ErpBackend.Services;

public interface ILeaveRequestService
{
    Task<List<LeaveRequestDto>> GetAllAsync(
        string? search = null,
        Guid? employeeId = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
    Task<List<EmployeeEssLeaveBalanceDto>> GetLeaveBalancesAsync(Guid employeeId, int year, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(LeaveRequestDto? dto, string status)> CreateAsync(CreateLeaveRequestRequest request, CancellationToken cancellationToken = default);
    Task<(bool ok, string status)> ApproveAsync(Guid id, ApproveLeaveRequestRequest request);
    Task<(bool ok, string status)> RejectAsync(Guid id, RejectLeaveRequestRequest request);
}

public class LeaveRequestService : ILeaveRequestService
{
    private readonly ErpGeneralDbContext _db;

    public LeaveRequestService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<LeaveRequestDto>> GetAllAsync(
        string? search = null,
        Guid? employeeId = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _db.LeaveRequests.Include(lr => lr.Employee).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(lr => lr.Employee.ChineseName.Contains(search) || lr.ApplicationNo.Contains(search));

        if (employeeId.HasValue)
            query = query.Where(lr => lr.EmployeeId == employeeId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(lr => lr.Status == status);

        var items = await query
            .OrderByDescending(lr => lr.ApplicationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return items.Select(MapToDto).ToList();
    }

    public async Task<List<EmployeeEssLeaveBalanceDto>> GetLeaveBalancesAsync(
        Guid employeeId,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _db.LeaveBalances
            .AsNoTracking()
            .Where(balance => balance.EmployeeId == employeeId && balance.Year == year)
            .OrderBy(balance => balance.LeaveType)
            .Select(balance => new EmployeeEssLeaveBalanceDto
            {
                LeaveType = balance.LeaveType,
                Year = balance.Year,
                TotalDays = balance.TotalEntitled,
                UsedDays = balance.Used,
                RemainingDays = balance.Remaining
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<LeaveRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lr = await _db.LeaveRequests
            .Include(lr => lr.Employee)
            .AsNoTracking()
            .FirstOrDefaultAsync(lr => lr.Id == id, cancellationToken);

        return lr == null ? null : MapToDto(lr);
    }

    public Task<(LeaveRequestDto? dto, string status)> CreateAsync(
        CreateLeaveRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            return Task.FromResult<(LeaveRequestDto?, string)>((null, "invalid"));

        if (!Guid.TryParse(request.EmployeeId, out var empId))
            return Task.FromResult<(LeaveRequestDto?, string)>((null, "invalid"));

        return Task.FromResult<(LeaveRequestDto?, string)>((null, "schedule_not_configured"));
    }

    public Task<(bool ok, string status)> ApproveAsync(Guid id, ApproveLeaveRequestRequest request)
        => Task.FromResult((false, "approval_not_configured"));

    public async Task<(bool ok, string status)> RejectAsync(Guid id, RejectLeaveRequestRequest request)
    {
        var lr = await _db.LeaveRequests.FirstOrDefaultAsync(lr => lr.Id == id);
        if (lr == null) return (false, "not_found");

        if (lr.Status != "pending") return (false, "guarded");

        lr.Status = "rejected";
        lr.ApproverId = request.ApproverId;
        lr.RejectReason = request.RejectReason;
        lr.ApprovedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (true, "ok");
    }

    private static LeaveRequestDto MapToDto(LeaveRequest lr) => new()
    {
        Id = lr.Id,
        ApplicationNo = lr.ApplicationNo,
        EmployeeId = lr.EmployeeId,
        EmployeeName = lr.Employee?.ChineseName,
        LeaveType = lr.LeaveType,
        ApplicationDate = lr.ApplicationDate,
        StartDate = lr.StartDate,
        EndDate = lr.EndDate,
        StartTime = lr.StartTime.HasValue ? lr.StartDate.Date.Add(lr.StartTime.Value.ToTimeSpan()) : null,
        EndTime = lr.EndTime.HasValue ? lr.EndDate.Date.Add(lr.EndTime.Value.ToTimeSpan()) : null,
        TotalHours = lr.TotalHours,
        Reason = lr.Reason,
        AttachmentUrl = lr.AttachmentUrl,
        IncludesSpecialPeriod = lr.IncludesSpecialPeriod,
        ShiftConflictFlag = lr.ShiftConflictFlag,
        AutoDeductPayFlag = lr.AutoDeductPayFlag,
        ImpactOtOrBonus = lr.ImpactOtOrBonus,
        ApprovalLevel = lr.ApprovalLevel,
        Status = lr.Status,
        ApproverId = lr.ApproverId,
        ApprovedAt = lr.ApprovedAt,
        RejectReason = lr.RejectReason,
        Remarks = lr.Remarks,
        CreatedAt = lr.CreatedAt
    };
}
