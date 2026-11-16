using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize]
[Route("api/erp/[controller]")]
public class DepartmentsController : BaseController
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    // GET /api/erp/departments
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? deptType,
        [FromQuery] bool?   isActive)
    {
        var list = await _departmentService.GetAllAsync(search, deptType, isActive);
        return Ok(list);
    }

    // GET /api/erp/departments/flat
    [HttpGet("flat")]
    public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> Flat()
    {
        var list = await _departmentService.GetFlatAsync();
        return Ok(list);
    }

    // GET /api/erp/departments/tree
    [HttpGet("tree")]
    public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> Tree()
    {
        var roots = await _departmentService.GetTreeAsync();
        return Ok(roots);
    }

    // GET /api/erp/departments/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Get(Guid id)
    {
        var dto = await _departmentService.GetByIdAsync(id);
        if (dto == null) return NotFound(ApiHelper.Fail("Department not found"));
        return Ok(dto!);
    }

    // POST /api/erp/departments
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Create([FromBody] DepartmentUpsertDto req)
    {
        var (dto, status) = await _departmentService.CreateAsync(req);
        if (status == "duplicate")
            return BadRequest(ApiHelper.Fail($"Department code {req.DeptCode} already exists"));
        return Ok(dto!, "Department created");
    }

    // PUT /api/erp/departments/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Update(Guid id, [FromBody] DepartmentUpsertDto req)
    {
        var (dto, status) = await _departmentService.UpdateAsync(id, req);
        if (status == "not_found")
            return NotFound(ApiHelper.Fail("Department not found"));
        if (status == "duplicate")
            return BadRequest(ApiHelper.Fail($"Department code {req.DeptCode} already exists"));
        return Ok(dto!, "Department updated");
    }

    // DELETE /api/erp/departments/{id}
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var (ok, status) = await _departmentService.DeleteAsync(id);
        if (status == "not_found")
            return NotFound(ApiHelper.Fail("Department not found"));
        if (!ok)
            return BadRequest(ApiHelper.Fail("Cannot delete: sub-departments or employees still exist"));
        return Ok("Department deleted");
    }
}
