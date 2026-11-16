using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/currencies")]
public class CurrenciesController : BaseController
{
    private readonly ICurrencyMasterService _currencyService;

    public CurrenciesController(ICurrencyMasterService currencyService) => _currencyService = currencyService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CurrencyDto>>>> List()
        => Ok(await _currencyService.GetCurrenciesAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CurrencyDto>>> Get(Guid id)
    {
        var dto = await _currencyService.GetCurrencyAsync(id);
        return dto == null ? NotFound(ApiHelper.FailOf<CurrencyDto>("Currency not found")) : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CurrencyDto>>> Create([FromBody] CurrencyDto model)
    {
        var (dto, status) = await _currencyService.CreateCurrencyAsync(model);
        return Ok(dto!);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CurrencyDto>>> Update(Guid id, [FromBody] CurrencyDto model)
    {
        var (dto, status) = await _currencyService.UpdateCurrencyAsync(id, model);
        if (status == "not_found") return NotFound(ApiHelper.FailOf<CurrencyDto>("Currency not found"));
        return Ok(dto!);
    }
}
