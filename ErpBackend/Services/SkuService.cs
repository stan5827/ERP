using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Validation;
using ErpBackend.Helpers;
using System.Data;

namespace ErpBackend.Services;

public interface ISkuService
{
    Task<ListResult<SkuDto>> GetSkusAsync(string? category, string? itemType, string? searchTerm, int page, int pageSize, bool? isPurchasable = null, bool? isSellable = null);
    Task<SkuDto?> GetSkuAsync(Guid id);
    Task<SkuDto> CreateSkuAsync(SkuDto dto);
    Task<SkuDto> UpdateSkuAsync(Guid id, SkuDto dto);
    Task DeleteSkuAsync(Guid id);
    SkuDto MapToDto(Sku s);
    Dictionary<string, List<IValidationRule>> BuildSkuValidationRules();
}

public class SkuService : ISkuService
{
    private readonly ErpGeneralDbContext _db;

    public SkuService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<ListResult<SkuDto>> GetSkusAsync(string? category, string? itemType, string? searchTerm, int page, int pageSize, bool? isPurchasable = null, bool? isSellable = null)
    {
        var query = _db.Skus
            .Include(s => s.DefaultWarehouse)
            .Include(s => s.DefaultSupplier)
            .Include(s => s.BaseUom)
            .Include(s => s.PurchaseUom)
            .Include(s => s.SalesUom)
            .Include(s => s.ProductionUom)
            .AsQueryable();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(s => s.Category == category);
        
        if (!string.IsNullOrEmpty(itemType))
            query = query.Where(s => s.ItemType == itemType);

        if (isPurchasable.HasValue)
        {
            query = isPurchasable.Value
                ? query.Where(s =>
                    s.IsPurchasable ||
                    s.SourceType == "buy" ||
                    s.SourceType == "make_or_buy")
                : query.Where(s => !s.IsPurchasable);
        }

        if (isSellable.HasValue)
            query = query.Where(s => s.IsSellable == isSellable.Value);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(s =>
                s.SkuNumber.Contains(searchTerm) ||
                s.SkuName.Contains(searchTerm) ||
                (s.Specification != null && s.Specification.Contains(searchTerm)));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.SkuNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ListResult<SkuDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SkuDto?> GetSkuAsync(Guid id)
    {
        var sku = await _db.Skus
            .Include(s => s.DefaultWarehouse)
            .Include(s => s.DefaultSupplier)
            .Include(s => s.BaseUom)
            .Include(s => s.PurchaseUom)
            .Include(s => s.SalesUom)
            .Include(s => s.ProductionUom)
            .Include(s => s.CostUom)
            .FirstOrDefaultAsync(s => s.Id == id);

        return sku == null ? null : MapToDto(sku);
    }

    public async Task<SkuDto> CreateSkuAsync(SkuDto dto)
    {
        if (dto.Qty < 0)
            throw new ArgumentOutOfRangeException(nameof(dto), "初始庫存不可小於零");

        var entity = new Sku
        {
            SkuNumber = dto.SkuNumber,
            SkuName = dto.SkuName,
            Specification = dto.Specification,
            Barcode = dto.Barcode,
            Unit = dto.Unit,
            BaseUomId = dto.BaseUomId,
            Category = dto.Category,
            ItemType = dto.ItemType ?? "purchased",
            SourceType = dto.SourceType ?? "buy",
            Model = dto.Model,
            Brand = dto.Brand,
            Status = dto.Status ?? "active",
            Qty = dto.Qty,
            LotControlled = dto.LotControlled,
            SerialControlled = dto.SerialControlled,
            AbcClassification = dto.AbcClassification,
            SafetyStock = dto.SafetyStock,
            ReorderPoint = dto.ReorderPoint,
            MaxStock = dto.MaxStock,
            LeadTimeDays = dto.LeadTimeDays,
            PurchasePrice = dto.PurchasePrice,
            StandardCost = dto.StandardCost,
            DefaultWarehouseId = dto.DefaultWarehouseId,
            DefaultBinLocationId = dto.DefaultBinLocationId,
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
            SuggestedPrice = dto.SuggestedPrice ?? 0,
            BasePrice = dto.BasePrice ?? 0,
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
            EffectiveFrom = NormalizeUtc(dto.EffectiveFrom),
            EffectiveTo = NormalizeUtc(dto.EffectiveTo),
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
            AvgDailyConsumption = dto.AvgDailyConsumption ?? 0,
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

        await SyncUomFieldsAsync(entity, dto);
        _db.Skus.Add(entity);
        if (entity.Qty > 0)
        {
            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                TrxId = DocumentNumberGenerator.GenerateShort("OPEN"),
                Sku = entity,
                Type = "opening_balance",
                QtyChange = entity.Qty,
                QtyBefore = 0,
                QtyAfter = entity.Qty,
                WarehouseId = entity.DefaultWarehouseId,
                BinLocationId = entity.DefaultBinLocationId,
                RelatedDocType = "SkuOpeningBalance",
                RelatedDocId = entity.Id,
                UnitCost = entity.StandardCost,
                TotalCost = entity.Qty * entity.StandardCost,
                Notes = "SKU 初始庫存"
            });
        }
        await _db.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<SkuDto> UpdateSkuAsync(Guid id, SkuDto dto)
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted)
            : null;
        var entity = await _db.Skus.FindAsync(id)
            ?? throw new KeyNotFoundException("找不到指定SKU");
        entity = await LockSkuAsync(id) ?? entity;
        if (dto.Qty < 0)
            throw new ArgumentOutOfRangeException(nameof(dto), "庫存不可小於零");
        var qtyBefore = entity.Qty;

