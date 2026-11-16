using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/bin-locations")]
[Route("api/erp/BinLocations")]
public class BinLocationsController : BaseController
{
    private readonly IWarehouseService _warehouseService;
    public BinLocationsController(IWarehouseService warehouseService) { _warehouseService = warehouseService; }

    [HttpGet]
    public async Task<dynamic> Get()
    {
        var data = await _warehouseService.GetAllBinLocationsAsync();
        return Ok(data);
    }

    [HttpGet("warehouse/{warehouseId:guid}")]
    public async Task<dynamic> GetByWarehouse(Guid warehouseId)
    {
        var data = await _warehouseService.GetBinLocationsAsync(warehouseId);
        return Ok(data);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BinLocation>>> Create([FromBody] CreateBinLocationRequestDto request)
    {
        var (binLocation, status) = await _warehouseService.CreateBinLocationAsync(request);
        if (status == "invalid")
            return BadRequest(ApiHelper.FailOf<BinLocation>("請選擇倉庫並輸入儲位代碼"));
        if (status == "warehouse_not_found")
            return BadRequest(ApiHelper.FailOf<BinLocation>("找不到啟用中的倉庫"));
        if (status == "duplicate")
            return BadRequest(ApiHelper.FailOf<BinLocation>("此倉庫已存在相同儲位代碼"));
        return Ok(binLocation!, "Created");
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BinLocation>>> Update(Guid id, [FromBody] UpdateBinLocationRequestDto request)
    {
        var (binLocation, status) = await _warehouseService.UpdateBinLocationAsync(id, request);
        if (status == "not_found")
            return NotFound(ApiHelper.FailOf<BinLocation>("找不到儲位"));
        if (status == "invalid")
            return BadRequest(ApiHelper.FailOf<BinLocation>("請選擇倉庫並輸入儲位代碼"));
        if (status == "warehouse_not_found")
            return BadRequest(ApiHelper.FailOf<BinLocation>("找不到啟用中的倉庫"));
        if (status == "duplicate")
            return BadRequest(ApiHelper.FailOf<BinLocation>("此倉庫已存在相同儲位代碼"));
        return Ok(binLocation!, "Updated");
    }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "BinLocationsController restored." });
}
