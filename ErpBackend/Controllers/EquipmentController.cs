using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/equipment")]
public class EquipmentController : BaseController
{
    private readonly IEquipmentService _equipmentService;

    public EquipmentController(IEquipmentService equipmentService) => _equipmentService = equipmentService;

    [HttpGet("equipment")]
    public async Task<ActionResult<ApiResponse<List<EquipmentDto>>>> GetEquipment(
        [FromQuery] string? status,
        [FromQuery] string? type,
        CancellationToken cancellationToken)
    {
        var equipment = await _equipmentService.GetEquipmentAsync(status, type, cancellationToken);
        return Ok(equipment, totalCount: equipment.Count);
    }

    [HttpGet("equipment/{id:guid}", Name = nameof(GetEquipmentById))]
    public async Task<ActionResult<ApiResponse<EquipmentDto>>> GetEquipmentById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var equipment = await _equipmentService.GetEquipmentAsync(id, cancellationToken);
        return equipment == null
            ? NotFound(ApiResponse<EquipmentDto>.Fail("設備不存在"))
            : Ok(equipment);
    }

    [HttpPost("equipment")]
    public async Task<ActionResult<ApiResponse<EquipmentDto>>> CreateEquipment(
        [FromBody] CreateEquipmentRequestDto request,
        CancellationToken cancellationToken)
    {
        var (equipment, status) = await _equipmentService.CreateEquipmentAsync(request, cancellationToken);
        if (status == "duplicate")
            return Conflict(ApiResponse<EquipmentDto>.Fail("設備編號已存在"));
        if (status == "work_center_not_found")
            return BadRequest(ApiResponse<EquipmentDto>.Fail("所屬工段不存在或已停用"));
        if (status == "supplier_not_found")
            return BadRequest(ApiResponse<EquipmentDto>.Fail("供應商不存在"));
        if (status != "ok" || equipment == null)
            return BadRequest(ApiResponse<EquipmentDto>.Fail("設備編號與設備名稱為必填欄位"));

        return CreatedAtAction(
            nameof(GetEquipmentById),
            new { id = equipment.Id },
            ApiResponse<EquipmentDto>.Ok(equipment, "設備建立成功"));
    }

    [HttpPut("equipment/{id:guid}")]
    public async Task<ActionResult<ApiResponse<EquipmentDto>>> UpdateEquipment(
        Guid id,
        [FromBody] UpdateEquipmentRequestDto request,
        CancellationToken cancellationToken)
    {
        var (equipment, status) = await _equipmentService.UpdateEquipmentAsync(id, request, cancellationToken);
        if (status == "not_found")
            return NotFound(ApiResponse<EquipmentDto>.Fail("設備不存在"));
        if (status == "work_center_not_found")
            return BadRequest(ApiResponse<EquipmentDto>.Fail("所屬工段不存在或已停用"));
        if (status == "supplier_not_found")
            return BadRequest(ApiResponse<EquipmentDto>.Fail("供應商不存在"));

        return Ok(equipment!);
    }

    [HttpDelete("equipment/{id:guid}")]
    public async Task<IActionResult> DeleteEquipment(Guid id, CancellationToken cancellationToken)
    {
        var status = await _equipmentService.DeleteEquipmentAsync(id, cancellationToken);
        return status == "not_found" ? NotFound() : NoContent();
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<EquipmentStatsDto>>> GetStats(CancellationToken cancellationToken)
        => Ok(await _equipmentService.GetStatsAsync(cancellationToken));
}
