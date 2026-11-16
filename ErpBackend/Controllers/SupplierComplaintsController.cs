using ErpBackend.DTOs;
using ErpBackend.Filters;
using ErpBackend.Infrastructure.CapaAttachments;
using ErpBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

[ApiController]
[Authorize]
[Route("api/erp/supplier-complaints")]
[PermissionAuthorize("quality", "view")]
public sealed class SupplierComplaintsController(
    ISupplierCapaReviewService reviewService,
    ICapaAttachmentService attachmentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierPortalComplaintSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupplierPortalComplaintSummaryDto>>> GetComplaints(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await reviewService.GetComplaintsAsync(
            status,
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            cancellationToken));
    }

    [HttpGet("{complaintId:guid}")]
    [ProducesResponseType(typeof(SupplierPortalComplaintDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierPortalComplaintDetailDto>> GetComplaint(
        Guid complaintId,
        CancellationToken cancellationToken)
    {
        var complaint = await reviewService.GetComplaintAsync(complaintId, cancellationToken);
        return complaint is null ? NotFound() : Ok(complaint);
    }

    [HttpGet("{complaintId:guid}/capa/reviews")]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierCapaReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SupplierCapaReviewDto>>> GetReviews(
        Guid complaintId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await reviewService.GetReviewsAsync(complaintId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{complaintId:guid}/capa/reviews/start")]
    [PermissionAuthorize("quality", "approve")]
    [ProducesResponseType(typeof(SupplierCapaReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierCapaReviewDto>> StartReview(
        Guid complaintId,
        CancellationToken cancellationToken)
    {
        var reviewerId = GetUserId();
        if (reviewerId == Guid.Empty)
            return Unauthorized();
        try
        {
            var result = await reviewService.StartReviewAsync(complaintId, reviewerId, cancellationToken);
            return CreatedAtAction(nameof(GetReviews), new { complaintId }, result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{complaintId:guid}/capa/reviews")]
    [PermissionAuthorize("quality", "approve")]
    [ProducesResponseType(typeof(SupplierCapaReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierCapaReviewDto>> Review(
        Guid complaintId,
        SupplierCapaReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        var reviewerId = GetUserId();
        if (reviewerId == Guid.Empty)
            return Unauthorized();
        try
        {
            var result = await reviewService.ReviewAsync(
                complaintId,
                reviewerId,
                request,
                cancellationToken);
            return CreatedAtAction(nameof(GetReviews), new { complaintId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{complaintId:guid}/capa/close")]
    [PermissionAuthorize("quality", "approve")]
    [ProducesResponseType(typeof(SupplierCapaReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierCapaReviewDto>> Close(
        Guid complaintId,
        SupplierCapaCloseRequestDto request,
        CancellationToken cancellationToken)
    {
        var reviewerId = GetUserId();
        if (reviewerId == Guid.Empty)
            return Unauthorized();
        try
        {
            var result = await reviewService.CloseAsync(
                complaintId,
                reviewerId,
                request,
                cancellationToken);
            return CreatedAtAction(nameof(GetReviews), new { complaintId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("{complaintId:guid}/capa/attachments")]
    [ProducesResponseType(typeof(IReadOnlyList<CapaAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CapaAttachmentDto>>> GetAttachments(
        Guid complaintId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await attachmentService.GetEmployeeAttachmentsAsync(complaintId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{complaintId:guid}/capa/attachments/{attachmentId:guid}/content")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAttachment(
        Guid complaintId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var download = await attachmentService.GetEmployeeDownloadAsync(
            complaintId,
            attachmentId,
            cancellationToken);
        if (download is null)
            return NotFound();
        return File(download.Content, download.Metadata.ContentType, download.Metadata.OriginalFileName);
    }

    [HttpPost("{complaintId:guid}/capa/attachments/{attachmentId:guid}/rescan")]
    [PermissionAuthorize("quality", "approve")]
    [ProducesResponseType(typeof(CapaAttachmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CapaAttachmentDto>> RescanAttachment(
        Guid complaintId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var reviewerId = GetUserId();
        if (reviewerId == Guid.Empty)
            return Unauthorized();
        try
        {
            return Ok(await attachmentService.RescanAsync(
                complaintId,
                attachmentId,
                reviewerId,
                cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (CapaAttachmentScannerUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (CapaAttachmentConfigurationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
