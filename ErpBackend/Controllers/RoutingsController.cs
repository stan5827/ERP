using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/routings")]
public class RoutingsController : BaseController
{
    private readonly IRoutingService _routingService;

    public RoutingsController(IRoutingService routingService) => _routingService = routingService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoutingDto>>>> GetRoutings(
        [FromQuery] Guid? productSkuId,
        [FromQuery] Guid? operationId)
        => Ok(await _routingService.GetRoutingsAsync(productSkuId, operationId));

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RoutingDto>>> GetRouting(Guid id)
    {
        var dto = await _routingService.GetRoutingAsync(id);
        return dto == null ? NotFound(ApiHelper.FailOf<RoutingDto>("找不到工藝")) : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoutingDto>>> CreateRouting([FromBody] CreateRoutingRequest request)
    {
        var (dto, status) = await _routingService.CreateRoutingAsync(request);
        return Ok(dto!);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<RoutingDto>>> UpdateRouting(Guid id, [FromBody] UpdateRoutingRequest request)
    {
        var (dto, status) = await _routingService.UpdateRoutingAsync(id, request);
        if (status == "not_found") return NotFound(ApiHelper.FailOf<RoutingDto>("找不到工藸"));
        return Ok(dto!);
    }
}