        entity.SkuName = dto.SkuName;
        entity.Specification = dto.Specification;
        entity.Barcode = dto.Barcode;
        entity.Unit = dto.Unit;
        entity.BaseUomId = dto.BaseUomId;
        entity.Category = dto.Category;
        entity.ItemType = dto.ItemType ?? entity.ItemType;
        entity.SourceType = dto.SourceType ?? entity.SourceType;
        entity.Model = dto.Model;
        entity.Brand = dto.Brand;
        entity.Status = dto.Status ?? entity.Status;
        var qtyChange = dto.Qty - qtyBefore;
        entity.Qty = dto.Qty;
        entity.LotControlled = dto.LotControlled;
        entity.SerialControlled = dto.SerialControlled;
        entity.AbcClassification = dto.AbcClassification;
        entity.SafetyStock = dto.SafetyStock;
        entity.ReorderPoint = dto.ReorderPoint;
        entity.MaxStock = dto.MaxStock;
        entity.LeadTimeDays = dto.LeadTimeDays;
        entity.PurchasePrice = dto.PurchasePrice;
        entity.StandardCost = dto.StandardCost;
        entity.DefaultWarehouseId = dto.DefaultWarehouseId;
        entity.DefaultBinLocationId = dto.DefaultBinLocationId;
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
        entity.SuggestedPrice = dto.SuggestedPrice ?? 0;
        entity.BasePrice = dto.BasePrice ?? 0;
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
        entity.EffectiveFrom = NormalizeUtc(dto.EffectiveFrom);
        entity.EffectiveTo = NormalizeUtc(dto.EffectiveTo);
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
        entity.AvgDailyConsumption = dto.AvgDailyConsumption ?? 0;
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
        await SyncUomFieldsAsync(entity, dto);
        if (qtyChange != 0)
        {
            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                TrxId = DocumentNumberGenerator.GenerateShort("ADJ"),
                SkuId = entity.Id,
                Type = qtyChange > 0 ? "ADJUST_IN" : "ADJUST_OUT",
                QtyChange = qtyChange,
                QtyBefore = qtyBefore,
                QtyAfter = entity.Qty,
                WarehouseId = entity.DefaultWarehouseId,
                BinLocationId = entity.DefaultBinLocationId,
                RelatedDocType = "SkuUpdateAdjustment",
                RelatedDocId = entity.Id,
                UnitCost = entity.StandardCost,
                TotalCost = qtyChange * entity.StandardCost,
                Notes = "SKU 設定庫存調整"
            });
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        if (transaction != null)
            await transaction.CommitAsync();

        return MapToDto(entity);
    }

    private async Task<Sku?> LockSkuAsync(Guid skuId)
    {
        if (_db.Database.IsRelational()
            && _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var sku = await _db.Skus
                .FromSqlInterpolated($"SELECT * FROM \"Skus\" WHERE \"Id\" = {skuId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (sku != null)
                await _db.Entry(sku).ReloadAsync();
            return sku;
        }

        return await _db.Skus.FirstOrDefaultAsync(sku => sku.Id == skuId);
    }

    public async Task DeleteSkuAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("無效的SKU ID");

        var entity = await _db.Skus.FindAsync(id)
            ?? throw new KeyNotFoundException("找不到該項目，請確認 ID 是否正確");

        _db.Skus.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public SkuDto MapToDto(Sku s) => new SkuDto
    {
        Id = s.Id,
        SkuNumber = s.SkuNumber,
        SkuName = s.SkuName,
        Specification = s.Specification,
        Barcode = s.Barcode,
        Unit = s.Unit,
        BaseUomId = s.BaseUomId,
        BaseUomCode = s.BaseUom?.UomCode ?? s.BaseUomCode ?? s.Unit,
        BaseUomName = s.BaseUom?.UomName,
        LotControlled = s.LotControlled,
        SerialControlled = s.SerialControlled,
        AbcClassification = s.AbcClassification,
        Category = s.Category,
        SourceType = s.SourceType,
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
        DefaultBinLocationId = s.DefaultBinLocationId,
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
        PurchaseUomCode = s.PurchaseUom?.UomCode,
        PurchaseUomName = s.PurchaseUom?.UomName,
        SalesUomId = s.SalesUomId,
        SalesUomCode = s.SalesUom?.UomCode,
        SalesUomName = s.SalesUom?.UomName,
        ProductionUomId = s.ProductionUomId,
        ProductionUomCode = s.ProductionUom?.UomCode ?? s.ProductionUomCode,
        ProductionUomName = s.ProductionUom?.UomName,
        PurchaseToBaseFactor = s.PurchaseToBaseFactor,
        SalesToBaseFactor = s.SalesToBaseFactor,
        ProductionToBaseFactor = s.ProductionToBaseFactor
    };

    private async Task SyncUomFieldsAsync(Sku sku, SkuDto dto)
    {
        if (dto.BaseUomId is Guid baseUomId)
        {
            var baseUom = await _db.Uoms.FindAsync(baseUomId)
                ?? throw new ArgumentException("找不到指定的基本／庫存單位", nameof(dto));
            sku.BaseUom = baseUom;
            sku.BaseUomId = baseUom.Id;
            sku.BaseUomCode = baseUom.UomCode;
            sku.Unit = baseUom.UomCode;
        }
        else
        {
            sku.BaseUomCode = dto.BaseUomCode ?? dto.Unit;
            sku.Unit = sku.BaseUomCode;
            sku.BaseUomId = null;
            sku.BaseUom = null;
        }

        sku.PurchaseUom = await FindUomAsync(dto.PurchaseUomId, "採購");
        sku.PurchaseUomId = sku.PurchaseUom?.Id;
        sku.PurchaseUomCode = sku.PurchaseUom?.UomCode ?? dto.PurchaseUomCode;

        sku.SalesUom = await FindUomAsync(dto.SalesUomId, "銷售");
        sku.SalesUomId = sku.SalesUom?.Id;
        sku.SalesUomCode = sku.SalesUom?.UomCode ?? dto.SalesUomCode;

        sku.ProductionUom = await FindUomAsync(dto.ProductionUomId, "生產");
        sku.ProductionUomId = sku.ProductionUom?.Id;
        sku.ProductionUomCode = sku.ProductionUom?.UomCode ?? dto.ProductionUomCode;
    }

    private async Task<Uom?> FindUomAsync(Guid? uomId, string unitType)
    {
        if (!uomId.HasValue)
            return null;

        return await _db.Uoms.FindAsync(uomId.Value)
            ?? throw new ArgumentException($"找不到指定的{unitType}單位");
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (value is null) return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => value.Value
        };
    }

    public Dictionary<string, List<IValidationRule>> BuildSkuValidationRules()
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
                new StringLengthRule(1, 10, "單位長度必須在 1 到 10 字元之間")
            },
            ["SafetyStock"] = new List<IValidationRule> { new RangeRule(0, null, "安全庫存不能小於 0") },
            ["ReorderPoint"] = new List<IValidationRule> { new RangeRule(0, null, "再訂購點不能小於 0") },
            ["MaxStock"] = new List<IValidationRule> { new RangeRule(0, null, "最高庫存量不符") },
            ["LeadTimeDays"] = new List<IValidationRule> { new RangeRule(0, null, "採購天數不符") },
            ["PurchasePrice"] = new List<IValidationRule> { new RangeRule(0, null, "採購價格不能小於 0") },
            ["StandardCost"] = new List<IValidationRule> { new RangeRule(0, null, "標準成本不能小於 0") },
            ["PriceA"] = new List<IValidationRule> { new RangeRule(0, null, "價格 A 不能小於 0") },
            ["PriceB"] = new List<IValidationRule> { new RangeRule(0, null, "價格 B 不能小於 0") },
            ["PriceC"] = new List<IValidationRule> { new RangeRule(0, null, "價格 C 不能小於 0") },
            ["PriceD"] = new List<IValidationRule> { new RangeRule(0, null, "價格 D 不能小於 0") },
            ["PriceE"] = new List<IValidationRule> { new RangeRule(0, null, "價格 E 不能小於 0") },
            ["ShelfLifeDays"] = new List<IValidationRule> { new RangeRule(0, null, "保存天數不能小於 0") }
        };
    }
}

public class ListResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
