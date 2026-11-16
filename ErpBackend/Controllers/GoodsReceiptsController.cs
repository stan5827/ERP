using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Filters;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class GoodsReceiptsController : BaseController
{
    private readonly IGoodsReceiptService _goodsReceiptService;
    private readonly IGoodsReceiptReversalService _goodsReceiptReversalService;

    public GoodsReceiptsController(
        IGoodsReceiptService goodsReceiptService,
        IGoodsReceiptReversalService goodsReceiptReversalService)
    {
        _goodsReceiptService = goodsReceiptService;
        _goodsReceiptReversalService = goodsReceiptReversalService;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<GoodsReceiptDto>>>> GetGoodsReceiptsAsync()
    {
        var result = await _goodsReceiptService.GetGoodsReceiptsAsync(GetPage(), GetPageSize());
        return Ok(result.Items, totalCount: result.TotalCount);
    }

    /// <summary>Reverse the inventory effect of a pending goods receipt without deleting its source records.</summary>
    [Authorize]
    [ServiceFilter(typeof(PermissionAuthorizeFilter))]
    [PermissionAuthorize("procurement", "reverse")]
    [HttpPost("{id:guid}/reverse")]
    [ProducesResponseType(typeof(ApiResponse<GoodsReceiptReversalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<GoodsReceiptReversalDto>>> ReverseGoodsReceiptAsync(
        Guid id,
        [FromBody] ReverseGoodsReceiptDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiHelper.Fail("無法識別操作人員"));

        try
        {
            var result = await _goodsReceiptReversalService.ReverseAsync(
                id, request.Reason, userId, cancellationToken);
            return Ok(result, "收貨單已沖銷");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiHelper.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiHelper.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiHelper.Fail(ex.Message));
        }
    }
}
