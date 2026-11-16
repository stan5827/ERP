using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Filters;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/boms")]
public class BomsController : BaseController
{
    private readonly IBomService _bomService;

    public BomsController(IBomService bomService) => _bomService = bomService;

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BomDto>>>> GetBoms([FromQuery] Guid? productSkuId)
        => Ok(await _bomService.GetBomsAsync(productSkuId));

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BomDto>>> GetBom(Guid id)
    {
        var dto = await _bomService.GetBomAsync(id);
        return dto == null ? NotFound(ApiHelper.FailOf<BomDto>("找不到物料清單 (BOM)")) : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BomDto>>> CreateBom([FromBody] CreateBomRequest request)
    {
        var (dto, status) = await _bomService.CreateBomAsync(request);
        if (status == "invalid") return BadRequest(ApiHelper.FailOf<BomDto>("無效的 BOM 資料"));
        if (status == "duplicate") return BadRequest(ApiHelper.FailOf<BomDto>($"BOM 代碼 {request.BomCode} 已存在"));
        return Ok(dto!);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BomDto>>> UpdateBom(Guid id, [FromBody] UpdateBomRequest request)
    {
        var (dto, status) = await _bomService.UpdateBomAsync(id, request);
        if (status == "not_found") return NotFound(ApiHelper.FailOf<BomDto>("找不到 BOM"));
        if (status == "invalid") return BadRequest(ApiHelper.FailOf<BomDto>("無效的 BOM 資料"));
        return Ok(dto!);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteBom(Guid id)
    {
        var (ok, status) = await _bomService.DeleteBomAsync(id);
        if (status == "not_found") return NotFound(ApiHelper.Fail("找不到 BOM"));
        return ok ? Ok("BOM 已刪除") : BadRequest(ApiHelper.Fail("無法刪除 BOM"));
    }

    [Authorize]
    [HttpPatch("{id:guid}/activate")]
    public async Task<ActionResult<ApiResponse>> ActivateBom(Guid id)
    {
        var (ok, status) = await _bomService.SetBomStatusAsync(id, "active");
        if (status == "not_found") return NotFound(ApiHelper.Fail("找不到 BOM"));
        return ok ? Ok("BOM 已啟用") : BadRequest(ApiHelper.Fail("無法啟用 BOM"));
    }

    [Authorize]
    [HttpPatch("{id:guid}/deprecate")]
    public async Task<ActionResult<ApiResponse>> DeprecateBom(Guid id)
    {
        var (ok, status) = await _bomService.SetBomStatusAsync(id, "deprecated");
        if (status == "not_found") return NotFound(ApiHelper.Fail("找不到 BOM"));
        return ok ? Ok("BOM 已停用") : BadRequest(ApiHelper.Fail("無法停用 BOM"));
    }

    [Authorize]
    [HttpGet("{id:guid}/cost-rollup")]
    public async Task<ActionResult<ApiResponse<BomCostRollupDto>>> CostRollup(Guid id)
    {
        var dto = await _bomService.GetCostRollupAsync(id);
        return dto == null ? NotFound(ApiHelper.FailOf<BomCostRollupDto>("找不到 BOM")) : Ok(dto);
    }

    /// <summary>Recalculates and approves the BOM rollup as the product SKU's standard cost.</summary>
    [PermissionAuthorize("engineering", "approve")]
    [HttpPost("{id:guid}/cost-rollup/approve")]
    [ProducesResponseType(typeof(ApiResponse<BomStandardCostRollupApprovalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<BomStandardCostRollupApprovalDto>>> ApproveCostRollup(
        Guid id,
        [FromBody] ApplyBomStandardCostRollupRequestDto request,
        CancellationToken cancellationToken)
    {
        var (dto, status) = await _bomService.ApplyStandardCostRollupAsync(
            id,
            request.ExpectedTotalCost,
            GetUserId(),
            cancellationToken);

        return status switch
        {
            "not_found" => NotFound(ApiHelper.FailOf<BomStandardCostRollupApprovalDto>("找不到 BOM 或產品 SKU")),
            "inactive_bom" => Conflict(ApiHelper.FailOf<BomStandardCostRollupApprovalDto>("BOM 必須啟用且在有效期間內，才能核准標準成本")),
            "obsolete_bom" => Conflict(ApiHelper.FailOf<BomStandardCostRollupApprovalDto>("此 BOM 已非最新有效版本，請改用最新版本核准")),
            "invalid_bom" => BadRequest(ApiHelper.FailOf<BomStandardCostRollupApprovalDto>("BOM 必須至少包含一筆有效元件")),
            "stale_cost" => Conflict(ApiHelper.FailOf<BomStandardCostRollupApprovalDto>("BOM 成本已變更，請重新試算並確認後再核准")),
            _ => Ok(dto!)
        };
    }

    [Authorize]
    [HttpGet("{productSkuId:guid}/tree")]
    public async Task<ActionResult<ApiResponse<BomTreeNodeDto>>> GetBomTree(Guid productSkuId)
    {
        var dto = await _bomService.GetBomTreeAsync(productSkuId);
        return dto == null ? NotFound(ApiHelper.FailOf<BomTreeNodeDto>("找不到物料清單 (BOM) 數量清單")) : Ok(dto);
    }

    [Authorize]
    [HttpGet("latest/{productSkuId:guid}")]
    public async Task<ActionResult<ApiResponse<BomDto>>> GetLatestRevision(Guid productSkuId)
    {
        var dto = await _bomService.GetLatestActiveRevisionAsync(productSkuId);
        return dto == null ? NotFound(ApiHelper.FailOf<BomDto>("找不到物料清單 (BOM)")) : Ok(dto);
    }
}
