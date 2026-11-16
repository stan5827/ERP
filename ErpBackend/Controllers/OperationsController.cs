using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/operations")]
public class OperationsController : BaseController
{
    private readonly IOperationService _operationService;

    public OperationsController(IOperationService operationService) => _operationService = operationService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<OperationDto>>>> GetOperations()
        => Ok(await _operationService.GetOperationsAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<OperationDto>>> GetOperation(Guid id)
    {
        var dto = await _operationService.GetOperationAsync(id);
        return dto == null ? NotFound(ApiHelper.FailOf<OperationDto>("找不到工序")) : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OperationDto>>> CreateOperation([FromBody] CreateOperationRequest request)
    {
        var (dto, status) = await _operationService.CreateOperationAsync(request);
        if (status == "invalid") return BadRequest(ApiHelper.FailOf<OperationDto>("OperationCode and OperationName are required"));
        if (status == "duplicate") return BadRequest(ApiHelper.FailOf<OperationDto>("Operation code already exists"));
        return Ok(dto!, "Operation created");
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<OperationDto>>> UpdateOperation(Guid id, [FromBody] UpdateOperationRequest request)
    {
        var (dto, status) = await _operationService.UpdateOperationAsync(id, request);
        if (status == "not_found") return NotFound(ApiHelper.FailOf<OperationDto>("找不到工序"));
        return Ok(dto!, "Operation updated");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteOperation(Guid id)
    {
        var (ok, status) = await _operationService.DeleteOperationAsync(id);
        if (status == "not_found") return NotFound(ApiHelper.Fail("找不到工序"));
        if (status == "guarded") return BadRequest(ApiHelper.Fail("該工序已在工藝中使用，無法刪除"));
        return Ok("工序已刪除");
    }
}
