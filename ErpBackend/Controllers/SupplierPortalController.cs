using ErpBackend.DTOs;
using ErpBackend.Security.SupplierPortal;
using ErpBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

/// <summary>Exposes the authenticated supplier's portal identity.</summary>
[ApiController]
[Route("api/supplier-portal")]
[Authorize(Policy = SupplierPortalAuthentication.Policy)]
public sealed class SupplierPortalController(
    SupplierPortalIdentityContext identityContext,
    ISupplierPortalCollaborationService collaborationService) : ControllerBase
{
    /// <summary>Returns the supplier identity resolved from the validated identity-provider subject.</summary>
    [HttpGet("session")]
    [ProducesResponseType<SupplierPortalSessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<SupplierPortalSessionDto> GetSession()
    {
        var supplierUser = identityContext.Current;
        if (supplierUser is null)
            return Forbid();

        return Ok(new SupplierPortalSessionDto(
            supplierUser.SupplierUserId,
            supplierUser.Username,
            supplierUser.ContactName,
            supplierUser.Role,
            supplierUser.SupplierCode,
            supplierUser.SupplierName));
    }

    /// <summary>Lists RFQs explicitly assigned to the authenticated supplier.</summary>
    [HttpGet("rfqs")]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierPortalRfqSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupplierPortalRfqSummaryDto>>> GetRfqs(
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();

        return Ok(await collaborationService.GetRfqsAsync(
            supplierUser.SupplierId,
            cancellationToken));
    }

    /// <summary>Returns the assigned RFQ details and only this supplier's existing quote.</summary>
    [HttpGet("rfqs/{rfqId:guid}")]
    [ProducesResponseType(typeof(SupplierPortalRfqDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierPortalRfqDetailDto>> GetRfq(
        Guid rfqId,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();

        var rfq = await collaborationService.GetRfqAsync(
            rfqId,
            supplierUser.SupplierId,
            cancellationToken);
        return rfq is null ? NotFound() : Ok(rfq);
    }

    /// <summary>Submits the authenticated supplier's quote for assigned RFQ items.</summary>
    [HttpPost("rfqs/{rfqId:guid}/quotes")]
    [ProducesResponseType(typeof(SupplierPortalQuoteSubmittedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierPortalQuoteSubmittedDto>> SubmitQuote(
        Guid rfqId,
        SubmitSupplierRfqQuoteRequestDto request,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();

        try
        {
            return Ok(await collaborationService.SubmitQuoteAsync(
                rfqId,
                supplierUser.SupplierId,
                supplierUser.SupplierUserId,
                supplierUser.Role,
                request,
                cancellationToken));
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

    /// <summary>Lists issued purchase orders belonging only to the authenticated supplier.</summary>
    [HttpGet("purchase-orders")]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierPortalPurchaseOrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupplierPortalPurchaseOrderSummaryDto>>> GetPurchaseOrders(
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();

        return Ok(await collaborationService.GetPurchaseOrdersAsync(
            supplierUser.SupplierId,
            cancellationToken));
    }

    /// <summary>Returns a purchase order issued to the authenticated supplier and its response history.</summary>
    [HttpGet("purchase-orders/{purchaseOrderId:guid}")]
    [ProducesResponseType(typeof(SupplierPortalPurchaseOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierPortalPurchaseOrderDetailDto>> GetPurchaseOrder(
        Guid purchaseOrderId,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();

        var order = await collaborationService.GetPurchaseOrderAsync(
            purchaseOrderId,
            supplierUser.SupplierId,
            cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>Accepts, rejects, or requests line-level changes to an issued purchase order.</summary>
    [HttpPost("purchase-orders/{purchaseOrderId:guid}/responses")]
    [ProducesResponseType(typeof(SupplierPortalPurchaseOrderResponseSubmittedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierPortalPurchaseOrderResponseSubmittedDto>> SubmitPurchaseOrderResponse(
        Guid purchaseOrderId,
        SubmitSupplierPurchaseOrderResponseRequestDto request,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();

        try
        {
            var result = await collaborationService.SubmitPurchaseOrderResponseAsync(
                purchaseOrderId,
                supplierUser.SupplierId,
                supplierUser.SupplierUserId,
                supplierUser.Role,
                request,
                cancellationToken);
            return CreatedAtAction(
                nameof(GetPurchaseOrder),
                new { purchaseOrderId },
                result);
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
