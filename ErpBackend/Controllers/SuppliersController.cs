using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Filters;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class SuppliersController : BaseController
{
    private readonly ISupplierService _supplierService;
    private readonly ISupplierRatingService _ratingService;
    private readonly ISupplierPortalInvitationService _portalInvitationService;

    public SuppliersController(
        ISupplierService supplierService,
        ISupplierRatingService ratingService,
        ISupplierPortalInvitationService portalInvitationService)
    {
        _supplierService = supplierService;
        _ratingService = ratingService;
        _portalInvitationService = portalInvitationService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SupplierDto>>>> GetSuppliers([FromQuery] string? searchTerm)
    {
        var page = GetPage();
        var pageSize = GetPageSize();
        var (items, totalCount) = await _supplierService.GetSuppliersAsync(searchTerm, page, pageSize);
        return Ok(items, $"第 {page} 頁，共 {totalCount} 筆", totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> GetSupplier(Guid id)
    {
        var dto = await _supplierService.GetSupplierAsync(id);
        if (dto == null) return NotFound(ApiResponse.Fail("供應商不存在"));
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> CreateSupplier(CreateSupplierRequest request)
    {
        try
        {
            var dto = await _supplierService.CreateSupplierAsync(request);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> UpdateSupplier(Guid id, CreateSupplierRequest request)
    {
        try
        {
            await _supplierService.UpdateSupplierAsync(id, request);
            return Ok("更新成功");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteSupplier(Guid id)
    {
        try
        {
            await _supplierService.DeleteSupplierAsync(id);
            return Ok("刪除成功");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
    }

    // ========== Portal User Endpoints ==========

    [HttpGet("{supplierId}/portal-users")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SupplierPortalUserStatusResponseDto>>>> GetPortalUsers(
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        var users = await _portalInvitationService.GetUsersAsync(supplierId, cancellationToken);
        return Ok(users);
    }

    [HttpPost("{supplierId}/portal-users")]
    [PermissionAuthorize("suppliers", "edit")]
    public ActionResult<ApiResponse> CreatePortalUser(Guid supplierId)
    {
        return StatusCode(
            StatusCodes.Status410Gone,
            ApiResponse.Fail("本地密碼帳號管理已停用，請改用 Keycloak 邀請流程。"));
    }

    /// <summary>Creates a Keycloak invitation for an approved supplier contact.</summary>
    [HttpPost("{supplierId}/portal-users/invitations")]
    [PermissionAuthorize("suppliers", "edit")]
    [ProducesResponseType(typeof(ApiResponse<SupplierPortalInvitationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<SupplierPortalInvitationResponseDto>>> InvitePortalUser(
        Guid supplierId,
        CreateSupplierPortalInvitationRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _portalInvitationService.InviteAsync(
                supplierId,
                request,
                GetUserId(),
                cancellationToken);
            return Ok(result, "Keycloak 邀請已寄出。");
        }
        catch (ArgumentException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (KeycloakSupplierIdentityException ex) when (ex.ErrorCode == "ExistingIdentityNotLinked")
        {
            return Conflict(ApiResponse.Fail(
                "此 Email 已存在於 WIP Keycloak，但未綁定這筆 ERP 邀請。基於帳號安全，系統不會自動連結既有帳號。請改用未註冊的 Email，或先確認並處理 Keycloak 中的既有測試帳號後，再重試此邀請。"));
        }
        catch (KeycloakSupplierIdentityException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse.Fail("Keycloak 邀請服務暫時無法完成，請稍後重試。"));
        }
    }

    /// <summary>Retries Keycloak provisioning after a failed invitation attempt.</summary>
    [HttpPost("portal-users/{userId}/retry")]
    [PermissionAuthorize("suppliers", "edit")]
    public async Task<ActionResult<ApiResponse<SupplierPortalInvitationResponseDto>>> RetryPortalInvitation(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _portalInvitationService.RetryAsync(userId, GetUserId(), cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (KeycloakSupplierIdentityException ex) when (ex.ErrorCode == "ExistingIdentityNotLinked")
        {
            return Conflict(ApiResponse.Fail(
                "此 Email 已存在於 WIP Keycloak，但未綁定這筆 ERP 邀請。基於帳號安全，系統不會自動連結既有帳號。請改用未註冊的 Email，或先確認並處理 Keycloak 中的既有測試帳號後，再重試此邀請。"));
        }
        catch (KeycloakSupplierIdentityException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse.Fail("Keycloak 邀請服務暫時無法完成，請稍後重試。"));
        }
    }

    /// <summary>Resends an outstanding Keycloak invitation.</summary>
    [HttpPost("portal-users/{userId}/resend-invitation")]
    [PermissionAuthorize("suppliers", "edit")]
    public async Task<ActionResult<ApiResponse<SupplierPortalInvitationResponseDto>>> ResendPortalInvitation(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _portalInvitationService.ResendAsync(userId, GetUserId(), cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (KeycloakSupplierIdentityException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse.Fail("Keycloak 邀請服務暫時無法完成，請稍後重試。"));
        }
    }

    /// <summary>Revokes a supplier portal invitation or account.</summary>
    [HttpDelete("portal-users/{userId}")]
    [PermissionAuthorize("suppliers", "edit")]
    public async Task<ActionResult<ApiResponse>> CancelPortalInvitation(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _portalInvitationService.CancelAsync(userId, GetUserId(), cancellationToken);
            return Ok("供應商入口帳號已停用。");
        }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (KeycloakSupplierIdentityException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse.Fail("ERP 已停用帳號，但 Keycloak 撤銷尚待重試。"));
        }
    }

    [HttpPut("portal-users/{userId}")]
    [PermissionAuthorize("suppliers", "edit")]
    public ActionResult<ApiResponse> UpdatePortalUser(Guid userId)
    {
        return StatusCode(
            StatusCodes.Status410Gone,
            ApiResponse.Fail("本地密碼帳號管理已停用，請使用 Keycloak 邀請與撤銷流程。"));
    }

    [HttpGet("{id}/rating")]
    public async Task<ActionResult<ApiResponse<SupplierRatingDto>>> GetSupplierRating(Guid id, [FromQuery] int months = 12)
    {
        try
        {
            var result = await _ratingService.CalculateRatingAsync(id, months);
            return Ok(new SupplierRatingDto
            {
                SupplierId = result.SupplierId,
                SupplierCode = result.SupplierCode,
                SupplierName = result.SupplierName,
                QualityScore = result.QualityScore,
                DeliveryScore = result.DeliveryScore,
                CostScore = result.CostScore,
                ServiceScore = result.ServiceScore,
                TotalScore = result.TotalScore,
                Grade = result.Grade,
                CalculatedAt = result.CalculationDate,
                PeriodMonths = result.PeriodMonths
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/rating/calculate")]
    public async Task<ActionResult<ApiResponse>> CalculateSupplierRating(Guid id)
    {
        try
        {
            await _ratingService.SaveRatingAsync(id);
            return Ok("評等計算完成");
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpGet("rating/all")]
    public async Task<ActionResult<ApiResponse<List<SupplierRatingDto>>>> GetAllRatings([FromQuery] int months = 12)
    {
        try
        {
            var results = await _ratingService.CalculateAllRatingsAsync(months);
            var dtos = results.Select(r => new SupplierRatingDto
            {
                SupplierId = r.SupplierId,
                SupplierCode = r.SupplierCode,
                SupplierName = r.SupplierName,
                QualityScore = r.QualityScore,
                DeliveryScore = r.DeliveryScore,
                CostScore = r.CostScore,
                ServiceScore = r.ServiceScore,
                TotalScore = r.TotalScore,
                Grade = r.Grade,
                CalculatedAt = r.CalculationDate,
                PeriodMonths = r.PeriodMonths
            }).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("rating/batch-calculate")]
    public async Task<ActionResult<ApiResponse>> BatchCalculateRatings([FromQuery] int months = 12)
    {
        try
        {
            await _ratingService.RunBatchCalculationAsync(months);
            return Ok("批次評等計算完成");
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/submit-for-approval")]
    public async Task<ActionResult<ApiResponse>> SubmitSupplierForApproval(Guid id)
    {
        try
        {
            await _supplierService.SubmitForApprovalAsync(id);
            return Ok("已送出審批");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/approve")]
    [PermissionAuthorize("suppliers", "approve")]
    public async Task<ActionResult<ApiResponse>> ApproveSupplier(Guid id, [FromBody] SupplierApprovalRequest req)
    {
        try
        {
            req.EmployeeId = GetEmployeeId();
            await _supplierService.ApproveSupplierAsync(id, req);
            return Ok("供應商已核准");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/reject")]
    public async Task<ActionResult<ApiResponse>> RejectSupplier(Guid id, [FromBody] SupplierApprovalRequest req)
    {
        try
        {
            await _supplierService.RejectSupplierAsync(id, req);
            return Ok("供應商已駁回");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult<ApiResponse>> DeactivateSupplier(Guid id, [FromBody] SupplierApprovalRequest req)
    {
        try
        {
            await _supplierService.DeactivateSupplierAsync(id, req);
            return Ok("供應商已停用");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
    }
}
