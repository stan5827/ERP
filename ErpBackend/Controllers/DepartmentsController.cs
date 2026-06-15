using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;

namespace ErpBackend.Controllers;

public class DepartmentsController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public DepartmentsController(ErpGeneralDbContext db) { _db = db; }

    // GET /api/erp/departments
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? deptType,
        [FromQuery] bool?   isActive)
    {
        var q = _db.Departments.Include(d => d.ParentDept).AsQueryable();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(d => d.DeptName.Contains(search) || d.DeptCode.Contains(search));
        if (!string.IsNullOrEmpty(deptType))
            q = q.Where(d => d.DeptType == deptType);
        if (isActive.HasValue)
            q = q.Where(d => d.IsActive == isActive.Value);

        var list = await q.OrderBy(d => d.DisplayOrder).ThenBy(d => d.DeptCode).ToListAsync();
        return Ok(list.Select(ToDto).ToList());
    }

    // GET /api/erp/departments/flat
    [HttpGet("flat")]
    public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> Flat([FromQuery] bool includeInactive = false)
    {
        var query = _db.Departments.AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(d => d.IsActive);
        }

        var list = await query.OrderBy(d => d.DisplayOrder).ThenBy(d => d.DeptCode).ToListAsync();
        return Ok(list.Select(ToDto).ToList());
    }

    // GET /api/erp/departments/tree
    [HttpGet("tree")]
    public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> Tree([FromQuery] bool includeInactive = false)
    {
        var query = _db.Departments.AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(d => d.IsActive);
        }

        var all = await query.OrderBy(d => d.DisplayOrder).ThenBy(d => d.DeptCode).ToListAsync();
        var roots = all.Where(d => d.ParentDeptId == null)
            .Select(d => BuildTree(d, all)).ToList();
        return Ok(roots);
    }

    // GET /api/erp/departments/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Get(Guid id)
    {
        var d = await _db.Departments.Include(x => x.ParentDept).FirstOrDefaultAsync(x => x.Id == id);
        if (d == null) return NotFound(ApiHelper.Fail("Department not found"));
        return Ok(ToDto(d));
    }

    // POST /api/erp/departments
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Create([FromBody] DepartmentUpsertDto req)
    {
        if (await _db.Departments.AnyAsync(d => d.DeptCode == req.DeptCode))
            return BadRequest(ApiHelper.Fail($"Department code {req.DeptCode} already exists"));

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
        return Ok(ToDto(dept), "Department created");
    }

    // PUT /api/erp/departments/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Update(Guid id, [FromBody] DepartmentUpsertDto req)
    {
        var dept = await _db.Departments.FindAsync(id);
        if (dept == null) return NotFound(ApiHelper.Fail("Department not found"));

        if (await _db.Departments.AnyAsync(d => d.DeptCode == req.DeptCode && d.Id != id))
            return BadRequest(ApiHelper.Fail($"Department code {req.DeptCode} already exists"));

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
        return Ok(ToDto(dept), "Department updated");
    }

    // DELETE /api/erp/departments/{id}
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var dept = await _db.Departments.FindAsync(id);
        if (dept == null) return NotFound(ApiHelper.Fail("Department not found"));
        if (await _db.Departments.AnyAsync(d => d.ParentDeptId == id))
            return BadRequest(ApiHelper.Fail("Cannot delete: sub-departments exist"));
        if (await _db.Set<Employee>().AnyAsync(e => e.DepartmentId == id))
            return BadRequest(ApiHelper.Fail("Cannot delete: employees still assigned"));
        _db.Departments.Remove(dept);
        await _db.SaveChangesAsync();
        return Ok("Department deleted");
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

public class DepartmentDto
{
    public Guid    Id             { get; set; }
    public string  DeptCode       { get; set; } = "";
    public string  DeptName       { get; set; } = "";
    public string? DeptType       { get; set; }
    public Guid?   ParentDeptId   { get; set; }
    public string? ParentDeptName { get; set; }
    public string? CostCenterCode { get; set; }
    public string? Description    { get; set; }
    public bool    IsActive       { get; set; }
    public bool    IsCostCenter   { get; set; }
    public decimal? BudgetLimit   { get; set; }
    public decimal? BudgetUsed    { get; set; }
    public int     DisplayOrder   { get; set; }
    public DateTime CreatedAt     { get; set; }
    public List<DepartmentDto> Children { get; set; } = new();
}

public class DepartmentUpsertDto
{
    public string  DeptCode       { get; set; } = null!;
    public string  DeptName       { get; set; } = null!;
    public string? DeptType       { get; set; }
    public Guid?   ParentDeptId   { get; set; }
    public string? CostCenterCode { get; set; }
    public string? Description    { get; set; }
    public bool?   IsActive       { get; set; }
    public bool?   IsCostCenter   { get; set; }
    public decimal? BudgetLimit   { get; set; }
    public int?    DisplayOrder   { get; set; }
}
