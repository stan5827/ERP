using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/employees")]
[Authorize]
public class EmployeesController : BaseController
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    // GET /api/erp/employees
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<EmployeeListDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid?   departmentId,
        [FromQuery] string? status,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 50)
    {
        var result = await _employeeService.GetAllAsync(search, departmentId, status, page, pageSize);
        return Ok(result.Items, totalCount: result.TotalCount);
    }

    // GET /api/erp/employees/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailDto>>> Get(Guid id)
    {
        var employee = await _employeeService.GetAsync(id);
        if (employee == null) return NotFound(ApiHelper.Fail("Employee not found"));
        return Ok(employee);
    }

    // POST /api/erp/employees
    [HttpPost]
    public async Task<ActionResult<ApiResponse<EmployeeDetailDto>>> Create([FromBody] EmployeeCreateDto req)
    {
        try
        {
            if (req == null) return BadRequest(ApiHelper.Fail("Invalid request body"));
            var created = await _employeeService.CreateAsync(req);
            return Ok(created, "Employee created");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiHelper.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            var errMsg = ex.InnerException != null ? $"{ex.Message} | {ex.InnerException.Message}" : ex.Message;
            return StatusCode(500, ApiHelper.Fail($"建立員工失敗：{errMsg}"));
        }
    }

    // PUT /api/erp/employees/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailDto>>> Update(Guid id, [FromBody] EmployeeUpdateDto req)
    {
        try
        {
            if (req == null) return BadRequest(ApiHelper.Fail("Invalid request body"));
            var updated = await _employeeService.UpdateAsync(id, req, GetUserId());
            return Ok(updated, "Employee updated");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiHelper.Fail("Employee not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiHelper.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            var errMsg = ex.InnerException != null ? $"{ex.Message} | {ex.InnerException.Message}" : ex.Message;
            return StatusCode(500, ApiHelper.Fail($"更新員工失敗：{errMsg}"));
        }
    }

    // PATCH /api/erp/employees/{id}/terminate
    [HttpPatch("{id:guid}/terminate")]
    public async Task<ActionResult<ApiResponse>> Terminate(Guid id, [FromBody] TerminateRequest req)
    {
        try
        {
            if (req.EffectiveDate == null)
                return BadRequest(ApiHelper.Fail("EffectiveDate is required."));
            await _employeeService.TerminateAsync(id, req.Reason, req.EffectiveDate.Value, GetUserId());
            return Ok("Employee terminated");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiHelper.Fail("Employee not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiHelper.Fail(ex.Message));
        }
    }

    // PATCH /api/erp/employees/{id}/rehire
    [HttpPatch("{id:guid}/rehire")]
    public async Task<ActionResult<ApiResponse>> Rehire(Guid id, [FromBody] EmployeeStatusReasonDto req)
    {
        try
        {
            await _employeeService.RehireAsync(id, req.Reason, GetUserId());
            return Ok("Employee rehired");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiHelper.Fail("Employee not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiHelper.Fail(ex.Message));
        }
    }
}