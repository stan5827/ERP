using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Procurement;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class SuppliersController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    private readonly ISupplierRatingService _ratingService;

    public SuppliersController(ErpGeneralDbContext db, ISupplierRatingService ratingService)
    {
        _db = db;
        _ratingService = ratingService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SupplierDto>>>> GetSuppliers([FromQuery] string? searchTerm)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var query = _db.Suppliers.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(s => s.SupplierName.Contains(searchTerm) || s.SupplierCode.Contains(searchTerm));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.SupplierCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
        var dtos = items.Select(s => new SupplierDto
        {
            Id = s.Id,
            SupplierCode = s.SupplierCode,
            SupplierName = s.SupplierName,
            TaxId = s.TaxId,
            ContactPerson = s.ContactPerson,
            Phone = s.Phone,
            Email = s.Email,
            Status = "ACTIVE",
            Rating = s.Rating,
            Currency = s.Currency ?? "TWD",
            CreatedAt = s.CreatedAt
        }).ToList();

        return Ok(dtos, $"第 {page} 頁，共 {totalCount} 筆", totalCount);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> GetSupplier(Guid id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        if (s == null) return NotFound(ApiResponse.Fail("供應商不存在"));

        return Ok(new SupplierDto
        {
            Id = s.Id,
            SupplierCode = s.SupplierCode,
            SupplierName = s.SupplierName,
            TaxId = s.TaxId,
            ContactPerson = s.ContactPerson,
            Phone = s.Phone,
            Email = s.Email,
            Status = s.Status,
            Rating = s.Rating,
            Currency = s.Currency,
            CreatedAt = s.CreatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> CreateSupplier(CreateSupplierRequest request)
    {
        if (await _db.Suppliers.AnyAsync(s => s.SupplierCode == request.SupplierCode))
            return BadRequest(ApiResponse.Fail("供應商編號已存在"));

        var supplier = new Supplier
        {
            SupplierCode = request.SupplierCode,
            SupplierName = request.SupplierName,
            TaxId = request.TaxId,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            InvoiceAddress = request.InvoiceAddress,
            DeliveryAddress = request.DeliveryAddress,
            PaymentTerms = request.PaymentTerms,
            Currency = request.Currency ?? "TWD",
            Status = "ACTIVE"
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        return Ok(new SupplierDto
        {
            Id = supplier.Id,
            SupplierCode = supplier.SupplierCode,
            SupplierName = supplier.SupplierName,
            TaxId = supplier.TaxId,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Status = supplier.Status,
            Currency = supplier.Currency,
            CreatedAt = supplier.CreatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> UpdateSupplier(Guid id, CreateSupplierRequest request)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound(ApiResponse.Fail("供應商不存在"));

        supplier.SupplierName = request.SupplierName;
        supplier.TaxId = request.TaxId;
        supplier.ContactPerson = request.ContactPerson;
        supplier.Phone = request.Phone;
        supplier.Email = request.Email;
        supplier.Address = request.Address;
        supplier.InvoiceAddress = request.InvoiceAddress;
        supplier.DeliveryAddress = request.DeliveryAddress;
        supplier.PaymentTerms = request.PaymentTerms;
        supplier.Currency = request.Currency ?? "TWD";
        supplier.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok("更新成功");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteSupplier(Guid id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound(ApiResponse.Fail("供應商不存在"));

        _db.Suppliers.Remove(supplier);
        await _db.SaveChangesAsync();
        return Ok("刪除成功");
    }

    // ========== Portal User Endpoints ==========

    [HttpGet("{supplierId}/portal-users")]
    public async Task<ActionResult<ApiResponse<List<SupplierPortalUserDto>>>> GetPortalUsers(Guid supplierId)
    {
        var users = await _db.Set<ErpBackend.Models.Portal.SupplierUser>()
            .Where(u => u.SupplierId == supplierId)
            .OrderBy(u => u.Username)
            .Select(u => new SupplierPortalUserDto
            {
                Id = u.Id,
                Username = u.Username,
                ContactName = u.ContactName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("{supplierId}/portal-users")]
    public async Task<ActionResult<ApiResponse>> CreatePortalUser(Guid supplierId, CreateSupplierPortalUserRequest request)
    {
        var supplier = await _db.Suppliers.FindAsync(supplierId);
        if (supplier == null) return NotFound(ApiResponse.Fail("供應商不存在"));

        if (await _db.Set<ErpBackend.Models.Portal.SupplierUser>().AnyAsync(u => u.Username == request.Username))
            return BadRequest(ApiResponse.Fail("帳號已存在"));

        var user = new ErpBackend.Models.Portal.SupplierUser
        {
            SupplierId = supplierId,
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            ContactName = request.ContactName,
            Email = request.Email,
            Role = request.Role,
            IsActive = true
        };

        _db.Add(user);
        await _db.SaveChangesAsync();

        return Ok("建立成功");
    }

    [HttpPut("portal-users/{userId}")]
    public async Task<ActionResult<ApiResponse>> UpdatePortalUser(Guid userId, CreateSupplierPortalUserRequest request)
    {
        var user = await _db.Set<ErpBackend.Models.Portal.SupplierUser>().FindAsync(userId);
        if (user == null) return NotFound(ApiResponse.Fail("帳號不存在"));

        user.ContactName = request.ContactName;
        user.Email = request.Email;
        user.Role = request.Role;
        
        if (!string.IsNullOrEmpty(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await _db.SaveChangesAsync();
        return Ok("更新成功");
    }

    [HttpDelete("portal-users/{userId}")]
    public async Task<ActionResult<ApiResponse>> DeletePortalUser(Guid userId)
    {
        var user = await _db.Set<ErpBackend.Models.Portal.SupplierUser>().FindAsync(userId);
        if (user == null) return NotFound(ApiResponse.Fail("帳號不存在"));

        _db.Remove(user);
        await _db.SaveChangesAsync();
        return Ok("刪除成功");
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
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound(ApiResponse.Fail("供應商不存在"));

        if (supplier.ApprovalStatus == "APPROVED")
            return BadRequest(ApiResponse.Fail("供應商已通過審批"));

        supplier.ApprovalStatus = "UNDER_REVIEW";
        await _db.SaveChangesAsync();
        return Ok("已送出審批");
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult<ApiResponse>> ApproveSupplier(Guid id, [FromBody] SupplierApprovalRequest req)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound(ApiResponse.Fail("供應商不存在"));

        if (supplier.ApprovalStatus == "APPROVED")
            return BadRequest(ApiResponse.Fail("供應商已通過審批"));

        supplier.ApprovalStatus = "APPROVED";
        supplier.Status = "ACTIVE";
        supplier.ApprovedBy = req.EmployeeId;
        supplier.ApprovedAt = DateTime.UtcNow;
        supplier.ApprovalNotes = req.Comments;

        await _db.SaveChangesAsync();
        return Ok("供應商已核准");
    }

    [HttpPost("{id}/reject")]
    public async Task<ActionResult<ApiResponse>> RejectSupplier(Guid id, [FromBody] SupplierApprovalRequest req)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound(ApiResponse.Fail("供應商不存在"));

        supplier.ApprovalStatus = "REJECTED";
        supplier.ApprovalNotes = req.Comments;

        await _db.SaveChangesAsync();
        return Ok("供應商已駁回");
    }

    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult<ApiResponse>> DeactivateSupplier(Guid id, [FromBody] SupplierApprovalRequest req)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound(ApiResponse.Fail("供應商不存在"));

        supplier.Status = "INACTIVE";
        supplier.ApprovalNotes = req.Comments;

        await _db.SaveChangesAsync();
        return Ok("供應商已停用");
    }
}

