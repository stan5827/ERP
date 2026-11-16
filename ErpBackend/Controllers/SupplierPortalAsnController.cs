using ErpBackend.DTOs;
using ErpBackend.Security.SupplierPortal;
using ErpBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

/// <summary>Exposes advance shipment notices within the authenticated supplier's scope.</summary>
[ApiController]
[Route("api/supplier-portal")]
[Authorize(Policy = SupplierPortalAuthentication.Policy)]
public sealed class SupplierPortalAsnController(
    SupplierPortalIdentityContext identityContext,
    ISupplierPortalAsnService asnService) : ControllerBase
{
    /// <summary>Lists the latest shipment notices belonging to the authenticated supplier.</summary>
    [HttpGet("asns")]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierPortalAsnSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupplierPortalAsnSummaryDto>>> GetAsns(
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();

        return Ok(await asnService.GetAsnsAsync(supplierUser.SupplierId, cancellationToken));
    }

    /// <summary>Returns a shipment notice only when it belongs to the authenticated supplier.</summary>
    [HttpGet("asns/{asnId:guid}")]
    [ProducesResponseType(typeof(SupplierPortalAsnDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierPortalAsnDetailDto>> GetAsn(
        Guid asnId,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();

        var asn = await asnService.GetAsnAsync(asnId, supplierUser.SupplierId, cancellationToken);
        return asn is null ? NotFound() : Ok(asn);
    }

    /// <summary>Returns shipment quantities available for the authenticated supplier's purchase order.</summary>
    [HttpGet("purchase-orders/{purchaseOrderId:guid}/asn-options")]
    [ProducesResponseType(typeof(SupplierPortalAsnOrderOptionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierPortalAsnOrderOptionsDto>> GetPurchaseOrderOptions(
        Guid purchaseOrderId,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();

        var options = await asnService.GetPurchaseOrderOptionsAsync(
            purchaseOrderId,
            supplierUser.SupplierId,
            cancellationToken);
        return options is null ? NotFound() : Ok(options);
    }

    /// <summary>Submits an advance shipment notice for the authenticated supplier's purchase order.</summary>
    [HttpPost("asns")]
    [ProducesResponseType(typeof(SupplierPortalAsnSubmittedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierPortalAsnSubmittedDto>> CreateAsn(
        CreateSupplierPortalAsnRequestDto request,
        CancellationToken cancellationToken)
    {
        if (identityContext.Current is not { } supplierUser)
            return Forbid();

        try
        {
            var result = await asnService.CreateAsnAsync(
                supplierUser.SupplierId,
                supplierUser.SupplierUserId,
                supplierUser.Role,
                request,
                cancellationToken);
            return CreatedAtAction(nameof(GetAsn), new { asnId = result.AsnId }, result);
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
