using Microsoft.AspNetCore.Mvc;
using ErpBackend.Services;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;

namespace ErpBackend.Controllers;

[Route("api/erp/rccp")]
public class RccpResourcesController : BaseController
{
    private readonly IRccpResourceService _rccpService;

    public RccpResourcesController(IRccpResourceService rccpService)
    {
        _rccpService = rccpService;
    }

    // --- Resources ---

    [HttpGet("resources")]
    public async Task<ActionResult<ApiResponse<List<RccpResource>>>> ListResources()
    {
        var list = await _rccpService.ListResourcesAsync();
        return Ok(list);
    }

    [HttpPost("resources")]
    public async Task<ActionResult<ApiResponse<RccpResource>>> CreateResource(RccpResource model)
    {
        var (resource, status) = await _rccpService.CreateResourceAsync(model);

        return status switch
        {
            "ok" => Ok(resource!),
            "duplicate" => ApiResponse<RccpResource>.Fail("Resource code already exists"),
            _ => ApiResponse<RccpResource>.Fail("Invalid resource data")
        };
    }

    [HttpPut("resources/{id}")]
    public async Task<ActionResult<ApiResponse<RccpResource>>> UpdateResource(Guid id, RccpResource model)
    {
        var (resource, status) = await _rccpService.UpdateResourceAsync(id, model);

        return status switch
        {
            "ok" => Ok(resource!),
            "not_found" => ApiResponse<RccpResource>.Fail("Resource not found"),
            _ => ApiResponse<RccpResource>.Fail("Invalid resource data")
        };
    }

    // --- Capacity Bills (Item Resources) ---

    [HttpGet("item-resources/{skuId}")]
    public async Task<ActionResult<ApiResponse<List<RccpItemResource>>>> GetItemResources(Guid skuId)
    {
        var list = await _rccpService.GetItemResourcesAsync(skuId);
        return Ok(list);
    }

    [HttpPost("item-resources")]
    public async Task<ActionResult<ApiResponse>> UpdateItemResources([FromBody] List<RccpItemResource> items)
    {
        var (success, message) = await _rccpService.UpdateItemResourcesAsync(items);

        return success
            ? Ok(message)
            : ApiResponse.Fail(message);
    }
}
