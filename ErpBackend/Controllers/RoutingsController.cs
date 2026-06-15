using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

[Route("api/erp/routings")]
public class RoutingsController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public RoutingsController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoutingDto>>>> GetRoutings([FromQuery] Guid? productSkuId)
    {
        var query = _db.Routings
            .Include(r => r.ProductSku)
            .Include(r => r.Operations)
                .ThenInclude(ro => ro.Operation)
            .Include(r => r.Operations)
                .ThenInclude(ro => ro.WorkCenter)
            .AsQueryable();

        if (productSkuId.HasValue)
            query = query.Where(r => r.ProductSkuId == productSkuId.Value);

        var items = await query
            .OrderBy(r => r.RoutingCode)
            .Select(r => MapToDto(r))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RoutingDto>>> GetRouting(Guid id)
    {
        var r = await _db.Routings
            .Include(r => r.ProductSku)
            .Include(r => r.Operations.OrderBy(o => o.OperationSeq))
                .ThenInclude(ro => ro.Operation)
            .Include(r => r.Operations)
                .ThenInclude(ro => ro.WorkCenter)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (r == null) return NotFound(ApiHelper.FailOf<RoutingDto>("找不到工藝"));

        return Ok(MapToDto(r));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoutingDto>>> CreateRouting([FromBody] CreateRoutingRequest request)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var routing = new Routing
            {
                RoutingCode = request.RoutingCode,
                RoutingName = request.RoutingName,
                ProductSkuId = request.ProductSkuId,
                Version = request.Version,
                IsActive = request.IsActive
            };

            _db.Routings.Add(routing);
            await _db.SaveChangesAsync();

            if (request.Operations != null)
            {
                foreach (var opReq in request.Operations)
                {
                    _db.RoutingOperations.Add(new RoutingOperation
                    {
                        RoutingId = routing.Id,
                        OperationSeq = opReq.OperationSeq,
                        OperationId = opReq.OperationId,
                        WorkCenterId = opReq.WorkCenterId,
                        TransferTime = opReq.TransferTime,
                        WaitTime = opReq.WaitTime
                    });
                }
                await _db.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            
            return await GetRouting(routing.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ApiHelper.FailOf<RoutingDto>($"建立工藝失敗: {ex.Message}"));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<RoutingDto>>> UpdateRouting(Guid id, [FromBody] UpdateRoutingRequest request)
    {
        var routing = await _db.Routings
            .Include(r => r.Operations)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (routing == null) return NotFound(ApiHelper.FailOf<RoutingDto>("找不到工藝"));

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            if (request.RoutingCode != null) routing.RoutingCode = request.RoutingCode;
            if (request.RoutingName != null) routing.RoutingName = request.RoutingName;
            if (request.Version != null) routing.Version = request.Version;
            if (request.IsActive.HasValue) routing.IsActive = request.IsActive.Value;

            if (request.Operations != null)
            {
                // Simple strategy: replace all operations
                _db.RoutingOperations.RemoveRange(routing.Operations);
                foreach (var opReq in request.Operations)
                {
                    _db.RoutingOperations.Add(new RoutingOperation
                    {
                        RoutingId = routing.Id,
                        OperationSeq = opReq.OperationSeq,
                        OperationId = opReq.OperationId,
                        WorkCenterId = opReq.WorkCenterId,
                        TransferTime = opReq.TransferTime,
                        WaitTime = opReq.WaitTime
                    });
                }
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetRouting(id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ApiHelper.FailOf<RoutingDto>($"更新工藝失敗: {ex.Message}"));
        }
    }

    private static RoutingDto MapToDto(Routing r) => new RoutingDto
    {
        Id = r.Id,
        RoutingCode = r.RoutingCode,
        RoutingName = r.RoutingName,
        ProductSkuId = r.ProductSkuId,
        ProductSkuNumber = r.ProductSku?.SkuNumber,
        ProductSkuName = r.ProductSku?.SkuName,
        Version = r.Version,
        IsActive = r.IsActive,
        Operations = r.Operations.Select(ro => new RoutingOperationDto
        {
            Id = ro.Id,
            RoutingId = ro.RoutingId,
            OperationSeq = ro.OperationSeq,
            OperationId = ro.OperationId,
            WorkCenterId = ro.WorkCenterId,
            TransferTime = ro.TransferTime,
            WaitTime = ro.WaitTime,
            OperationCode = ro.Operation?.OperationCode,
            OperationName = ro.Operation?.OperationName,
            WorkCenterCode = ro.WorkCenter?.WorkCenterCode,
            WorkCenterName = ro.WorkCenter?.WorkCenterName
        }).ToList()
    };
}
