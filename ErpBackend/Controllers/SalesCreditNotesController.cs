using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[ApiController]
[Authorize]
[Route("api/erp/sales/credit-notes")]
[Route("api/v1/erp/sales/credit-notes")]
public sealed class SalesCreditNotesController : BaseController
{
    private readonly ISalesCreditNoteService _service;

    public SalesCreditNotesController(ISalesCreditNoteService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SalesCreditNoteDto>>> Create(
        [FromBody] CreateSalesCreditNoteRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(ApiResponse<SalesCreditNoteDto>.Fail("缺少有效的使用者識別"));
        try
        {
            var key = Request.Headers["Idempotency-Key"].FirstOrDefault() ?? string.Empty;
            var note = await _service.CreateAsync(request, key, actorId, cancellationToken);
            return Ok(note);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiResponse<SalesCreditNoteDto>.Fail(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<SalesCreditNoteDto>.Fail(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiResponse<SalesCreditNoteDto>.Fail(exception.Message));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SalesCreditNoteDto>>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var note = await _service.GetAsync(id, cancellationToken);
        return note is null
            ? NotFound(ApiResponse<SalesCreditNoteDto>.Fail("找不到銷貨折讓單"))
            : Ok(note);
    }

    [HttpPost("{id:guid}/approvals")]
    public async Task<ActionResult<ApiResponse<SalesCreditNoteDto>>> Approve(
        Guid id,
        [FromBody] ApproveSalesCreditNoteRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(ApiResponse<SalesCreditNoteDto>.Fail("缺少有效的使用者識別"));
        try
        {
            var note = await _service.ApproveAsync(
                id, request, actorId, GetRoleClaims(), cancellationToken);
            return Ok(note);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiResponse<SalesCreditNoteDto>.Fail(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<SalesCreditNoteDto>.Fail(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiResponse<SalesCreditNoteDto>.Fail(exception.Message));
        }
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<ApiResponse<SalesCreditNoteDto>>> Post(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actorId = GetUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(ApiResponse<SalesCreditNoteDto>.Fail("缺少有效的使用者識別"));
        try
        {
            var note = await _service.PostAsync(id, actorId, GetRoleClaims(), cancellationToken);
            return Ok(note);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiResponse<SalesCreditNoteDto>.Fail(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<SalesCreditNoteDto>.Fail(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiResponse<SalesCreditNoteDto>.Fail(exception.Message));
        }
    }

    private IReadOnlyCollection<string> GetRoleClaims() =>
        User.Claims
            .Where(claim => claim.Type == ClaimTypes.Role || claim.Type == "role")
            .Select(claim => claim.Value)
            .ToArray();
}
