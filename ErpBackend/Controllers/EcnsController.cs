using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class EcnsController : BaseController
{
    private readonly IEcnService _ecnService;

    public EcnsController(IEcnService ecnService) => _ecnService = ecnService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<EcnDto>>>> GetEcns([FromQuery] string? status)
        => Ok(await _ecnService.GetEcnsAsync(status));

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<EcnDto>>> GetEcn(Guid id)
    {
        var dto = await _ecnService.GetEcnAsync(id);
        if (dto == null) return NotFound(ApiResponse<string>.Fail("ECN not found"));
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateEcn([FromBody] CreateEcnRequest req)
    {
        var id = await _ecnService.CreateEcnAsync(req);
        return Ok(id);
    }

    [HttpGet("{id}/impact")]
    public ActionResult<ApiResponse<EcnImpactDto>> GetEcnImpact(Guid id)
    {
        var impact = _ecnService.GetEcnImpact(id);
        return impact == null ? NotFound(ApiHelper.Fail("ECN not found")) : Ok(impact);
    }
}
