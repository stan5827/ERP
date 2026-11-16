using ErpBackend.DTOs;
using ErpBackend.Infrastructure.CapaAttachments;
using ErpBackend.Security.SupplierPortal;
using ErpBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

/// <summary>Exposes supplier complaints and CAPA responses within the authenticated supplier's scope.</summary>
[ApiController]
[Route("api/supplier-portal/complaints")]
[Authorize(Policy = SupplierPortalAuthentication.Policy)]
public sealed class SupplierPortalCapaController(
    SupplierPortalIdentityContext identityContext,
    ISupplierPortalCapaService capaService,
    ICapaAttachmentService attachmentService) : ControllerBase
{
    /// <summary>Lists complaints issued to the authenticated supplier.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierPortalComplaintSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupplierPortalComplaintSummaryDto>>> GetComplaints(
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();

        return Ok(await capaService.GetComplaintsAsync(supplierUser.SupplierId, cancellationToken));
    }

    /// <summary>Returns a complaint and its CAPA response history only to its supplier.</summary>
    [HttpGet("{complaintId:guid}")]
    [ProducesResponseType(typeof(SupplierPortalComplaintDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierPortalComplaintDetailDto>> GetComplaint(
        Guid complaintId,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();

        var complaint = await capaService.GetComplaintAsync(
            complaintId,
            supplierUser.SupplierId,
            cancellationToken);
        return complaint is null ? NotFound() : Ok(complaint);
    }

    /// <summary>Saves a draft CAPA response without submitting it for internal review.</summary>
    [HttpPut("{complaintId:guid}/capa")]
    [ProducesResponseType(typeof(SupplierPortalCapaResponseSavedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<SupplierPortalCapaResponseSavedDto>> SaveDraft(
        Guid complaintId,
        SupplierPortalCapaResponseRequestDto request,
        CancellationToken cancellationToken) =>
        SaveResponseAsync(complaintId, request, submit: false, cancellationToken);

    /// <summary>Submits all eight CAPA disciplines for internal quality review.</summary>
    [HttpPost("{complaintId:guid}/capa/submit")]
    [ProducesResponseType(typeof(SupplierPortalCapaResponseSavedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<SupplierPortalCapaResponseSavedDto>> SubmitResponse(
        Guid complaintId,
        SupplierPortalCapaResponseRequestDto request,
        CancellationToken cancellationToken) =>
        SaveResponseAsync(complaintId, request, submit: true, cancellationToken);

    /// <summary>Uploads a private CAPA attachment and makes it available only after a clean scan.</summary>
    [HttpPost("{complaintId:guid}/capa/attachments")]
    [RequestSizeLimit(21 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 21 * 1024 * 1024)]
    [ProducesResponseType(typeof(CapaAttachmentUploadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CapaAttachmentUploadDto>> UploadAttachment(
        Guid complaintId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();
        if (file is null)
            return BadRequest(new { message = "請選擇附件檔案。" });

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await attachmentService.UploadSupplierAttachmentAsync(
                complaintId,
                supplierUser.SupplierId,
                supplierUser.SupplierUserId,
                supplierUser.Role,
                file.FileName,
                file.Length,
                stream,
                cancellationToken);
            return result.ScanStatus == "clean"
                ? StatusCode(StatusCodes.Status201Created, result)
                : UnprocessableEntity(new
                {
                    attachment = result,
                    message = "附件未通過惡意程式掃描，請移除後重新上傳。"
                });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
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

    /// <summary>Lists attachments belonging only to the authenticated supplier's complaint.</summary>
    [HttpGet("{complaintId:guid}/capa/attachments")]
    [ProducesResponseType(typeof(IReadOnlyList<CapaAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CapaAttachmentDto>>> GetAttachments(
        Guid complaintId,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();
        try
        {
            return Ok(await attachmentService.GetSupplierAttachmentsAsync(
                complaintId,
                supplierUser.SupplierId,
                cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Downloads a clean attachment only after checking supplier ownership.</summary>
    [HttpGet("{complaintId:guid}/capa/attachments/{attachmentId:guid}/content")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAttachment(
        Guid complaintId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();
        var download = await attachmentService.GetSupplierDownloadAsync(
            complaintId,
            attachmentId,
            supplierUser.SupplierId,
            cancellationToken);
        if (download is null)
            return NotFound();
        return File(download.Content, download.Metadata.ContentType, download.Metadata.OriginalFileName);
    }

    /// <summary>Revokes an attachment that has not been attached to a submitted response.</summary>
    [HttpDelete("{complaintId:guid}/capa/attachments/{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RevokeAttachment(
        Guid complaintId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();
        try
        {
            await attachmentService.RevokeSupplierAttachmentAsync(
                complaintId,
                attachmentId,
                supplierUser.SupplierId,
                supplierUser.Role,
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
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private async Task<ActionResult<SupplierPortalCapaResponseSavedDto>> SaveResponseAsync(
        Guid complaintId,
        SupplierPortalCapaResponseRequestDto request,
        bool submit,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();

        try
        {
            var result = submit
                ? await capaService.SubmitResponseAsync(
                    complaintId,
                    supplierUser.SupplierId,
                    supplierUser.SupplierUserId,
                    supplierUser.Role,
                    request,
                    cancellationToken)
                : await capaService.SaveDraftAsync(
                    complaintId,
                    supplierUser.SupplierId,
                    supplierUser.SupplierUserId,
                    supplierUser.Role,
                    request,
                    cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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
}
