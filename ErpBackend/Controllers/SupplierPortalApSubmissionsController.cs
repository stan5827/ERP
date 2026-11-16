using System.Globalization;
using ErpBackend.DTOs;
using ErpBackend.Infrastructure.CapaAttachments;
using ErpBackend.Security.SupplierPortal;
using ErpBackend.Services;
using ErpBackend.Services.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/supplier-portal/ap-submissions")]
[Authorize(Policy = SupplierPortalAuthentication.Policy)]
public sealed class SupplierPortalApSubmissionsController(
    SupplierPortalIdentityContext identityContext,
    ISupplierApSubmissionService submissionService,
    ISupplierApSubmissionAttachmentService attachmentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(SupplierApSubmissionPageDto), 200)]
    public async Task<ActionResult<SupplierApSubmissionPageDto>> List(
        [FromQuery] string? status,
        [FromQuery] string? invoiceDateFrom,
        [FromQuery] string? invoiceDateTo,
        [FromQuery] string? submissionNumber,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (identityContext.Current is not { } supplier)
            return Forbid();
        if (!TryParseDate(invoiceDateFrom, out var from)
            || !TryParseDate(invoiceDateTo, out var to)
            || (from.HasValue && to.HasValue && from > to))
            return BadRequest(new { message = "發票日期範圍格式無效。" });
        return Ok(await submissionService.GetSupplierSubmissionsAsync(
            supplier.SupplierId,
            status,
            from,
            to,
            submissionNumber,
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            cancellationToken));
    }

    [HttpGet("eligible-sources")]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierApEligibleSourceDto>), 200)]
    public async Task<ActionResult<IReadOnlyList<SupplierApEligibleSourceDto>>> GetEligibleSources(
        [FromQuery] Guid? purchaseOrderId,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplier)
            return Forbid();
        return Ok(await submissionService.GetEligibleSourcesAsync(
            supplier.SupplierId,
            purchaseOrderId,
            cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(SupplierApSubmissionResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierApSubmissionResultDto>> CreateDraft(
        SupplierApSubmissionCreateRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplier)
            return Forbid();
        try
        {
            var result = await submissionService.CreateDraftAsync(
                supplier.SupplierId,
                supplier.SupplierUserId,
                supplier.Role,
                idempotencyKey ?? string.Empty,
                request,
                cancellationToken);
            SetEntityTag(result.Version);
            return CreatedAtAction(nameof(Get), new { submissionId = result.SubmissionId }, result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (SupplierApSubmissionPolicyException exception)
        {
            return UnprocessableEntity(new { message = exception.Message });
        }
    }

    [HttpGet("{submissionId:guid}")]
    [ProducesResponseType(typeof(SupplierApSubmissionDetailDto), 200)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierApSubmissionDetailDto>> Get(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplier)
            return Forbid();
        var result = await submissionService.GetSupplierSubmissionAsync(
            submissionId,
            supplier.SupplierId,
            cancellationToken);
        if (result is null)
            return NotFound();
        SetEntityTag(result.Version);
        return Ok(result);
    }

    [HttpPut("{submissionId:guid}/draft")]
    [ProducesResponseType(typeof(SupplierApSubmissionResultDto), 200)]
    public async Task<ActionResult<SupplierApSubmissionResultDto>> UpdateDraft(
        Guid submissionId,
        SupplierApSubmissionDraftRequestDto request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplier)
            return Forbid();
        if (!TryParseVersion(ifMatch, out var version))
            return BadRequest(new { message = "If-Match 必須包含有效的案件版本。" });
        try
        {
            var result = await submissionService.UpdateDraftAsync(
                submissionId,
                supplier.SupplierId,
                supplier.SupplierUserId,
                supplier.Role,
                version,
                request,
                cancellationToken);
            SetEntityTag(result.Version);
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (SupplierApSubmissionPolicyException exception)
        {
            return UnprocessableEntity(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPost("{submissionId:guid}/submit")]
    [ProducesResponseType(typeof(SupplierApSubmissionResultDto), 200)]
    public async Task<ActionResult<SupplierApSubmissionResultDto>> Submit(
        Guid submissionId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplier)
            return Forbid();
        if (!TryParseVersion(ifMatch, out var version))
            return BadRequest(new { message = "If-Match 必須包含有效的案件版本。" });
        try
        {
            var result = await submissionService.SubmitAsync(
                submissionId,
                supplier.SupplierId,
                supplier.SupplierUserId,
                supplier.Role,
                version,
                cancellationToken);
            SetEntityTag(result.Version);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (SupplierApSubmissionPolicyException exception)
        {
            return UnprocessableEntity(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPost("{submissionId:guid}/cancel")]
    [ProducesResponseType(typeof(SupplierApSubmissionResultDto), 200)]
    public async Task<ActionResult<SupplierApSubmissionResultDto>> Cancel(
        Guid submissionId,
        [FromBody] SupplierApSubmissionCancelRequestDto? request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplier)
            return Forbid();
        if (!TryParseVersion(ifMatch, out var version))
            return BadRequest(new { message = "If-Match 必須包含有效的案件版本。" });
        try
        {
            var result = await submissionService.CancelAsync(
                submissionId,
                supplier.SupplierId,
                supplier.SupplierUserId,
                supplier.Role,
                version,
                request?.Reason,
                cancellationToken);
            SetEntityTag(result.Version);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpGet("{submissionId:guid}/attachments")]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierApSubmissionAttachmentDto>), 200)]
    public async Task<ActionResult<IReadOnlyList<SupplierApSubmissionAttachmentDto>>> GetAttachments(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplier)
            return Forbid();
        try
        {
            return Ok(await attachmentService.GetSupplierAttachmentsAsync(
                submissionId,
                supplier.SupplierId,
                cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{submissionId:guid}/attachments")]
    [RequestSizeLimit(21 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 21 * 1024 * 1024)]
    [ProducesResponseType(typeof(SupplierApSubmissionAttachmentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SupplierApSubmissionAttachmentDto>> UploadAttachment(
        Guid submissionId,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplier)
            return Forbid();
        if (file is null)
            return BadRequest(new { message = "請選擇附件檔案。" });
        try
        {
            await using var stream = file.OpenReadStream();
            var result = await attachmentService.UploadSupplierAttachmentAsync(
                submissionId,
                supplier.SupplierId,
                supplier.SupplierUserId,
                supplier.Role,
                file.FileName,
                file.Length,
                stream,
                cancellationToken);
            return result.ScanStatus == "clean"
                ? StatusCode(StatusCodes.Status201Created, result)
                : UnprocessableEntity(new { attachment = result, message = "附件未通過安全掃描。" });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (CapaAttachmentScannerUnavailableException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = exception.Message });
        }
        catch (CapaAttachmentConfigurationException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = exception.Message });
        }
    }

    [HttpDelete("{submissionId:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAttachment(
        Guid submissionId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplier)
            return Forbid();
        try
        {
            await attachmentService.RevokeSupplierAttachmentAsync(
                submissionId,
                attachmentId,
                supplier.SupplierId,
                supplier.Role,
                cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpGet("{submissionId:guid}/attachments/{attachmentId:guid}/content")]
    public async Task<IActionResult> DownloadAttachment(
        Guid submissionId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplier)
            return Forbid();
        var download = await attachmentService.GetSupplierDownloadAsync(
            submissionId,
            attachmentId,
            supplier.SupplierId,
            cancellationToken);
        return download is null
            ? NotFound()
            : File(download.Content, download.Metadata.ContentType, download.Metadata.OriginalFileName);
    }

    private static bool TryParseDate(string? value, out DateOnly? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;
        if (!DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
            return false;
        date = parsed;
        return true;
    }

    private static bool TryParseVersion(string? value, out int version)
    {
        version = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var normalized = value.Trim();
        if (normalized.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[2..].Trim();
        normalized = normalized.Trim('"');
        return int.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out version)
            && version > 0;
    }

    private void SetEntityTag(int version) =>
        Response.Headers["ETag"] = $"\"{version}\"";
}
