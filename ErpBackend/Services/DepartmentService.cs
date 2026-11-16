using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;

namespace ErpBackend.Services;

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetAllAsync(string? search, string? deptType, bool? isActive);
    Task<List<DepartmentDto>> GetFlatAsync();
    Task<List<DepartmentDto>> GetTreeAsync();
    Task<DepartmentDto?> GetByIdAsync(Guid id);
    Task<(DepartmentDto? Dto, string Status)> CreateAsync(DepartmentUpsertDto req);
    Task<(DepartmentDto? Dto, string Status)> UpdateAsync(Guid id, DepartmentUpsertDto req);
    Task<(bool Ok, string Status)> DeleteAsync(Guid id);
}

public class DepartmentService : IDepartmentService
{
    private readonly ErpGeneralDbContext _db;

    public DepartmentService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<DepartmentDto>> GetAllAsync(string? search, string? deptType, bool? isActive)
    {
        var q = _db.Departments.Include(d => d.ParentDept).AsQueryable();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(d => d.DeptName.Contains(search) || d.DeptCode.Contains(search));
        if (!string.IsNullOrEmpty(deptType))
            q = q.Where(d => d.DeptType == deptType);
        if (isActive.HasValue)
            q = q.Where(d => d.IsActive == isActive.Value);

        var list = await q.OrderBy(d => d.DisplayOrder).ThenBy(d => d.DeptCode).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<List<DepartmentDto>> GetFlatAsync()
    {
        var list = await _db.Departments
            .Where(d => d.IsActive)
            .OrderBy(d => d.DisplayOrder).ThenBy(d => d.DeptCode)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<List<DepartmentDto>> GetTreeAsync()
    {
        var all = await _db.Departments
            .Where(d => d.IsActive)
            .OrderBy(d => d.DisplayOrder).ThenBy(d => d.DeptCode)
            .ToListAsync();
        var roots = all.Where(d => d.ParentDeptId == null)
            .Select(d => BuildTree(d, all)).ToList();
        return roots;
    }

    public async Task<DepartmentDto?> GetByIdAsync(Guid id)
    {
        var d = await _db.Departments.Include(x => x.ParentDept).FirstOrDefaultAsync(x => x.Id == id);
        return d == null ? null : ToDto(d);
    }

    public async Task<(DepartmentDto? Dto, string Status)> CreateAsync(DepartmentUpsertDto req)
    {
        if (await _db.Departments.AnyAsync(d => d.DeptCode == req.DeptCode))
            return (null, "duplicate");

        var dept = new Department
        {
            DeptCode       = req.DeptCode,
            DeptName       = req.DeptName,
            DeptType       = req.DeptType ?? "admin",
            ParentDeptId   = req.ParentDeptId,
            CostCenterCode = req.CostCenterCode,
            Description    = req.Description,
            IsActive       = req.IsActive ?? true,
            IsCostCenter   = req.IsCostCenter ?? true,
            BudgetLimit    = req.BudgetLimit,
            DisplayOrder   = req.DisplayOrder ?? 0,
        };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync();
        return (ToDto(dept), "ok");
    }

    public async Task<(DepartmentDto? Dto, string Status)> UpdateAsync(Guid id, DepartmentUpsertDto req)
    {
        var dept = await _db.Departments.FindAsync(id);
        if (dept == null) return (null, "not_found");

        if (await _db.Departments.AnyAsync(d => d.DeptCode == req.DeptCode && d.Id != id))
            return (null, "duplicate");

        dept.DeptCode       = req.DeptCode;
        dept.DeptName       = req.DeptName;
        dept.DeptType       = req.DeptType ?? dept.DeptType;
        dept.ParentDeptId   = req.ParentDeptId;
        dept.CostCenterCode = req.CostCenterCode;
        dept.Description    = req.Description;
        dept.IsActive       = req.IsActive ?? dept.IsActive;
        dept.IsCostCenter   = req.IsCostCenter ?? dept.IsCostCenter;
        dept.BudgetLimit    = req.BudgetLimit;
        dept.DisplayOrder   = req.DisplayOrder ?? dept.DisplayOrder;
        dept.UpdatedAt      = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (ToDto(dept), "ok");
    }

    public async Task<(bool Ok, string Status)> DeleteAsync(Guid id)
    {
        var dept = await _db.Departments.FindAsync(id);
        if (dept == null) return (false, "not_found");
        if (await _db.Departments.AnyAsync(d => d.ParentDeptId == id))
            return (false, "guarded");
        if (await _db.Set<Employee>().AnyAsync(e => e.DepartmentId == id))
            return (false, "guarded");

        _db.Departments.Remove(dept);
        await _db.SaveChangesAsync();
        return (true, "ok");
    }

    private static DepartmentDto ToDto(Department d) => new()
    {
        Id             = d.Id,
        DeptCode       = d.DeptCode,
        DeptName       = d.DeptName,
        DeptType       = d.DeptType,
        ParentDeptId   = d.ParentDeptId,
        ParentDeptName = d.ParentDept?.DeptName,
        CostCenterCode = d.CostCenterCode,
        Description    = d.Description,
        IsActive       = d.IsActive,
        IsCostCenter   = d.IsCostCenter,
        BudgetLimit    = d.BudgetLimit,
        BudgetUsed     = d.BudgetUsed,
        DisplayOrder   = d.DisplayOrder,
        CreatedAt      = d.CreatedAt,
    };

    private static DepartmentDto BuildTree(Department d, List<Department> all)
    {
        var dto = ToDto(d);
        dto.Children = all.Where(x => x.ParentDeptId == d.Id)
            .Select(x => BuildTree(x, all)).ToList();
        return dto;
    }
}
