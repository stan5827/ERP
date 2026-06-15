using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Procurement;
using ErpBackend.Helpers;
using ErpBackend.Models.HR;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class PurchaseRequisitionsController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    private readonly ApprovalWorkflowService _approvalService;
    private readonly IServiceProvider _serviceProvider;

    public PurchaseRequisitionsController(ErpGeneralDbContext db, ApprovalWorkflowService approvalService, IServiceProvider serviceProvider)
    {
        _db = db;
        _approvalService = approvalService;
        _serviceProvider = serviceProvider;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PurchaseRequisitionDto>>>> GetPurchaseRequisitions([FromQuery] string? status, [FromQuery] string? searchTerm)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var query = _db.PurchaseRequisitions
            .Include(r => r.Department)
            .Include(r => r.Requester)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(r => r.PrNumber.Contains(searchTerm) || (r.Purpose != null && r.Purpose.Contains(searchTerm)));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.PrDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(r => new PurchaseRequisitionDto
        {
            Id = r.Id,
            PrNumber = r.PrNumber,
            RequestDate = r.PrDate,
            DepartmentId = r.DepartmentId,
            DepartmentName = r.Department?.DeptName,
            RequesterId = r.RequesterId,
            RequesterName = r.Requester?.ChineseName,
            ProjectCode = r.ProjectCode,
            Purpose = r.Purpose,
            Priority = r.Priority,
            BudgetAccount = r.BudgetAccount,
            Status = r.Status,
            IsLocked = r.IsLocked,
            TotalEstimatedAmount = r.TotalEstimatedAmount,
            CreatedAt = r.CreatedAt,
            ItemCount = r.Items?.Count ?? 0
        }).ToList();

        return Ok(dtos, totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PurchaseRequisitionDto>>> GetPurchaseRequisition(Guid id)
    {
        var r = await _db.PurchaseRequisitions
            .Include(r => r.Department)
            .Include(r => r.Requester)
            .Include(r => r.Items)
                .ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (r == null) return NotFound(ApiHelper.Fail("PR not found"));

        var dto = new PurchaseRequisitionDto
        {
            Id = r.Id,
            PrNumber = r.PrNumber,
            RequestDate = r.PrDate,
            DepartmentId = r.DepartmentId,
            DepartmentName = r.Department?.DeptName,
            RequesterId = r.RequesterId,
            RequesterName = r.Requester?.ChineseName,
            ProjectCode = r.ProjectCode,
            Purpose = r.Purpose,
            Priority = r.Priority,
            BudgetAccount = r.BudgetAccount,
            Status = r.Status,
            IsLocked = r.IsLocked,
            TotalEstimatedAmount = r.TotalEstimatedAmount,
            CreatedAt = r.CreatedAt,
            Items = r.Items.Select(i => new PurchaseRequisitionItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                SkuNumber = i.Sku?.SkuNumber,
                SkuName = i.Sku?.SkuName,
                Quantity = i.Quantity,
                Unit = i.Unit ?? "PCS",
                EstimatedUnitPrice = i.EstimatedUnitPrice,
                Currency = i.Currency,
                RequiredDate = i.RequiredDate,
                Notes = i.Notes
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseRequisitionDto>>> CreatePurchaseRequisition([FromBody] CreatePurchaseRequisitionRequest req)
    {
        var pr = new PurchaseRequisition
        {
            Id = Guid.NewGuid(),
            PrNumber = DocumentNumberGenerator.GeneratePurchaseRequisition(),
            PrDate = req.PrDate ?? DateTime.UtcNow,
            DepartmentId = req.DepartmentId,
            RequesterId = req.RequesterId,
            ProjectCode = req.ProjectCode,
            Purpose = req.Purpose,
            Priority = req.Priority,
            BudgetAccount = req.BudgetAccount,
            Status = "DRAFT"
        };

        decimal total = 0;
        if (req.Items != null)
        {
            foreach (var item in req.Items)
            {
                pr.Items.Add(new PurchaseRequisitionItem
                {
                    Id = Guid.NewGuid(),
                    PurchaseRequisitionId = pr.Id,
                    SkuId = item.SkuId,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    EstimatedUnitPrice = item.EstimatedUnitPrice,
                    Currency = item.Currency,
                    RequiredDate = item.RequiredDate,
                    Notes = item.Notes
                });
                total += item.Quantity * item.EstimatedUnitPrice;
            }
        }
        pr.TotalEstimatedAmount = total;

        _db.PurchaseRequisitions.Add(pr);
        await _db.SaveChangesAsync();

        var dto = new PurchaseRequisitionDto
        {
            Id = pr.Id,
            PrNumber = pr.PrNumber,
            RequestDate = pr.PrDate,
            Status = pr.Status,
            TotalEstimatedAmount = pr.TotalEstimatedAmount,
            CreatedAt = pr.CreatedAt
        };

        return Ok(dto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> UpdatePurchaseRequisition(Guid id, [FromBody] UpdatePurchaseRequisitionRequest req)
    {
        var pr = await _db.PurchaseRequisitions.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);
        if (pr == null) return NotFound(ApiHelper.Fail("PR not found"));
        if (pr.IsLocked || (pr.Status != "pending" && pr.Status != "draft" && pr.Status != "RETURNED")) 
            return BadRequest(ApiHelper.Fail("Cannot edit PR in its current state"));

        if (req.PrDate.HasValue) pr.PrDate = req.PrDate.Value;
        if (req.DepartmentId != Guid.Empty && req.DepartmentId != null) pr.DepartmentId = req.DepartmentId;
        if (req.RequesterId != Guid.Empty && req.RequesterId != null) pr.RequesterId = req.RequesterId;
        if (req.ProjectCode != null) pr.ProjectCode = req.ProjectCode;
        if (req.Purpose != null) pr.Purpose = req.Purpose;
        if (req.Priority != null) pr.Priority = req.Priority;
        if (req.BudgetAccount != null) pr.BudgetAccount = req.BudgetAccount;

        if (req.Items != null)
        {
            _db.PurchaseRequisitionItems.RemoveRange(pr.Items);
            pr.Items.Clear();
            decimal total = 0;
            foreach (var item in req.Items)
            {
                pr.Items.Add(new PurchaseRequisitionItem
                {
                    Id = Guid.NewGuid(),
                    PurchaseRequisitionId = pr.Id,
                    SkuId = item.SkuId,
                    Quantity = item.Quantity,
                    Unit = item.Unit ?? "PCS",
                    EstimatedUnitPrice = item.EstimatedUnitPrice,
                    Currency = item.Currency ?? "TWD",
                    RequiredDate = item.RequiredDate,
                    Notes = item.Notes
                });
                total += item.Quantity * item.EstimatedUnitPrice;
            }
            pr.TotalEstimatedAmount = total;
        }

        await _db.SaveChangesAsync();
        return ApiResponse.Ok("PR updated");
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(Guid id, [FromBody] UpdatePrStatusRequest req)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id);
        if (pr == null) return NotFound(ApiHelper.Fail("PR not found"));

        pr.Status = req.Status;
        await _db.SaveChangesAsync();

        return ApiResponse.Ok("Status updated");
    }

    [HttpPatch("{id}/lock")]
    public async Task<ActionResult<ApiResponse>> ToggleLock(Guid id)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id);
        if (pr == null) return NotFound(ApiHelper.Fail("PR not found"));

        pr.IsLocked = !pr.IsLocked;
        await _db.SaveChangesAsync();

        return ApiResponse.Ok(pr.IsLocked ? "PR locked" : "PR unlocked");
    }

    [HttpPost("{id}/submit")]
    public async Task<ActionResult<ApiResponse>> SubmitPR(Guid id)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id);
        if (pr == null) return NotFound(ApiHelper.Fail("PR not found"));

        if (pr.Status != "DRAFT")
            return BadRequest(ApiHelper.Fail("Only DRAFT PR can be submitted"));

        if (pr.TotalEstimatedAmount <= 0)
            return BadRequest(ApiHelper.Fail("PR must have items with estimated amount"));

        var chainResult = await _approvalService.GetApprovalChainAsync("PR", pr.TotalEstimatedAmount);
        
        pr.WorkflowStatus = "SUBMITTED";
        pr.Status = "SUBMITTED";
        pr.CurrentLevel = chainResult.TotalLevels > 0 ? 1 : 0;
        
        await _db.SaveChangesAsync();
        return ApiResponse.Ok("PR submitted for approval");
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult<ApiResponse>> ApprovePR(Guid id, [FromBody] ApprovalActionRequest req)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id);
        if (pr == null) return NotFound(ApiHelper.Fail("PR not found"));

        if (pr.WorkflowStatus != "SUBMITTED" && pr.WorkflowStatus != "UNDER_REVIEW")
            return BadRequest(ApiHelper.Fail("PR is not pending approval"));

        if (req.EmployeeId == Guid.Empty)
            return BadRequest(ApiHelper.Fail("Employee ID required"));

        var chainResult = await _approvalService.GetApprovalChainAsync("PR", pr.TotalEstimatedAmount);
        
        pr.WorkflowStatus = "APPROVED";
        pr.Status = "APPROVED";
        
        _db.PurchaseRequisitionApprovals.Add(new PurchaseRequisitionApproval
        {
            PurchaseRequisitionId = pr.Id,
            ApprovalLevel = pr.CurrentLevel,
            ApproverId = req.EmployeeId,
            Action = "APPROVE",
            Comments = req.Comments,
            ActionDate = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return ApiResponse.Ok("PR approved");
    }

    [HttpPost("{id}/reject")]
    public async Task<ActionResult<ApiResponse>> RejectPR(Guid id, [FromBody] ApprovalActionRequest req)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id);
        if (pr == null) return NotFound(ApiHelper.Fail("PR not found"));

        if (pr.WorkflowStatus != "SUBMITTED" && pr.WorkflowStatus != "UNDER_REVIEW")
            return BadRequest(ApiHelper.Fail("PR is not pending approval"));

        if (req.EmployeeId == Guid.Empty)
            return BadRequest(ApiHelper.Fail("Employee ID required"));

        pr.WorkflowStatus = "REJECTED";
        pr.Status = "REJECTED";

        _db.PurchaseRequisitionApprovals.Add(new PurchaseRequisitionApproval
        {
            PurchaseRequisitionId = pr.Id,
            ApprovalLevel = pr.CurrentLevel,
            ApproverId = req.EmployeeId,
            Action = "REJECT",
            Comments = req.Comments,
            ActionDate = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return ApiResponse.Ok("PR rejected");
    }

    [HttpPost("{id}/return-for-edit")]
    public async Task<ActionResult<ApiResponse>> ReturnPRForEdit(Guid id, [FromBody] ApprovalActionRequest req)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id);
        if (pr == null) return NotFound(ApiHelper.Fail("PR not found"));

        if (pr.WorkflowStatus != "SUBMITTED" && pr.WorkflowStatus != "UNDER_REVIEW")
            return BadRequest(ApiHelper.Fail("PR is not pending approval"));

        if (req.EmployeeId == Guid.Empty)
            return BadRequest(ApiHelper.Fail("Employee ID required"));

        pr.WorkflowStatus = "RETURNED_FOR_EDITS";
        pr.Status = "RETURNED";

        _db.PurchaseRequisitionApprovals.Add(new PurchaseRequisitionApproval
        {
            PurchaseRequisitionId = pr.Id,
            ApprovalLevel = pr.CurrentLevel,
            ApproverId = req.EmployeeId,
            Action = "RETURN",
            Comments = req.Comments,
            ActionDate = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return ApiResponse.Ok("PR returned for edits");
    }

    [HttpGet("{id}/approval-chain")]
    public async Task<ActionResult<ApiResponse<ApprovalChainResult>>> GetApprovalChain(Guid id)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id);
        if (pr == null) return NotFound(ApiHelper.Fail("PR not found"));

        var chainResult = await _approvalService.GetApprovalChainAsync("PR", pr.TotalEstimatedAmount);
        return Ok(chainResult);
    }

    [HttpGet("{id}/approval-history")]
    public async Task<ActionResult<ApiResponse<List<ApprovalHistoryDto>>>> GetApprovalHistory(Guid id)
    {
        var approvals = await _db.PurchaseRequisitionApprovals
            .Include(a => a.Approver)
            .Where(a => a.PurchaseRequisitionId == id)
            .OrderBy(a => a.ActionDate)
            .ToListAsync();

        var dtos = approvals.Select(a => new ApprovalHistoryDto
        {
            Id = a.Id,
            Level = a.ApprovalLevel,
            ApproverName = a.Approver?.ChineseName,
            Action = a.Action,
            Comments = a.Comments,
            ActionDate = a.ActionDate
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost("{id}/convert-to-po")]
    public async Task<ActionResult<ApiResponse>> ConvertToPO(Guid id, [FromBody] ConvertToPoRequest req)
    {
        var pr = await _db.PurchaseRequisitions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pr == null) return NotFound(ApiHelper.Fail("PR not found"));

        if (pr.Status != "APPROVED" && pr.WorkflowStatus != "APPROVED")
            return BadRequest(ApiHelper.Fail("Only APPROVED PR can be converted to PO"));

        if (!pr.Items.Any())
            return BadRequest(ApiHelper.Fail("PR has no items"));

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = DocumentNumberGenerator.GeneratePurchaseOrder(),
            PoDate = DateTime.UtcNow,
            SupplierId = req.SupplierId,
            PrId = pr.Id,
            Notes = pr.Purpose,
            Status = "pending",
            TotalAmount = 0
        };

        decimal total = 0;
        foreach (var item in pr.Items)
        {
            po.Items.Add(new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = po.Id,
                SkuId = item.SkuId,
                Quantity = item.Quantity,
                Unit = item.Unit,
                UnitPrice = item.EstimatedUnitPrice
            });
            total += item.Quantity * item.EstimatedUnitPrice;
        }
        po.TotalAmount = total;

        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync();

        return ApiResponse.Ok(new { poId = po.Id, poNumber = po.PoNumber });
    }

    [HttpGet("budget-check")]
    public async Task<ActionResult<ApiResponse<BudgetOverrunCheck>>> CheckBudget([FromQuery] string costCenter, [FromQuery] decimal amount)
    {
        using var scope = _serviceProvider.CreateScope();
        var budgetService = scope.ServiceProvider.GetRequiredService<IBudgetControlService>();
        var result = await budgetService.CheckOverrunAsync(costCenter, amount);
        return Ok(result);
    }
}
