using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class QuotesController : BaseController
{
    private readonly IQuoteService _quoteService;

    public QuotesController(IQuoteService quoteService) => _quoteService = quoteService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<QuoteDto>>>> GetQuotes([FromQuery] Guid? customerId)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var dtos = await _quoteService.GetQuotesAsync(customerId, page, pageSize);
        return Ok(dtos, totalCount: dtos.Count);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<QuoteDto>>> CreateQuote(
        [FromBody] CreateQuoteRequest req,
        CancellationToken cancellationToken)
    {
        var dto = await _quoteService.CreateAsync(req, cancellationToken);
        if (dto == null) return BadRequest(ApiHelper.Fail("Error"));
        return Ok(dto, "價格已建立");
    }

    [HttpPatch("{id}/approve")]
    public async Task<ActionResult<ApiResponse<QuoteDto>>> ApproveQuote(Guid id)
    {
        var (found, status) = await _quoteService.ApproveQuoteAsync(id);
        if (!found) return BadRequest(ApiHelper.Fail("not found"));
        if (status == "invalid_state") return BadRequest(ApiHelper.Fail("Error"));
        return Ok(new QuoteDto { Status = "approved" }, "價格已審核");
    }

    [HttpPatch("{id}/reject")]
    public async Task<ActionResult<ApiResponse<QuoteDto>>> RejectQuote(Guid id)
    {
        var (found, status) = await _quoteService.RejectQuoteAsync(id);
        if (!found) return BadRequest(ApiHelper.Fail("not found"));
        if (status == "invalid_state") return BadRequest(ApiHelper.Fail("Error"));
        return Ok(new QuoteDto { Status = "rejected" }, "價格已作廢");
    }

    [HttpPatch("{id}/convert-to-order")]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> ConvertToOrder(Guid id)
    {
        var (dto, status) = await _quoteService.ConvertToOrderAsync(id);
        return status switch
        {
            "not_found" => BadRequest(ApiHelper.Fail("not found")),
            "not_approved" => BadRequest(ApiHelper.Fail("Error")),
            "expired" => BadRequest(ApiHelper.Fail("Error")),
            "error" => StatusCode(500, ApiHelper.Fail("Error")),
            _ => Ok(dto!, "Success")
        };
    }
}
