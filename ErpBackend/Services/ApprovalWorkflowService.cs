using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;

namespace ErpBackend.Services;

public class ApprovalWorkflowService
{
    private readonly ErpGeneralDbContext _db;

    public ApprovalWorkflowService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<ApprovalChainResult> GetApprovalChainAsync(string docType, decimal amount)
    {
        var authorities = await _db.ApprovalAuthorities
            .Include(a => a.Grade)
            .Where(a => a.IsActive && a.DocType == docType && a.MinAmount <= amount && a.MaxAmount >= amount)
            .OrderBy(a => a.ApprovalLevel)
            .ToListAsync();

        var result = new ApprovalChainResult
        {
            DocType = docType,
            Amount = amount,
            Levels = authorities.Select(a => new ApprovalLevelInfo
            {
                Level = a.ApprovalLevel,
                DocType = a.DocType,
                GradeId = a.GradeId,
                GradeCode = a.Grade.GradeCode,
                GradeName = a.Grade.GradeName,
                MinAmount = a.MinAmount,
                MaxAmount = a.MaxAmount,
                Description = a.Description
            }).ToList(),
            TotalLevels = authorities.Count
        };

        return result;
    }

    public async Task<ApprovalCheckResult> CheckApprovalAuthorityAsync(Guid employeeId, string docType, decimal amount)
    {
        var employee = await _db.Employees
            .Include(e => e.Grade)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            return new ApprovalCheckResult
            {
                CanApprove = false,
                Message = "員工不存在"
            };
        }

        if (employee.Status != "active")
        {
            return new ApprovalCheckResult
            {
                CanApprove = false,
                Message = "員工狀態非在職"
            };
        }

        if (employee.Grade == null)
        {
            return new ApprovalCheckResult
            {
                CanApprove = false,
                Message = "員工未設定職等"
            };
        }

        var authority = await _db.ApprovalAuthorities
            .Include(a => a.Grade)
            .Where(a => a.IsActive && a.DocType == docType)
            .Where(a => a.MinAmount <= amount)
            .OrderByDescending(a => a.Grade.Level)
            .FirstOrDefaultAsync();

        if (authority == null)
        {
            return new ApprovalCheckResult
            {
                CanApprove = false,
                UserGradeLevel = employee.Grade.Level,
                UserApprovalLimit = employee.Grade.ApprovalLimit,
                RequiredAmount = amount,
                Message = "此文件類型/金額未設定核決權限"
            };
        }

        var canApprove = employee.Grade.Level >= authority.Grade.Level;
        var nextAuthority = await _db.ApprovalAuthorities
            .Include(a => a.Grade)
            .Where(a => a.IsActive && a.DocType == docType && a.ApprovalLevel > authority.ApprovalLevel)
            .OrderBy(a => a.ApprovalLevel)
            .FirstOrDefaultAsync();

        return new ApprovalCheckResult
        {
            CanApprove = canApprove,
            UserGradeLevel = employee.Grade.Level,
            UserApprovalLimit = employee.Grade.ApprovalLimit,
            RequiredAmount = amount,
            Message = canApprove ? "可核准" : $"需要 {authority.Grade.GradeName} 或更高職等核准",
            NextApproverGrade = nextAuthority?.Grade.GradeName,
            NextApprovalLimit = nextAuthority?.MaxAmount
        };
    }

    public async Task<List<Employee>> GetApproversAsync(string docType, decimal amount)
    {
        var chain = await GetApprovalChainAsync(docType, amount);
        if (chain.Levels.Count == 0) return new List<Employee>();

        var maxGradeId = chain.Levels.Last().GradeId;
        var minLevel = chain.Levels.First().Level;

        var approvers = await _db.Employees
            .Include(e => e.Department)
            .Include(e => e.Grade)
            .Include(e => e.JobTitle)
            .Where(e => e.Status == "active")
            .Where(e => e.Grade != null && e.JobTitle != null && e.JobTitle.IsSupervisor)
            .Where(e => e.Grade!.Level >= minLevel)
            .OrderBy(e => e.Department!.DisplayOrder)
            .ThenBy(e => e.Grade!.Level)
            .ToListAsync();

        return approvers;
    }

    public async Task<int> CalculateRequiredLevelsAsync(string docType, decimal amount)
    {
        var chain = await GetApprovalChainAsync(docType, amount);
        return chain.TotalLevels;
    }

    public async Task<bool> CanEmployeeApproveAsync(Guid employeeId, string docType, decimal amount)
    {
        var result = await CheckApprovalAuthorityAsync(employeeId, docType, amount);
        return result.CanApprove;
    }

    public async Task<decimal> GetEmployeeApprovalLimitAsync(Guid employeeId)
    {
        var employee = await _db.Employees
            .Include(e => e.Grade)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee?.Grade == null) return 0;
        return employee.Grade.ApprovalLimit;
    }

    public async Task<Dictionary<string, decimal>> GetAllDocTypeLimitsAsync(Guid gradeId)
    {
        var authorities = await _db.ApprovalAuthorities
            .Where(a => a.IsActive && a.GradeId == gradeId)
            .ToListAsync();

        return authorities.ToDictionary(a => a.DocType, a => a.MaxAmount);
    }
}
