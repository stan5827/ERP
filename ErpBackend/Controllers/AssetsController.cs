using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/[controller]")]
public class AssetsController : BaseController
{
    private readonly IAssetService _assetService;

    public AssetsController(IAssetService assetService)
    {
        _assetService = assetService;
    }

    // GET /api/erp/assets
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AssetDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? categoryCode)
    {
        var list = await _assetService.GetAllAsync(search, status, categoryCode);
        return Ok(list);
    }

    // GET /api/erp/assets/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> Get(Guid id)
    {
        var dto = await _assetService.GetByIdAsync(id);
        if (dto == null) return NotFound(ApiHelper.Fail<AssetDto>("Asset not found"));
        return Ok(dto!);
    }

    // POST /api/erp/assets
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AssetDto>>> Create([FromBody] AssetUpsertDto req)
    {
        var (dto, status) = await _assetService.CreateAsync(req);
        if (status == "duplicate")
            return BadRequest(ApiHelper.Fail<AssetDto>($"Asset number {req.AssetNumber} already exists"));
        if (status == "category_not_found")
            return BadRequest(ApiHelper.Fail<AssetDto>("Asset category not found"));
        return Ok(dto!, "Asset created");
    }

    // PUT /api/erp/assets/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> Update(Guid id, [FromBody] AssetUpsertDto req)
    {
        var (dto, status) = await _assetService.UpdateAsync(id, req);
        if (status == "not_found")
            return NotFound(ApiHelper.Fail<AssetDto>("Asset not found"));
        if (status == "duplicate")
            return BadRequest(ApiHelper.Fail<AssetDto>($"Asset number {req.AssetNumber} already exists"));
        if (status == "category_not_found")
            return BadRequest(ApiHelper.Fail<AssetDto>("Asset category not found"));
        return Ok(dto!, "Asset updated");
    }

    // DELETE /api/erp/assets/{id}
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var (ok, status) = await _assetService.DeleteAsync(id);
        if (status == "not_found")
            return NotFound(ApiHelper.Fail("Asset not found"));
        return Ok("Asset deleted");
    }

    // GET /api/erp/assets/{id}/maintenance
    [HttpGet("{id:guid}/maintenance")]
    public async Task<ActionResult<ApiResponse<List<AssetMaintenanceDto>>>> GetMaintenance(Guid id)
    {
        var list = await _assetService.GetMaintenanceAsync(id);
        return Ok(list);
    }

    // POST /api/erp/assets/{id}/maintenance
    [HttpPost("{id:guid}/maintenance")]
    public async Task<ActionResult<ApiResponse<AssetMaintenanceDto>>> AddMaintenance(Guid id, [FromBody] AssetMaintenanceUpsertDto req)
    {
        var (dto, status) = await _assetService.AddMaintenanceAsync(id, req);
        if (status == "not_found")
            return NotFound(ApiHelper.Fail<AssetMaintenanceDto>("Asset not found"));
        return Ok(dto!, "Maintenance record added");
    }

    // POST /api/erp/assets/depreciation/run
    [HttpPost("depreciation/run")]
    public async Task<ActionResult<ApiResponse<DepreciationResultDto>>> RunDepreciation(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var result = await _assetService.RunDepreciationAsync(year, month);
        return Ok(result, "Depreciation calculated");
    }

    // GET /api/erp/assets/{id}/depreciation
    [HttpGet("{id:guid}/depreciation")]
    public async Task<ActionResult<ApiResponse<List<DepreciationRecordDto>>>> GetDepreciation(Guid id)
    {
        var list = await _assetService.GetDepreciationHistoryAsync(id);
        return Ok(list);
    }
}
