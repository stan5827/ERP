using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize]
[Route("api/erp/cost-centers")]
[Route("api/v1/erp/cost-centers")]
public class CostCentersController : BaseController
{
    private readonly ICostCenterService _service;

    public CostCentersController(ICostCenterService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CostCenterDto>>>> GetAll([FromQuery] string? search)
        => Ok(await _service.GetAllAsync(search));

    [HttpGet("tree")]
    public async Task<ActionResult<ApiResponse<List<CostCenterDto>>>> GetTree()
        => Ok(await _service.GetTreeAsync());

    [HttpGet("budget")]
    public async Task<ActionResult<ApiResponse<List<CostCenterBudgetDto>>>> GetBudget()
        => Ok(await _service.GetBudgetAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CostCenterDto>>> GetById(Guid id)
    {
        var center = await _service.GetByIdAsync(id);
        return center is null ? NotFound(ApiResponse.Fail("找不到成本中心")) : Ok(center);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CostCenterDto>>> Create([FromBody] SaveCostCenterRequestDto request)
    {
        var (success, status, center) = await _service.CreateAsync(request);
        if (!success)
            return BadRequest(ApiResponse.Fail(GetErrorMessage(status)));
        return Ok(center!);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CostCenterDto>>> Update(
        Guid id,
        [FromBody] SaveCostCenterRequestDto request)
    {
        var (success, status, center) = await _service.UpdateAsync(id, request);
        if (!success)
            return status == "not_found"
                ? NotFound(ApiResponse.Fail("找不到成本中心"))
                : BadRequest(ApiResponse.Fail(GetErrorMessage(status)));
        return Ok(center!);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var (success, status) = await _service.DeleteAsync(id);
        if (!success)
            return status == "not_found"
                ? NotFound(ApiResponse.Fail("找不到成本中心"))
                : Conflict(ApiResponse.Fail(status == "has_children"
                    ? "請先移除或停用下層成本中心"
                    : "成本中心已有成本或會計分錄，無法刪除"));
        return Ok("成本中心已停用");
    }

    private static string GetErrorMessage(string status) => status switch
    {
        "invalid_code" => "成本中心代碼不可空白且最多 20 個字元",
        "invalid_name" => "成本中心名稱不可空白且最多 100 個字元",
        "invalid_type" => "成本中心類型不可空白",
        "invalid_amount" => "預算與占用面積不可為負數",
        "invalid_effective_range" => "生效日期不可晚於失效日期",
        "duplicate_code" => "成本中心代碼已存在",
        "invalid_parent" => "上層成本中心無效",
        "invalid_department" => "關聯部門不存在",
        "invalid_work_center" => "關聯工作中心不存在",
        _ => "成本中心資料無效"
    };
}

