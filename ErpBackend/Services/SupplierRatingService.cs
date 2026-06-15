using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.Procurement;
using ErpBackend.Models.QA;

namespace ErpBackend.Services;

public interface ISupplierRatingService
{
    Task<SupplierRatingResult> CalculateRatingAsync(Guid supplierId, int months = 12);
    Task<List<SupplierRatingResult>> CalculateAllRatingsAsync(int months = 12);
    Task SaveRatingAsync(Guid supplierId);
    Task RunBatchCalculationAsync(int months = 12);
}

public class SupplierRatingService : ISupplierRatingService
{
    private readonly ErpGeneralDbContext _db;

    public SupplierRatingService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<SupplierRatingResult> CalculateRatingAsync(Guid supplierId, int months = 12)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months);
        var supplier = await _db.Suppliers.FindAsync(supplierId);
        if (supplier == null)
            throw new ArgumentException("Supplier not found");

        var q = await CalculateQualityScoreAsync(supplierId, startDate);
        var d = await CalculateDeliveryScoreAsync(supplierId, startDate);
        var c = await CalculateCostScoreAsync(supplierId, startDate);
        var s = await CalculateServiceScoreAsync(supplierId);

        var totalScore = 0.4m * q + 0.3m * d + 0.2m * c + 0.1m * s;
        var grade = GetGrade(totalScore);

        return new SupplierRatingResult
        {
            SupplierId = supplierId,
            SupplierCode = supplier.SupplierCode,
            SupplierName = supplier.SupplierName,
            QualityScore = q,
            DeliveryScore = d,
            CostScore = c,
            ServiceScore = s,
            TotalScore = totalScore,
            Grade = grade,
            CalculationDate = DateTime.UtcNow,
            PeriodMonths = months
        };
    }

    public async Task<List<SupplierRatingResult>> CalculateAllRatingsAsync(int months = 12)
    {
        var suppliers = await _db.Suppliers.Where(s => s.Status == "active").ToListAsync();
        var results = new List<SupplierRatingResult>();

        foreach (var supplier in suppliers)
        {
            try
            {
                var result = await CalculateRatingAsync(supplier.Id, months);
                results.Add(result);
            }
            catch
            {
                // Skip suppliers with errors
            }
        }

        return results;
    }

    public async Task SaveRatingAsync(Guid supplierId)
    {
        var result = await CalculateRatingAsync(supplierId);

        var existing = await _db.SupplierRatings
            .Where(r => r.SupplierId == supplierId && r.PeriodMonths == result.PeriodMonths)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            existing.QualityScore = result.QualityScore;
            existing.DeliveryScore = result.DeliveryScore;
            existing.CostScore = result.CostScore;
            existing.ServiceScore = result.ServiceScore;
            existing.TotalScore = result.TotalScore;
            existing.Grade = result.Grade;
            existing.CalculatedAt = result.CalculationDate;
        }
        else
        {
            _db.SupplierRatings.Add(new SupplierRating
            {
                Id = Guid.NewGuid(),
                SupplierId = supplierId,
                PeriodMonths = result.PeriodMonths,
                QualityScore = result.QualityScore,
                DeliveryScore = result.DeliveryScore,
                CostScore = result.CostScore,
                ServiceScore = result.ServiceScore,
                TotalScore = result.TotalScore,
                Grade = result.Grade,
                CalculatedAt = result.CalculationDate
            });
        }

        var supplier = await _db.Suppliers.FindAsync(supplierId);
        if (supplier != null)
            supplier.Rating = result.Grade;

        await _db.SaveChangesAsync();
    }

    public async Task RunBatchCalculationAsync(int months = 12)
    {
        var suppliers = await _db.Suppliers.Where(s => s.Status == "active").ToListAsync();

        foreach (var supplier in suppliers)
        {
            try
            {
                await SaveRatingAsync(supplier.Id);
            }
            catch
            {
                // Continue with next supplier
            }
        }
    }

    private async Task<decimal> CalculateQualityScoreAsync(Guid supplierId, DateTime startDate)
    {
        var goodsReceipts = await _db.GoodsReceipts
            .Include(g => g.Items)
            .Where(g => g.SupplierId == supplierId && g.GrDate >= startDate)
            .ToListAsync();

        if (!goodsReceipts.Any())
            return 50m;

        var totalReceived = goodsReceipts.Sum(g => g.Items.Sum(i => i.ReceivedQty));
        var totalDefective = goodsReceipts.Sum(g => g.Items.Sum(i => i.RejectedQty));

        if (totalReceived == 0)
            return 50m;

        var defectRate = (decimal)totalDefective / totalReceived * 100;

        if (defectRate <= 0.1m) return 100m;
        if (defectRate <= 0.5m) return 90m;
        if (defectRate <= 1m) return 80m;
        if (defectRate <= 2m) return 70m;
        return 50m;
    }

    private async Task<decimal> CalculateDeliveryScoreAsync(Guid supplierId, DateTime startDate)
    {
        var purchaseOrders = await _db.PurchaseOrders
            .Where(po => po.SupplierId == supplierId && po.PoDate >= startDate)
            .ToListAsync();

        if (!purchaseOrders.Any())
            return 50m;

        var totalPos = purchaseOrders.Count;
        var onTimePos = purchaseOrders.Count(po => po.ExpectedDeliveryDate.HasValue &&
            po.UpdatedAt <= po.ExpectedDeliveryDate.Value.AddDays(2));

        var otdRate = (decimal)onTimePos / totalPos * 100;

        if (otdRate >= 98) return 100m;
        if (otdRate >= 95) return 90m;
        if (otdRate >= 90) return 80m;
        if (otdRate >= 80) return 70m;
        return 60m;
    }

    private async Task<decimal> CalculateCostScoreAsync(Guid supplierId, DateTime startDate)
    {
        var poItems = await _db.PurchaseOrderItems
            .Include(i => i.PurchaseOrder)
            .Where(i => i.PurchaseOrder != null && i.PurchaseOrder.SupplierId == supplierId &&
                i.PurchaseOrder.PoDate >= startDate)
            .ToListAsync();

        if (!poItems.Any())
            return 50m;

        var avgPrice = poItems.Average(i => i.UnitPrice);
        var baselinePrice = avgPrice * 0.95m;
        var variance = (avgPrice - baselinePrice) / baselinePrice * 100;

        if (variance <= 0) return 100m;
        if (variance <= 5) return 90m;
        if (variance <= 10) return 80m;
        if (variance <= 20) return 70m;
        return 60m;
    }

    private async Task<decimal> CalculateServiceScoreAsync(Guid supplierId)
    {
        var lastRating = await _db.SupplierRatings
            .Where(r => r.SupplierId == supplierId)
            .OrderByDescending(r => r.CalculatedAt)
            .FirstOrDefaultAsync();

        return lastRating?.ServiceScore ?? 80m;
    }

    private static string GetGrade(decimal score)
    {
        if (score >= 90) return "A";
        if (score >= 80) return "B";
        if (score >= 70) return "C";
        return "D";
    }
}

public class SupplierRatingResult
{
    public Guid SupplierId { get; set; }
    public string SupplierCode { get; set; } = null!;
    public string SupplierName { get; set; } = null!;
    public decimal QualityScore { get; set; }
    public decimal DeliveryScore { get; set; }
    public decimal CostScore { get; set; }
    public decimal ServiceScore { get; set; }
    public decimal TotalScore { get; set; }
    public string Grade { get; set; } = "C";
    public DateTime CalculationDate { get; set; }
    public int PeriodMonths { get; set; }
}

public class SupplierRating
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierId { get; set; }
    public int PeriodMonths { get; set; }
    public decimal QualityScore { get; set; }
    public decimal DeliveryScore { get; set; }
    public decimal CostScore { get; set; }
    public decimal ServiceScore { get; set; }
    public decimal TotalScore { get; set; }
    public string Grade { get; set; } = "C";
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}