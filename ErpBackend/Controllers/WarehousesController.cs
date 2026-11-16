using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class WarehousesController : BaseController
{
    private readonly IWarehouseService _warehouseService;

    public WarehousesController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<WarehouseDto>>>> GetWarehouses([FromQuery] bool includeInactive = false)
    {
        var dtos = await _warehouseService.GetWarehousesAsync(includeInactive);
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> CreateWarehouse([FromBody] CreateWarehouseRequestDto request)
    {
        var (dto, status) = await _warehouseService.CreateWarehouseAsync(request);
        if (status == "invalid")
            return BadRequest(ApiHelper.FailOf<WarehouseDto>("倉庫代碼與名稱為必填"));
        if (status == "duplicate")
            return BadRequest(ApiHelper.FailOf<WarehouseDto>("倉庫代碼已存在"));
        return Ok(dto!, "Created");
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> UpdateWarehouse(Guid id, [FromBody] UpdateWarehouseRequestDto request)
    {
        var (dto, status) = await _warehouseService.UpdateWarehouseAsync(id, request);
        if (status == "not_found")
            return NotFound(ApiHelper.FailOf<WarehouseDto>("找不到倉庫"));
        if (status == "duplicate")
            return BadRequest(ApiHelper.FailOf<WarehouseDto>("倉庫代碼已存在"));
        if (status == "invalid")
            return BadRequest(ApiHelper.FailOf<WarehouseDto>("倉庫代碼與名稱不可為空白"));
        return Ok(dto!, "Updated");
    }
}
