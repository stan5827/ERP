using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Filters;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class UomsController : BaseController
{
    private readonly IUomService _uomService;
    private readonly IUomConversionService _conversionService;

    public UomsController(IUomService uomService, IUomConversionService conversionService)
    {
        _uomService = uomService;
        _conversionService = conversionService;
    }

    [HttpGet]
    [PermissionAuthorize("inventory", "view", "uom:view")]
    public async Task<ActionResult<ApiResponse<List<UomDto>>>> GetUoms(
        [FromQuery] string? uomType,
        [FromQuery] bool? isActive,
        [FromQuery] string? searchTerm)
    {
        var items = await _uomService.GetUomsAsync(uomType, isActive, searchTerm);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UomDto>>> GetUom(Guid id)
    {
        var dto = await _uomService.GetUomAsync(id);
        if (dto == null) return NotFound(ApiHelper.Fail("Error"));

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UomDto>>> CreateUom([FromBody] CreateUomDto model)
    {
        var (dto, status) = await _uomService.CreateUomAsync(model);
        return status switch
        {
            "ok" => Ok(dto!, "Created"),
            "duplicate" => BadRequest(ApiHelper.FailOf<UomDto>("單位代碼已存在")),
            "invalid" => BadRequest(ApiHelper.FailOf<UomDto>("單位代碼不可空白")),
            _ => BadRequest(ApiHelper.FailOf<UomDto>("建立單位失敗"))
        };
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UomDto>>> UpdateUom(Guid id, [FromBody] UpdateUomDto model)
    {
        var (dto, status) = await _uomService.UpdateUomAsync(id, model);
        return status switch
        {
            "ok" => Ok(dto!, "Updated"),
            "not_found" => NotFound(ApiHelper.FailOf<UomDto>("找不到指定單位")),
            "duplicate" => BadRequest(ApiHelper.FailOf<UomDto>("單位代碼已存在")),
            "invalid" => BadRequest(ApiHelper.FailOf<UomDto>("單位代碼不可空白")),
            _ => BadRequest(ApiHelper.FailOf<UomDto>("更新單位失敗"))
        };
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteUom(Guid id)
    {
        var (_, status) = await _uomService.DeleteUomAsync(id);
        return status switch
        {
            "ok" => Ok("Deleted"),
            "not_found" => NotFound(ApiHelper.Fail("找不到指定單位")),
            "has_conversions" => BadRequest(ApiHelper.Fail("該單位已有換算關係，無法刪除")),
            _ => BadRequest(ApiHelper.Fail("刪除單位失敗"))
        };
    }

    [HttpGet("conversions")]
    public async Task<ActionResult<ApiResponse<List<UomConversionDto>>>> GetConversions(
        [FromQuery] bool? isActive, [FromQuery] Guid? skuId)
    {
        var items = await _conversionService.GetConversionsAsync(isActive, skuId);
        return Ok(items);
    }

    [HttpPost("conversions")]
    public async Task<ActionResult<ApiResponse<UomConversionDto>>> CreateConversion([FromBody] CreateUomConversionDto model)
    {
        var (dto, status) = await _conversionService.CreateConversionAsync(model);
        return status switch
        {
            "ok" => Ok(dto!, "Created"),
            "invalid" => BadRequest(ApiHelper.FailOf<UomConversionDto>("來源單位與目標單位不可相同")),
            "not_found" => NotFound(ApiHelper.FailOf<UomConversionDto>("單位不存在")),
            "duplicate" => BadRequest(ApiHelper.FailOf<UomConversionDto>("換算關係已存在")),
            _ => BadRequest(ApiHelper.FailOf<UomConversionDto>("建立換算關係失敗"))
        };
    }

    [HttpPut("conversions/{id}")]
    public async Task<ActionResult<ApiResponse<UomConversionDto>>> UpdateConversion(Guid id, [FromBody] UpdateUomConversionDto model)
    {
        var (dto, status) = await _conversionService.UpdateConversionAsync(id, model);
        return status switch
        {
            "ok" => Ok(dto!, "Updated"),
            "not_found" => NotFound(ApiHelper.FailOf<UomConversionDto>("找不到指定換算關係")),
            "invalid" => BadRequest(ApiHelper.FailOf<UomConversionDto>("來源單位與目標單位不可相同")),
            "duplicate" => BadRequest(ApiHelper.FailOf<UomConversionDto>("換算關係已存在")),
            _ => BadRequest(ApiHelper.FailOf<UomConversionDto>("更新換算關係失敗"))
        };
    }

    [HttpDelete("conversions/{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteConversion(Guid id)
    {
        var (_, status) = await _conversionService.DeleteConversionAsync(id);
        return status switch
        {
            "ok" => Ok("Deleted"),
            "not_found" => NotFound(ApiHelper.Fail("找不到指定換算關係")),
            _ => BadRequest(ApiHelper.Fail("刪除換算關係失敗"))
        };
    }

    [HttpGet("convert")]
    public async Task<ActionResult<ApiResponse<UomConversionResult>>> Convert(
        [FromQuery] Guid skuId, [FromQuery] decimal qty, [FromQuery] string fromUom, [FromQuery] string toUom)
    {
        var result = await _conversionService.ConvertAsync(skuId, qty, fromUom, toUom);
        return Ok(result);
    }

    [HttpGet("to-base")]
    public async Task<IActionResult> ToBase(
        [FromQuery] Guid skuId, [FromQuery] decimal qty, [FromQuery] string uom)
    {
        var baseQty = await _conversionService.ConvertToBaseUomAsync(skuId, qty, uom);
        return new OkObjectResult(new { qty, uom, baseQty });
    }
}
