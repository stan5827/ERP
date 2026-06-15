using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.Models.Sales;
using ErpBackend.DTOs;

namespace ErpBackend.Services;

public interface IMpsService
{
    Task<MpsHeader> CreateMpsVersionAsync(CreateMpsRequest request, Guid userId);
    Task<List<MpsHeaderDto>> GetMpsHeadersAsync();
    Task<List<MpsHeaderDto>> GetMpsVersionsAsync(string planName);
    Task<MpsHeaderDto?> GetLatestApprovedMpsAsync(string planName);
    Task<MpsDetailDto?> GetMpsDetailAsync(Guid id);
    Task<bool> RunConsumptionAsync(Guid mpsHeaderId);
    Task<List<MpsException>> GetExceptionsAsync(Guid mpsHeaderId);
    Task<RccpStatusDto> GetRccpStatusAsync(Guid mpsHeaderId);
    Task<AggregateRccpDto> GetAggregateRccpStatusAsync(DateTime start, DateTime end, int page = 1, int pageSize = 20);
}

public class MpsService : IMpsService
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<MpsService> _logger;

    public MpsService(ErpGeneralDbContext db, ILogger<MpsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<MpsHeader> CreateMpsVersionAsync(CreateMpsRequest request, Guid userId)
    {
        var lastVersion = await _db.MpsHeaders
            .Where(h => h.PlanName == request.PlanName)
            .OrderByDescending(h => h.VersionNo)
            .FirstOrDefaultAsync();

        var header = new MpsHeader
        {
            MpsNo = $"MPS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
            PlanName = request.PlanName,
            PlantCode = "HQ",
            HorizonStart = request.HorizonStart,
            HorizonEnd = request.HorizonEnd,
            TimeBucketType = request.TimeBucketType,
            VersionNo = (lastVersion?.VersionNo ?? 0) + 1,
            Status = "DRAFT",
            BaseOn = request.BaseOn,
            BackwardConsumptionDays = request.BackwardConsumptionDays,
            ForwardConsumptionDays = request.ForwardConsumptionDays,
            ConsumptionStrategy = request.ConsumptionStrategy,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var l in request.Lines)
        {
            header.Lines.Add(new MpsLine
            {
                SkuId = l.SkuId,
                BucketStart = l.BucketStart,
                BucketEnd = l.BucketEnd,
                BucketCode = l.BucketCode,
                PlanQty = l.PlanQty,
                AdjustedQty = l.PlanQty, // Initial
                Notes = l.Notes
            });
        }

        _db.MpsHeaders.Add(header);
        await _db.SaveChangesAsync();
        
        // Auto-run consumption after creation
        await RunConsumptionAsync(header.Id);
        
        return header;
    }

    public async Task<List<MpsHeaderDto>> GetMpsHeadersAsync()
    {
        return await _db.MpsHeaders
            .AsNoTracking()
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new MpsHeaderDto
            {
                Id = h.Id,
                MpsNo = h.MpsNo,
                PlanName = h.PlanName,
                HorizonStart = h.HorizonStart,
                HorizonEnd = h.HorizonEnd,
                Status = h.Status,
                VersionNo = h.VersionNo,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync();
    }
    
    /// <summary>
    /// 修復 MRP-001: 取得指定 PlanName 的所有版本（支援多版本並存）
    /// </summary>
    public async Task<List<MpsHeaderDto>> GetMpsVersionsAsync(string planName)
    {
        return await _db.MpsHeaders
            .AsNoTracking()
            .Where(h => h.PlanName == planName)
            .OrderByDescending(h => h.VersionNo)
            .Select(h => new MpsHeaderDto
            {
                Id = h.Id,
                MpsNo = h.MpsNo,
                PlanName = h.PlanName,
                HorizonStart = h.HorizonStart,
                HorizonEnd = h.HorizonEnd,
                Status = h.Status,
                VersionNo = h.VersionNo,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync();
    }
    
    /// <summary>
    /// 修復 MRP-001: 取得指定 PlanName 的最新已核准版本
    /// </summary>
    public async Task<MpsHeaderDto?> GetLatestApprovedMpsAsync(string planName)
    {
        return await _db.MpsHeaders
            .AsNoTracking()
            .Where(h => h.PlanName == planName && h.Status == "APPROVED")
            .OrderByDescending(h => h.VersionNo)
            .Select(h => new MpsHeaderDto
            {
                Id = h.Id,
                MpsNo = h.MpsNo,
                PlanName = h.PlanName,
                HorizonStart = h.HorizonStart,
                HorizonEnd = h.HorizonEnd,
                Status = h.Status,
                VersionNo = h.VersionNo,
                CreatedAt = h.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<MpsDetailDto?> GetMpsDetailAsync(Guid id)
    {
        var header = await _db.MpsHeaders
            .Include(h => h.Lines)
            .ThenInclude(l => l.Sku)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (header == null) return null;

        return new MpsDetailDto
        {
            Id = header.Id,
            MpsNo = header.MpsNo,
            PlanName = header.PlanName,
            HorizonStart = header.HorizonStart,
            HorizonEnd = header.HorizonEnd,
            Status = header.Status,
            VersionNo = header.VersionNo,
            Lines = header.Lines.Select(l => new MpsLineDto
            {
                Id = l.Id,
                SkuId = l.SkuId,
                SkuNumber = l.Sku.SkuNumber,
                SkuName = l.Sku.SkuName,
                BucketStart = l.BucketStart,
                BucketEnd = l.BucketEnd,
                PlanQty = l.PlanQty,
                ConsumedQty = l.ConsumedQty,
                AdjustedQty = l.AdjustedQty,
                OverrideFlag = l.OverrideFlag,
                Notes = l.Notes
            }).ToList()
        };
    }

    public async Task<bool> RunConsumptionAsync(Guid mpsHeaderId)
    {
        var header = await _db.MpsHeaders
            .Include(h => h.Lines)
            .FirstOrDefaultAsync(h => h.Id == mpsHeaderId);

        if (header == null) return false;

        // 1. Reset current consumption
        var lines = header.Lines.ToList();
        foreach (var line in lines)
        {
            line.ConsumedQty = 0;
            line.AdjustedQty = line.PlanQty;
        }
        
        var oldDetails = await _db.ForecastConsumptionDetails
            .Where(d => lines.Select(l => l.Id).Contains(d.MpsLineId))
            .ToListAsync();
        _db.ForecastConsumptionDetails.RemoveRange(oldDetails);

        // 2. Get relevant Sales Orders
        // Focus on SO items within horizon and for SKUs in this MPS
        var skuIds = lines.Select(l => l.SkuId).Distinct().ToList();
        var salesOrders = await _db.SalesOrderItems
            .Include(soi => soi.SalesOrder)
            .Where(soi => soi.SkuId.HasValue && skuIds.Contains(soi.SkuId.Value) 
                       && soi.SalesOrder.Status == "confirmed"
                       && soi.PlannedFinishDate >= header.HorizonStart 
                       && soi.PlannedFinishDate <= header.HorizonEnd)
            .OrderBy(soi => soi.PlannedFinishDate)
            .ToListAsync();

        foreach (var so in salesOrders)
        {
            decimal remainingSoQty = so.Quantity - so.DeliveredQty;
            if (remainingSoQty <= 0) continue;

            // Strategy: BACKWARD_THEN_FORWARD
            // 1. Precise bucket
            var targetLine = lines.FirstOrDefault(l => l.SkuId == so.SkuId 
                                                  && l.BucketStart <= so.PlannedFinishDate 
                                                  && l.BucketEnd >= so.PlannedFinishDate);

            if (targetLine != null)
            {
                remainingSoQty = Consume(targetLine, so, remainingSoQty);
            }

            // 2. Backward
            if (remainingSoQty > 0 && header.BackwardConsumptionDays > 0)
            {
                var backwardLimit = so.PlannedFinishDate?.AddDays(-header.BackwardConsumptionDays);
                var backwardLines = lines
                    .Where(l => l.SkuId == so.SkuId 
                             && l.BucketEnd < (targetLine?.BucketStart ?? so.PlannedFinishDate)
                             && l.BucketEnd >= backwardLimit)
                    .OrderByDescending(l => l.BucketEnd)
                    .ToList();

                foreach (var bl in backwardLines)
                {
                    remainingSoQty = Consume(bl, so, remainingSoQty);
                    if (remainingSoQty <= 0) break;
                }
            }

            // 3. Forward
            if (remainingSoQty > 0 && header.ForwardConsumptionDays > 0)
            {
                var forwardLimit = so.PlannedFinishDate?.AddDays(header.ForwardConsumptionDays);
                var forwardLines = lines
                    .Where(l => l.SkuId == so.SkuId 
                             && l.BucketStart > (targetLine?.BucketEnd ?? so.PlannedFinishDate)
                             && l.BucketStart <= forwardLimit)
                    .OrderBy(l => l.BucketStart)
                    .ToList();

                foreach (var fl in forwardLines)
                {
                    remainingSoQty = Consume(fl, so, remainingSoQty);
                    if (remainingSoQty <= 0) break;
                }
            }
            
            // If still remaining, it's an "Unconsumed Demand" exception
            if (remainingSoQty > 0)
            {
                _db.MpsExceptions.Add(new MpsException
                {
                    MpsHeaderId = header.Id,
                    SkuId = so.SkuId,
                    Severity = "WARNING",
                    Message = $"Sales Order {so.SalesOrder.OrderNumber} line has {remainingSoQty} unconsumed by any MPS slot."
                });
            }
        }

        // 3. Finalize AdjustedQty
        foreach (var line in lines)
        {
            // AdjustedQty is what MRP will see.
            // If we planned 100, and SO is 120, we produce 120.
            // If we planned 100, and SO is 20, we still produce 100 (keep the forecast buffer)
            // or we produce Max(Plan, Consumed).
            line.AdjustedQty = Math.Max(line.PlanQty, line.ConsumedQty);
        }

        header.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    private decimal Consume(MpsLine line, SalesOrderItem so, decimal qty)
    {
        // How much can this line "absorb" of the forecast?
        // Traditionally, consumption is subtracting SO from Forecast.
        // If Forecast is 100, SO is 30, then Net Forecast is 70.
        // But we track it via ConsumedQty.
        
        line.ConsumedQty += qty;
        
        _db.ForecastConsumptionDetails.Add(new ForecastConsumptionDetail
        {
            MpsLineId = line.Id,
            SoNo = so.SalesOrder.OrderNumber,
            SoLineId = so.Id,
            ConsumedQty = qty,
            ConsumedAt = DateTime.UtcNow
        });

        return 0; // Fully consumed in logic for now (assuming infinite capacity in forecast slot)
        // Note: Real MPS might limit consumption to PlanQty if it's "hard" allocation,
        // but usually, real orders just replace forecast.
    }

    public async Task<List<MpsException>> GetExceptionsAsync(Guid mpsHeaderId)
    {
        return await _db.MpsExceptions
            .Where(e => e.MpsHeaderId == mpsHeaderId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<RccpStatusDto> GetRccpStatusAsync(Guid mpsHeaderId)
    {
        var header = await _db.MpsHeaders
            .Include(h => h.Lines)
            .FirstOrDefaultAsync(h => h.Id == mpsHeaderId);

        if (header == null) return new RccpStatusDto();

        var result = new RccpStatusDto { MpsHeaderId = mpsHeaderId };

        // 1. Get all SKU resources (Capacity Bill)
        var skuIds = header.Lines.Select(l => l.SkuId).Distinct().ToList();
        var bills = await _db.RccpItemResources
            .Include(b => b.Resource)
            .Where(b => skuIds.Contains(b.SkuId))
            .ToListAsync();

        if (!bills.Any()) return result;

        var resources = bills.Select(b => b.Resource).DistinctBy(r => r.Id).ToList();

        // 2. Aggregate Load per Resource + Bucket
        var loadMap = new Dictionary<(Guid ResourceId, string BucketCode), decimal>();

        foreach (var line in header.Lines)
        {
            var itemResources = bills.Where(b => b.SkuId == line.SkuId);
            foreach (var ir in itemResources)
            {
                var bucketCode = line.BucketCode ?? "UNKNOWN";
                var key = (ir.ResourceId, bucketCode);
                var load = line.AdjustedQty * ir.UsagePerUnit;
                
                if (loadMap.ContainsKey(key)) loadMap[key] += load;
                else loadMap[key] = load;
            }
        }

        // 3. Compare with Capacity
        foreach (var res in resources)
        {
            // For now, simplify: use the same BucketCodes found in MPS
            var bucketCodes = header.Lines.Select(l => l.BucketCode).Distinct();
            foreach (var code in bucketCodes)
            {
                var bucketCode = code ?? "UNKNOWN";
                var key = (res.Id, bucketCode);
                var required = loadMap.ContainsKey(key) ? loadMap[key] : 0;
                
                // Get bucket dates from one of the lines
                var sampleLine = header.Lines.First(l => l.BucketCode == code);

                result.Loads.Add(new ResourceLoadDto
                {
                    ResourceId = res.Id,
                    ResourceCode = res.ResourceCode,
                    ResourceName = res.ResourceName,
                    BucketCode = code ?? "UNKNOWN",
                    BucketStart = sampleLine.BucketStart,
                    BucketEnd = sampleLine.BucketEnd,
                    RequiredCapacity = required,
                    AvailableCapacity = res.CapacityPerBucket * res.Efficiency * res.Utilization
                });
            }
        }

        return result;
    }

    public async Task<AggregateRccpDto> GetAggregateRccpStatusAsync(DateTime start, DateTime end, int page = 1, int pageSize = 20)
    {
        var result = new AggregateRccpDto { StartDate = start, EndDate = end };
        
        // 修復 MPS-005: 分頁處理，防止 OOM
        pageSize = Math.Clamp(pageSize, 1, 50); // 最多50筆
        var skip = (page - 1) * pageSize;

        // 1. Get current active MPS versions (Latest version per PlanName)
        var latestHeaders = await _db.MpsHeaders
            .Where(h => h.Status != "DRAFT" && h.Status != "CLOSED")
            .GroupBy(h => h.PlanName)
            .Select(g => g.OrderByDescending(h => h.VersionNo).First())
            .ToListAsync();

        if (!latestHeaders.Any()) return result;

        var headerIds = latestHeaders.Select(h => h.Id).ToList();
        var lines = await _db.MpsLines
            .Where(l => headerIds.Contains(l.MpsHeaderId) && l.BucketStart >= start && l.BucketStart <= end)
            .ToListAsync();

        if (!lines.Any()) return result;

        // 2. Get all involved Capacity Bills
        var skuIds = lines.Select(l => l.SkuId).Distinct().ToList();
        var bills = await _db.RccpItemResources
            .Include(b => b.Resource)
            .Where(b => skuIds.Contains(b.SkuId))
            .ToListAsync();

        var resources = await _db.RccpResources.Where(r => r.IsActive).ToListAsync();
        
        // 修復 MPS-005: 限制資源數量並套用分頁
        var totalResources = resources.Count;
        result.Page = page;
        result.PageSize = pageSize;
        result.TotalCount = totalResources;
        result.TotalPages = (int)Math.Ceiling((double)totalResources / pageSize);
        resources = resources.Skip(skip).Take(pageSize).ToList();

        // 3. Generate 12 weekly buckets (or buckets between start/end)
        var buckets = new List<(DateTime Start, DateTime End, string Code)>();
        var current = start;
        int i = 1;
        while (current < end)
        {
            var bEnd = current.AddDays(7).AddSeconds(-1);
            buckets.Add((current, bEnd, $"W{i}"));
            current = current.AddDays(7);
            i++;
        }

        // 4. Aggregate Load
        foreach (var res in resources)
        {
            var resDto = new ResourceAggregateLoadDto
            {
                ResourceId = res.Id,
                ResourceCode = res.ResourceCode,
                ResourceName = res.ResourceName,
                ResourceType = res.ResourceType,
                Uom = res.Uom
            };

            foreach (var b in buckets)
            {
                // Find MPS lines touching this bucket
                // For simplicity, find lines whose BucketStart falls in this bucket
                var bucketLines = lines.Where(l => l.BucketStart >= b.Start && l.BucketStart < b.End).ToList();
                
                decimal totalRequired = 0;
                foreach (var bl in bucketLines)
                {
                    var consumption = bills.FirstOrDefault(bill => bill.SkuId == bl.SkuId && bill.ResourceId == res.Id);
                    if (consumption != null)
                    {
                        totalRequired += bl.AdjustedQty * consumption.UsagePerUnit;
                    }
                }

                var weekly = new WeeklyLoadDto
                {
                    WeekCode = b.Code,
                    WeekStart = b.Start,
                    Required = totalRequired,
                    Available = res.CapacityPerBucket * res.Efficiency * res.Utilization
                };
                resDto.WeeklyLoads.Add(weekly);
            }

            resDto.MaxLoadPercentage = resDto.WeeklyLoads.Any() ? resDto.WeeklyLoads.Max(w => w.LoadPercentage) : 0;
            result.Resources.Add(resDto);
        }

        return result;
    }
}
