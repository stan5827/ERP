using System.Globalization;
using System.Security.Claims;
using ErpBackend.DTOs;
using ErpBackend.Filters;
using ErpBackend.Infrastructure.CapaAttachments;
using ErpBackend.Services;
using ErpBackend.Services.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

[ApiController]
[Authorize]
[Route("api/erp/supplier-ap-submissions")]
[PermissionAuthorize("finance", "view")]
public sealed class SupplierApSubmissionsController(
    ISupplierApSubmissionService submissionService,
    ISupplierApSubmissionAttachmentService attachmentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(SupplierApSubmissionPageDto), 200)]
    public async Task<ActionResult<SupplierApSubmissionPageDto>> List(
        [FromQuery] string? status,
        [FromQuery] Guid? supplierId,
        [FromQuery] string? invoiceDateFrom,
        [FromQuery] string? invoiceDateTo,
        [FromQuery] string? submissionNumber,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseDate(invoiceDateFrom, out var from)
            || !TryParseDate(invoiceDateTo, out var to)
            || (from.HasValue && to.HasValue && from > to))
            return BadRequest(new { message = "發票日期範圍格式無效。" });
        return Ok(await submissionService.GetEmployeeSubmissionsAsync(
            status,
            supplierId,
            from,
            to,
            submissionNumber,
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            cancellationToken));
    }

    [HttpGet("{submissionId:guid}")]
    [ProducesResponseType(typeof(SupplierApSubmissionDetailDto), 200)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierApSubmissionDetailDto>> Get(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        var result = await submissionService.GetEmployeeSubmissionAsync(submissionId, cancellationToken);
        if (result is null)
            return NotFound();
        SetEntityTag(result.Version);
        return Ok(result);
    }

    [HttpGet("{submissionId:guid}/attachments")]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierApSubmissionAttachmentDto>), 200)]
    public async Task<ActionResult<IReadOnlyList<SupplierApSubmissionAttachmentDto>>> GetAttachments(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await attachmentService.GetEmployeeAttachmentsAsync(submissionId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{submissionId:guid}/attachments/{attachmentId:guid}/content")]
    public async Task<IActionResult> DownloadAttachment(
        Guid submissionId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var download = await attachmentService.GetEmployeeDownloadAsync(
            submissionId,
            attachmentId,
            cancellationToken);
        return download is null
            ? NotFound()
            : File(download.Content, download.Metadata.ContentType, download.Metadata.OriginalFileName);
    }

    [HttpPost("{submissionId:guid}/return")]
    [PermissionAuthorize("finance", "approve")]
    [ProducesResponseType(typeof(SupplierApSubmissionResultDto), 200)]
    public Task<ActionResult<SupplierApSubmissionResultDto>> Return(
        Guid submissionId,
        SupplierApSubmissionReviewRequestDto request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken) =>
        ReviewAsync(submissionId, "return", request, ifMatch, cancellationToken);

    [HttpPost("{submissionId:guid}/reject")]
    [PermissionAuthorize("finance", "approve")]
    [ProducesResponseType(typeof(SupplierApSubmissionResultDto), 200)]
    public Task<ActionResult<SupplierApSubmissionResultDto>> Reject(
        Guid submissionId,
        SupplierApSubmissionReviewRequestDto request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken) =>
        ReviewAsync(submissionId, "reject", request, ifMatch, cancellationToken);

    [HttpPost("{submissionId:guid}/approve")]
    [PermissionAuthorize("finance", "approve")]
    [ProducesResponseType(typeof(SupplierApSubmissionResultDto), 200)]
    public Task<ActionResult<SupplierApSubmissionResultDto>> Approve(
        Guid submissionId,
        SupplierApSubmissionReviewRequestDto request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken) =>
        ReviewAsync(submissionId, "approve", request, ifMatch, cancellationToken);

    private async Task<ActionResult<SupplierApSubmissionResultDto>> ReviewAsync(
        Guid submissionId,
        string action,
        SupplierApSubmissionReviewRequestDto request,
        string? ifMatch,
        CancellationToken cancellationToken)
    {
        if (!TryParseVersion(ifMatch, out var version))
            return BadRequest(new { message = "If-Match 必須包含有效的案件版本。" });
        var reviewerId = GetUserId();
        if (reviewerId == Guid.Empty)
            return Unauthorized();
        try
        {
            var result = await submissionService.ReviewAsync(
                submissionId,
                reviewerId,
                action,
                version,
                request.Reason ?? string.Empty,
                cancellationToken);
            SetEntityTag(result.Version);
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
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

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");
        return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
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
