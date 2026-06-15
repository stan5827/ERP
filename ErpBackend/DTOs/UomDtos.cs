namespace ErpBackend.DTOs;

public class UomDto
{
    public Guid Id { get; set; }
    public string UomCode { get; set; } = null!;
    public string UomName { get; set; } = null!;
    public string? UomNameEn { get; set; }
    public string UomType { get; set; } = "base";
    public int DecimalPlaces { get; set; } = 2;
    public bool IsActive { get; set; } = true;
    public bool AllowDecimal { get; set; } = true;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
}

public class UomConversionDto
{
    public Guid Id { get; set; }
    public Guid? SkuId { get; set; }
    public string? SkuCode { get; set; }
    public string? SkuName { get; set; }
    public Guid FromUomId { get; set; }
    public string FromUomCode { get; set; } = null!;
    public string FromUomName { get; set; } = null!;
    public Guid ToUomId { get; set; }
    public string ToUomCode { get; set; } = null!;
    public string ToUomName { get; set; } = null!;
    public decimal Factor { get; set; } = 1;
    public string Operator { get; set; } = "*";
    public decimal? InverseFactor { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public bool IsGlobal => SkuId == null;
}

public class CreateUomRequest
{
    public string UomCode { get; set; } = null!;
    public string UomName { get; set; } = null!;
    public string? UomNameEn { get; set; }
    public string UomType { get; set; } = "base";
    public int DecimalPlaces { get; set; } = 2;
    public bool IsActive { get; set; } = true;
    public bool AllowDecimal { get; set; } = true;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;
}

public class UpdateUomRequest
{
    public string? UomCode { get; set; }
    public string? UomName { get; set; }
    public string? UomNameEn { get; set; }
    public string? UomType { get; set; }
    public int? DecimalPlaces { get; set; }
    public bool? IsActive { get; set; }
    public bool? AllowDecimal { get; set; }
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
}

public class CreateUomConversionRequest
{
    public Guid? SkuId { get; set; }
    public Guid FromUomId { get; set; }
    public Guid ToUomId { get; set; }
    public decimal Factor { get; set; } = 1;
    public string Operator { get; set; } = "*";
    public string? Description { get; set; }
}

public class UpdateUomConversionRequest
{
    public decimal? Factor { get; set; }
    public string? Operator { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class UomConversionResult
{
    public decimal OriginalQty { get; set; }
    public string OriginalUom { get; set; } = null!;
    public decimal ConvertedQty { get; set; }
    public string TargetUom { get; set; } = null!;
    public decimal Factor { get; set; }
    public string Operator { get; set; } = "*";
    public int DecimalPlaces { get; set; }
    public string Formula => $"{OriginalQty} {OriginalUom} × {Factor} = {ConvertedQty} {TargetUom}";
}

public class UomConversionChainResult
{
    public List<UomConversionStep> Steps { get; set; } = new();
    public decimal OriginalQty { get; set; }
    public string OriginalUom { get; set; } = null!;
    public decimal FinalQty { get; set; }
    public string FinalUom { get; set; } = null!;
    public int TotalConversions { get; set; }
}

public class UomConversionStep
{
    public int StepNumber { get; set; }
    public decimal FromQty { get; set; }
    public string FromUom { get; set; } = null!;
    public decimal ToQty { get; set; }
    public string ToUom { get; set; } = null!;
    public decimal Factor { get; set; }
    public string Operator { get; set; } = "*";
}
