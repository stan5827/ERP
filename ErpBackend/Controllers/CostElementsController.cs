using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class CostElementsController : BaseController
{
    private readonly ICostElementService _costElementService;

    public CostElementsController(ICostElementService costElementService) => _costElementService = costElementService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CostElementDto>>>> GetCostElements(
        [FromQuery] string? search,
        [FromQuery] string? elementType,
        [FromQuery] bool? isActive)
        => Ok(await _costElementService.GetCostElementsAsync(search, elementType, isActive));

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CostElementDto>>> GetCostElement(Guid id)
    {
        var dto = await _costElementService.GetCostElementAsync(id);
        return dto == null ? NotFound(ApiHelper.FailOf<CostElementDto>("Cost element not found")) : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CostElementDto>>> Create([FromBody] CreateCostElementRequest request)
    {
        var (dto, status) = await _costElementService.CreateCostElementAsync(request);
        if (status == "invalid") return BadRequest(ApiHelper.FailOf<CostElementDto>("ElementCode and ElementName are required"));
        if (status == "duplicate") return BadRequest(ApiHelper.FailOf<CostElementDto>("Element code already exists"));
        return Ok(dto!, "Cost element created");
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CostElementDto>>> Update(Guid id, [FromBody] UpdateCostElementRequest request)
    {
        var (dto, status) = await _costElementService.UpdateCostElementAsync(id, request);
        if (status == "not_found") return NotFound(ApiHelper.FailOf<CostElementDto>("Cost element not found"));
        return Ok(dto!, "Cost element updated");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var (ok, status) = await _costElementService.DeleteCostElementAsync(id);
        if (status == "not_found") return NotFound(ApiHelper.Fail("Cost element not found"));
        if (status == "guarded") return BadRequest(ApiHelper.Fail("Cannot delete: cost element assignments exist"));
        return Ok("Cost element deleted");
    }
}
