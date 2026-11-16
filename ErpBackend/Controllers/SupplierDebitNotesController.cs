using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[ApiController]
[Authorize]
[Route("api/erp/procurement/debit-notes")]
[Route("api/v1/erp/procurement/debit-notes")]
public sealed class SupplierDebitNotesController : BaseController
{
    private readonly ISupplierDebitNoteService _service;

    public SupplierDebitNotesController(ISupplierDebitNoteService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SupplierDebitNoteDto>>> Create(
        [FromBody] CreateSupplierDebitNoteRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(ApiResponse<SupplierDebitNoteDto>.Fail("缺少有效的使用者識別"));
        try
        {
            var key = Request.Headers["Idempotency-Key"].FirstOrDefault() ?? string.Empty;
            var note = await _service.CreateAsync(request, key, actorId, cancellationToken);
            return Ok(note);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiResponse<SupplierDebitNoteDto>.Fail(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<SupplierDebitNoteDto>.Fail(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiResponse<SupplierDebitNoteDto>.Fail(exception.Message));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SupplierDebitNoteDto>>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var note = await _service.GetAsync(id, cancellationToken);
        return note is null
            ? NotFound(ApiResponse<SupplierDebitNoteDto>.Fail("找不到採購借項單"))
            : Ok(note);
    }

    [HttpPost("{id:guid}/approvals")]
    public async Task<ActionResult<ApiResponse<SupplierDebitNoteDto>>> Approve(
        Guid id,
        [FromBody] ApproveSupplierDebitNoteRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(ApiResponse<SupplierDebitNoteDto>.Fail("缺少有效的使用者識別"));
        try
        {
            var note = await _service.ApproveAsync(
                id, request, actorId, GetRoleClaims(), cancellationToken);
            return Ok(note);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiResponse<SupplierDebitNoteDto>.Fail(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<SupplierDebitNoteDto>.Fail(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiResponse<SupplierDebitNoteDto>.Fail(exception.Message));
        }
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<ApiResponse<SupplierDebitNoteDto>>> Post(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actorId = GetUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(ApiResponse<SupplierDebitNoteDto>.Fail("缺少有效的使用者識別"));
        try
        {
            var note = await _service.PostAsync(id, actorId, GetRoleClaims(), cancellationToken);
            return Ok(note);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiResponse<SupplierDebitNoteDto>.Fail(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<SupplierDebitNoteDto>.Fail(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiResponse<SupplierDebitNoteDto>.Fail(exception.Message));
        }
    }

    private IReadOnlyCollection<string> GetRoleClaims() =>
        User.Claims
            .Where(claim => claim.Type == ClaimTypes.Role || claim.Type == "role")
            .Select(claim => claim.Value)
            .ToArray();
}
