using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class RfqsController : BaseController
{
    private readonly IRfqService _rfqService;

    public RfqsController(IRfqService rfqService) => _rfqService = rfqService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RfqDto>>>> GetRfqs([FromQuery] string? status, [FromQuery] string? searchTerm)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var dtos = await _rfqService.GetAllAsync(status, searchTerm, page, pageSize);
        return Ok(dtos, totalCount: dtos.Count);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RfqDto>>> GetRfq(Guid id)
    {
        var dto = await _rfqService.GetByIdAsync(id);
        return dto == null ? NotFound(ApiHelper.Fail("RFQ not found")) : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RfqDto>>> CreateRfq([FromBody] CreateRfqRequest req)
    {
        var dto = await _rfqService.CreateAsync(req);
        return Ok(dto!, "RFQ created");
    }

    [HttpPost("{id}/responses")]
    public async Task<ActionResult<ApiResponse>> AddSupplierResponse(Guid id, [FromBody] AddRfqResponseRequest req)
    {
        await _rfqService.AddSupplierResponseAsync(id, req);
        return ApiResponse.Ok("Response added");
    }

    /// <summary>Assigns an open RFQ to approved suppliers for supplier-portal access.</summary>
    [HttpPut("{id:guid}/suppliers")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<int>>> AssignSuppliers(
        Guid id,
        [FromBody] AssignRfqSuppliersRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var count = await _rfqService.AssignSuppliersAsync(
                id,
                request,
                GetUserId(),
                cancellationToken);
            return Ok(count, "RFQ 已指派給供應商。");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.Fail(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(Guid id, [FromBody] UpdateRfqStatusRequest req)
    {
        var ok = await _rfqService.UpdateStatusAsync(id, req);
        if (!ok) return NotFound(ApiHelper.Fail("RFQ not found"));
        return ApiResponse.Ok("Status updated");
    }
}
