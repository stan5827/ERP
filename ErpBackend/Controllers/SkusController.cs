using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;
using ErpBackend.Validation;

namespace ErpBackend.Controllers;

public class SkusController : BaseController
{
    private readonly ISkuService _skuService;

    public SkusController(ISkuService skuService)
    {
        _skuService = skuService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SkuDto>>>> GetSkus(
        [FromQuery] string? category,
        [FromQuery] string? itemType,
        [FromQuery] string? searchTerm,
        [FromQuery] bool? isPurchasable,
        [FromQuery] bool? isSellable)
    {
        var page = GetPage();
        var pageSize = GetPageSize();
        var result = await _skuService.GetSkusAsync(category, itemType, searchTerm, page, pageSize, isPurchasable, isSellable);
        return Ok(result.Items, totalCount: result.TotalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SkuDto>>> GetSku(Guid id)
    {
        var sku = await _skuService.GetSkuAsync(id);
        if (sku == null) return NotFound(ApiHelper.FailOf<SkuDto>("找不到指定SKU"));
        return Ok(sku);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SkuDto>>> CreateSku([FromBody] SkuDto dto)
    {
        var validationError = ValidateSku(dto);
        if (validationError != null)
            return BadRequest(ApiHelper.FailOf<SkuDto>(validationError));

        var created = await _skuService.CreateSkuAsync(dto);
        return Ok(created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<SkuDto>>> UpdateSku(Guid id, [FromBody] SkuDto dto)
    {
        var validationError = ValidateSku(dto);
        if (validationError != null)
            return BadRequest(ApiHelper.FailOf<SkuDto>(validationError));

        try
        {
            var updated = await _skuService.UpdateSkuAsync(id, dto);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiHelper.FailOf<SkuDto>(ex.Message));
        }
    }

    private string? ValidateSku(SkuDto dto)
    {
        var validationResult = Validator.ValidateObject(dto, _skuService.BuildSkuValidationRules());
        return validationResult.IsValid ? null : validationResult.Errors.Values.First();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteSku(Guid id)
    {
        try
        {
            await _skuService.DeleteSkuAsync(id);
            return Ok("Success");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiHelper.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiHelper.Fail(ex.Message));
        }
    }
}
