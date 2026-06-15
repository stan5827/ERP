using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

[Route("api/erp/operations")]
public class OperationsController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public OperationsController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<OperationDto>>>> GetOperations()
    {
        var items = await _db.Operations
            .Where(o => o.IsActive)
            .OrderBy(o => o.OperationCode)
            .Select(o => new OperationDto
            {
                Id = o.Id,
                OperationCode = o.OperationCode,
                OperationName = o.OperationName,
                SetupTime = o.SetupTime,
                RunTime = o.RunTime,
                CostPerHour = o.CostPerHour,
                IsOutsourced = o.IsOutsourced,
                IsActive = o.IsActive
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<OperationDto>>> GetOperation(Guid id)
    {
        var o = await _db.Operations.FindAsync(id);
        if (o == null) return NotFound(ApiHelper.FailOf<OperationDto>("找不到工序"));

        return Ok(new OperationDto
        {
            Id = o.Id,
            OperationCode = o.OperationCode,
            OperationName = o.OperationName,
            SetupTime = o.SetupTime,
            RunTime = o.RunTime,
            CostPerHour = o.CostPerHour,
            IsOutsourced = o.IsOutsourced,
            IsActive = o.IsActive
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OperationDto>>> CreateOperation([FromBody] CreateOperationRequest request)
    {
        var op = new Operation
        {
            OperationCode = request.OperationCode,
            OperationName = request.OperationName,
            SetupTime = request.SetupTime,
            RunTime = request.RunTime,
            CostPerHour = request.CostPerHour,
            IsOutsourced = request.IsOutsourced,
            IsActive = request.IsActive
        };

        _db.Operations.Add(op);
        await _db.SaveChangesAsync();

        return Ok(new OperationDto
        {
            Id = op.Id,
            OperationCode = op.OperationCode,
            OperationName = op.OperationName,
            SetupTime = op.SetupTime,
            RunTime = op.RunTime,
            CostPerHour = op.CostPerHour,
            IsOutsourced = op.IsOutsourced,
            IsActive = op.IsActive
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<OperationDto>>> UpdateOperation(Guid id, [FromBody] UpdateOperationRequest request)
    {
        var op = await _db.Operations.FindAsync(id);
        if (op == null) return NotFound(ApiHelper.FailOf<OperationDto>("找不到工序"));

        if (request.OperationCode != null) op.OperationCode = request.OperationCode;
        if (request.OperationName != null) op.OperationName = request.OperationName;
        if (request.SetupTime.HasValue) op.SetupTime = request.SetupTime.Value;
        if (request.RunTime.HasValue) op.RunTime = request.RunTime.Value;
        if (request.CostPerHour.HasValue) op.CostPerHour = request.CostPerHour.Value;
        if (request.IsOutsourced.HasValue) op.IsOutsourced = request.IsOutsourced.Value;
        if (request.IsActive.HasValue) op.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();

        return Ok(new OperationDto
        {
            Id = op.Id,
            OperationCode = op.OperationCode,
            OperationName = op.OperationName,
            SetupTime = op.SetupTime,
            RunTime = op.RunTime,
            CostPerHour = op.CostPerHour,
            IsOutsourced = op.IsOutsourced,
            IsActive = op.IsActive
        });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteOperation(Guid id)
    {
        var op = await _db.Operations.FindAsync(id);
        if (op == null) return NotFound(ApiHelper.Fail("找不到工序"));

        // Check if used in any routing
        var isUsed = await _db.RoutingOperations.AnyAsync(ro => ro.OperationId == id);
        if (isUsed) return BadRequest(ApiHelper.Fail("該工序已在工藝中使用，無法刪除"));

        _db.Operations.Remove(op);
        await _db.SaveChangesAsync();

        return Ok("工序已刪除");
    }
}
