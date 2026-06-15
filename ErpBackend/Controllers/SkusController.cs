using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Validation;

namespace ErpBackend.Controllers;

public class SkusController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public SkusController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SkuDto>>>> GetSkus(
        [FromQuery] string? category,
        [FromQuery] string? itemType,
        [FromQuery] string? searchTerm)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var query = _db.Skus
            .Include(s => s.DefaultWarehouse)
            .Include(s => s.DefaultSupplier)
            .AsQueryable();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(s => s.Category == category);
        
        if (!string.IsNullOrEmpty(itemType))
            query = query.Where(s => s.ItemType == itemType);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(s => s.SkuNumber.Contains(searchTerm) || s.SkuName.Contains(searchTerm));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.SkuNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(items.Select(MapToDto).ToList(), totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SkuDto>>> GetSku(Guid id)
    {
        var sku = await _db.Skus
            .Include(s => s.DefaultWarehouse)
            .Include(s => s.DefaultSupplier)
            .Include(s => s.PurchaseUom)
            .Include(s => s.SalesUom)
            .Include(s => s.ProductionUom)
            .Include(s => s.CostUom)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sku == null) return NotFound(ApiHelper.FailOf<SkuDto>("找不到指定SKU"));

        return Ok(MapToDto(sku));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SkuDto>>> CreateSku([FromBody] SkuDto dto)
    {
        var rules = BuildSkuValidationRules();
        var validationResult = Validator.ValidateObject(dto, rules);
        if (!validationResult.IsValid)
            return BadRequest(ApiHelper.FailOf<SkuDto>(validationResult.Errors.Values.First()));

        var entity = new Sku
        {
            SkuNumber = dto.SkuNumber,
            SkuName = dto.SkuName,
            Specification = dto.Specification,
            Unit = dto.Unit,
            Category = dto.Category,
            ItemType = dto.ItemType,
            Model = dto.Model,
            Brand = dto.Brand,
            Status = dto.Status ?? "active",
            Qty = dto.Qty,
            SafetyStock = dto.SafetyStock,
            ReorderPoint = dto.ReorderPoint,
            MaxStock = dto.MaxStock,
            LeadTimeDays = dto.LeadTimeDays,
            PurchasePrice = dto.PurchasePrice,
            StandardCost = dto.StandardCost,
            DefaultWarehouseId = dto.DefaultWarehouseId,
            PriceA = dto.PriceA,
            PriceB = dto.PriceB,
            PriceC = dto.PriceC,
            PriceD = dto.PriceD,
            PriceE = dto.PriceE,
            Currency = dto.Currency ?? "TWD",
            IsSellable = dto.IsSellable,
            IsPurchasable = dto.IsPurchasable,
            IsManufacturable = dto.IsManufacturable,
            IsOutsourced = dto.IsOutsourced,
            CostMethod = dto.CostMethod ?? "standard",
            SuggestedPrice = dto.SuggestedPrice,
            BasePrice = dto.BasePrice,
            IsBomTop = dto.IsBomTop,
            IsBomComponent = dto.IsBomComponent,
            IsPhantom = dto.IsPhantom,
            IsMto = dto.IsMto,
            IsMts = dto.IsMts,
            IsCustom = dto.IsCustom,
            MakeOrBuy = dto.MakeOrBuy ?? "buy",
            DefaultSupplierId = dto.DefaultSupplierId,
            SupplierItemCode = dto.SupplierItemCode,
            SupplierLeadTime = dto.SupplierLeadTime,
            DrawingNo = dto.DrawingNo,
            DrawingRevision = dto.DrawingRevision,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            QualitySpec = dto.QualitySpec,
            ProductionDept = dto.ProductionDept,
            MachineCode = dto.MachineCode,
            MoldCode = dto.MoldCode,
            DefaultBomId = dto.DefaultBomId,
            DefaultRoutingId = dto.DefaultRoutingId,
            CostUnit = dto.CostUnit,
            CostUomId = dto.CostUomId,
            LatestPurchaseCost = dto.LatestPurchaseCost,
            TaxCode = dto.TaxCode,
            TaxRate = dto.TaxRate,
            IsSubstitute = dto.IsSubstitute,
            IsAlternate = dto.IsAlternate,
            IqcRequired = dto.IqcRequired,
            OqcRequired = dto.OqcRequired,
            Color = dto.Color,
            PackingMethod = dto.PackingMethod,
            MinPackingQty = dto.MinPackingQty,
            IsStocked = dto.IsStocked,
            MaterialType = dto.MaterialType,
            DesignOwner = dto.DesignOwner,
            ShelfLifeDays = dto.ShelfLifeDays,
            AvgDailyConsumption = dto.AvgDailyConsumption,
            PurchasePriceCeiling = dto.PurchasePriceCeiling,
            FloorPrice = dto.FloorPrice,
            DiscountRate = dto.DiscountRate,
            CommissionAccount = dto.CommissionAccount,
            RelatedSkuId = dto.RelatedSkuId,
            CustomerItemCode = dto.CustomerItemCode,
            BomType = dto.BomType,
            RoutingId = dto.RoutingId,
            SubstituteSkuId = dto.SubstituteSkuId,
            OutsourceSupplierId = dto.OutsourceSupplierId,
            ProcessingFeeAccount = dto.ProcessingFeeAccount,
            OutsourceWarehouseId = dto.OutsourceWarehouseId,
            IsSupplierProvided = dto.IsSupplierProvided,
            PurchaseUomId = dto.PurchaseUomId,
            SalesUomId = dto.SalesUomId,
            ProductionUomId = dto.ProductionUomId,
            PurchaseToBaseFactor = dto.PurchaseToBaseFactor,
            SalesToBaseFactor = dto.SalesToBaseFactor,
            ProductionToBaseFactor = dto.ProductionToBaseFactor
        };

        _db.Skus.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(MapToDto(entity));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<SkuDto>>> UpdateSku(Guid id, [FromBody] SkuDto dto)
    {
        var entity = await _db.Skus.FindAsync(id);
        if (entity == null) return NotFound(ApiHelper.FailOf<SkuDto>("找不到指定SKU"));

        entity.SkuName = dto.SkuName;
        entity.Specification = dto.Specification;
        entity.Unit = dto.Unit;
        entity.Category = dto.Category;
        entity.ItemType = dto.ItemType;
        entity.Model = dto.Model;
        entity.Brand = dto.Brand;
        entity.Status = dto.Status ?? entity.Status;
        entity.Qty = dto.Qty;
        entity.SafetyStock = dto.SafetyStock;
        entity.ReorderPoint = dto.ReorderPoint;
        entity.MaxStock = dto.MaxStock;
        entity.LeadTimeDays = dto.LeadTimeDays;
        entity.PurchasePrice = dto.PurchasePrice;
        entity.StandardCost = dto.StandardCost;
        entity.DefaultWarehouseId = dto.DefaultWarehouseId;
        entity.PriceA = dto.PriceA;
        entity.PriceB = dto.PriceB;
        entity.PriceC = dto.PriceC;
        entity.PriceD = dto.PriceD;
        entity.PriceE = dto.PriceE;
        entity.Currency = dto.Currency ?? entity.Currency;
        entity.IsSellable = dto.IsSellable;
        entity.IsPurchasable = dto.IsPurchasable;
        entity.IsManufacturable = dto.IsManufacturable;
        entity.IsOutsourced = dto.IsOutsourced;
        entity.CostMethod = dto.CostMethod ?? entity.CostMethod;
        entity.SuggestedPrice = dto.SuggestedPrice;
        entity.BasePrice = dto.BasePrice;
        entity.IsBomTop = dto.IsBomTop;
        entity.IsBomComponent = dto.IsBomComponent;
        entity.IsPhantom = dto.IsPhantom;
        entity.IsMto = dto.IsMto;
        entity.IsMts = dto.IsMts;
        entity.IsCustom = dto.IsCustom;
        entity.MakeOrBuy = dto.MakeOrBuy ?? entity.MakeOrBuy;
        entity.DefaultSupplierId = dto.DefaultSupplierId;
        entity.SupplierItemCode = dto.SupplierItemCode;
        entity.SupplierLeadTime = dto.SupplierLeadTime;
        entity.DrawingNo = dto.DrawingNo;
        entity.DrawingRevision = dto.DrawingRevision;
        entity.EffectiveFrom = dto.EffectiveFrom;
        entity.EffectiveTo = dto.EffectiveTo;
        entity.QualitySpec = dto.QualitySpec;
        entity.ProductionDept = dto.ProductionDept;
        entity.MachineCode = dto.MachineCode;
        entity.MoldCode = dto.MoldCode;
        entity.DefaultBomId = dto.DefaultBomId;
        entity.DefaultRoutingId = dto.DefaultRoutingId;
        entity.CostUnit = dto.CostUnit;
        entity.CostUomId = dto.CostUomId;
        entity.LatestPurchaseCost = dto.LatestPurchaseCost;
        entity.TaxCode = dto.TaxCode;
        entity.TaxRate = dto.TaxRate;
        entity.IsSubstitute = dto.IsSubstitute;
        entity.IsAlternate = dto.IsAlternate;
        entity.IqcRequired = dto.IqcRequired;
        entity.OqcRequired = dto.OqcRequired;
        entity.Color = dto.Color;
        entity.PackingMethod = dto.PackingMethod;
        entity.MinPackingQty = dto.MinPackingQty;
        entity.IsStocked = dto.IsStocked;
        entity.MaterialType = dto.MaterialType;
        entity.DesignOwner = dto.DesignOwner;
        entity.ShelfLifeDays = dto.ShelfLifeDays;
        entity.AvgDailyConsumption = dto.AvgDailyConsumption;
        entity.PurchasePriceCeiling = dto.PurchasePriceCeiling;
        entity.FloorPrice = dto.FloorPrice;
        entity.DiscountRate = dto.DiscountRate;
        entity.CommissionAccount = dto.CommissionAccount;
        entity.RelatedSkuId = dto.RelatedSkuId;
        entity.CustomerItemCode = dto.CustomerItemCode;
        entity.BomType = dto.BomType;
        entity.RoutingId = dto.RoutingId;
        entity.SubstituteSkuId = dto.SubstituteSkuId;
        entity.OutsourceSupplierId = dto.OutsourceSupplierId;
        entity.ProcessingFeeAccount = dto.ProcessingFeeAccount;
        entity.OutsourceWarehouseId = dto.OutsourceWarehouseId;
        entity.IsSupplierProvided = dto.IsSupplierProvided;
        entity.PurchaseUomId = dto.PurchaseUomId;
        entity.SalesUomId = dto.SalesUomId;
        entity.ProductionUomId = dto.ProductionUomId;
        entity.PurchaseToBaseFactor = dto.PurchaseToBaseFactor;
        entity.SalesToBaseFactor = dto.SalesToBaseFactor;
        entity.ProductionToBaseFactor = dto.ProductionToBaseFactor;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(entity));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteSku(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(ApiHelper.Fail("無效的SKU ID"));

            var entity = await _db.Skus.FindAsync(id);
            if (entity == null)
                return NotFound(ApiHelper.Fail("找不到該項目，請確認 ID 是否正確"));

            _db.Skus.Remove(entity);
            await _db.SaveChangesAsync();
            
            return Ok("Success");
        }
        catch (Exception)
        {
            return StatusCode(500, ApiHelper.Fail("Error"));
        }
    }

    private SkuDto MapToDto(Sku s) => new SkuDto
    {
        Id = s.Id,
        SkuNumber = s.SkuNumber,
        SkuName = s.SkuName,
        Specification = s.Specification,
        Unit = s.Unit,
        Category = s.Category,
        ItemType = s.ItemType,
        Model = s.Model,
        Brand = s.Brand,
        Status = s.Status,
        Qty = s.Qty,
        SafetyStock = s.SafetyStock,
        ReorderPoint = s.ReorderPoint,
        MaxStock = s.MaxStock,
        LeadTimeDays = s.LeadTimeDays,
        PurchasePrice = s.PurchasePrice,
        StandardCost = s.StandardCost,
        DefaultWarehouseId = s.DefaultWarehouseId,
        DefaultWarehouseName = s.DefaultWarehouse?.WarehouseName,
        PriceA = s.PriceA,
        PriceB = s.PriceB,
        PriceC = s.PriceC,
        PriceD = s.PriceD,
        PriceE = s.PriceE,
        Currency = s.Currency,
        IsSellable = s.IsSellable,
        IsPurchasable = s.IsPurchasable,
        IsManufacturable = s.IsManufacturable,
        IsOutsourced = s.IsOutsourced,
        CostMethod = s.CostMethod,
        SuggestedPrice = s.SuggestedPrice,
        BasePrice = s.BasePrice,
        IsBomTop = s.IsBomTop,
        IsBomComponent = s.IsBomComponent,
        IsPhantom = s.IsPhantom,
        IsMto = s.IsMto,
        IsMts = s.IsMts,
        IsCustom = s.IsCustom,
        MakeOrBuy = s.MakeOrBuy,
        DefaultSupplierId = s.DefaultSupplierId,
        DefaultSupplierName = s.DefaultSupplier?.SupplierName,
        SupplierItemCode = s.SupplierItemCode,
        SupplierLeadTime = s.SupplierLeadTime,
        DrawingNo = s.DrawingNo,
        DrawingRevision = s.DrawingRevision,
        EffectiveFrom = s.EffectiveFrom,
        EffectiveTo = s.EffectiveTo,
        QualitySpec = s.QualitySpec,
        ProductionDept = s.ProductionDept,
        MachineCode = s.MachineCode,
        MoldCode = s.MoldCode,
        DefaultBomId = s.DefaultBomId,
        DefaultRoutingId = s.DefaultRoutingId,
        CostUnit = s.CostUnit,
        CostUomId = s.CostUomId,
        LatestPurchaseCost = s.LatestPurchaseCost,
        TaxCode = s.TaxCode,
        TaxRate = s.TaxRate,
        IsSubstitute = s.IsSubstitute,
        IsAlternate = s.IsAlternate,
        IqcRequired = s.IqcRequired,
        OqcRequired = s.OqcRequired,
        Color = s.Color,
        PackingMethod = s.PackingMethod,
        MinPackingQty = s.MinPackingQty,
        IsStocked = s.IsStocked,
        MaterialType = s.MaterialType,
        DesignOwner = s.DesignOwner,
        ShelfLifeDays = s.ShelfLifeDays,
        AvgDailyConsumption = s.AvgDailyConsumption,
        PurchasePriceCeiling = s.PurchasePriceCeiling,
        FloorPrice = s.FloorPrice,
        DiscountRate = s.DiscountRate,
        CommissionAccount = s.CommissionAccount,
        RelatedSkuId = s.RelatedSkuId,
        CustomerItemCode = s.CustomerItemCode,
        BomType = s.BomType,
        RoutingId = s.RoutingId,
        SubstituteSkuId = s.SubstituteSkuId,
        OutsourceSupplierId = s.OutsourceSupplierId,
        ProcessingFeeAccount = s.ProcessingFeeAccount,
        OutsourceWarehouseId = s.OutsourceWarehouseId,
        IsSupplierProvided = s.IsSupplierProvided,
        PurchaseUomId = s.PurchaseUomId,
        SalesUomId = s.SalesUomId,
        ProductionUomId = s.ProductionUomId,
        PurchaseToBaseFactor = s.PurchaseToBaseFactor,
        SalesToBaseFactor = s.SalesToBaseFactor,
        ProductionToBaseFactor = s.ProductionToBaseFactor
    };

    private Dictionary<string, List<IValidationRule>> BuildSkuValidationRules()
    {
        return new Dictionary<string, List<IValidationRule>>
        {
            ["SkuNumber"] = new List<IValidationRule>
            {
                new RequiredRule("品號不可為空"),
                new StringLengthRule(1, 50, "品號長度必須在 1 到 50 字元之間")
            },
            ["SkuName"] = new List<IValidationRule>
            {
                new RequiredRule("品名不可為空"),
                new StringLengthRule(1, 100, "品名長度必須在 1 到 100 字元之間")
            },
            ["Unit"] = new List<IValidationRule>
            {
                new RequiredRule("單位不可為空"),
                new StringLengthRule(1, 20, "單位長度必須在 1 到 20 字元之間")
            },
            ["SafetyStock"] = new List<IValidationRule>
            {
                new RangeRule(0, null, "安全庫存不能小於 0")
            },
            ["ReorderPoint"] = new List<IValidationRule>
            {
                new RangeRule(0, null, "再訂購點不能小於 0")
            },
            ["MaxStock"] = new List<IValidationRule>
            {
                new RangeRule(0, null, "最高庫存量不符")
            },
            ["LeadTimeDays"] = new List<IValidationRule>
            {
                new RangeRule(0, null, "採購天數不符")
            },
            ["PurchasePrice"] = new List<IValidationRule>
            {
                new RangeRule(0, null, "採購價格不能小於 0")
            },
            ["StandardCost"] = new List<IValidationRule>
            {
                new RangeRule(0, null, "標準成本不能小於 0")
            },
            ["PriceA"] = new List<IValidationRule>
            {
                new RangeRule(0, null, "價格 A 不能小於 0")
            },
            ["PriceB"] = new List<IValidationRule>
            {
                new RangeRule(0, null, "價格 B 不能小於 0")
            },
            ["PriceC"] = new List<IValidationRule>
            {
                new RangeRule(0, null, "價格 C 不能小於 0")
            },
            ["PriceD"] = new List<IValidationRule>
            {
                new RangeRule(0, null, "價格 D 不能小於 0")
            },
            ["PriceE"] = new List<IValidationRule>
            {
                new RangeRule(0, null, "價格 E 不能小於 0")
            },
            ["ShelfLifeDays"] = new List<IValidationRule>
            {
                new RangeRule(0, null, "保存天數不能小於 0")
            }
        };
    }
}
