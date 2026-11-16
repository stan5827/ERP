using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class UomDto
{
    public Guid Id { get; set; }
    public string? UomCode { get; set; }
    public string? UomName { get; set; }
    public string? UomNameEn { get; set; }
    public string? UomType { get; set; }
    public int DecimalPlaces { get; set; }
    public bool IsActive { get; set; }
    public bool AllowDecimal { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUomDto
{
    public string? UomCode { get; set; }
    public string? UomName { get; set; }
    public string? UomNameEn { get; set; }
    public string? UomType { get; set; }
    public int DecimalPlaces { get; set; } = 2;
    public bool IsActive { get; set; } = true;
    public bool AllowDecimal { get; set; } = true;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateUomDto
{
    public string? UomCode { get; set; }
    public string? UomName { get; set; }
    public string? UomNameEn { get; set; }
    public string? UomType { get; set; }
    public int DecimalPlaces { get; set; }
    public bool IsActive { get; set; }
    public bool AllowDecimal { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}

public class UomConversionDto
{
    public Guid Id { get; set; }
    public Guid? SkuId { get; set; }
    public string? SkuCode { get; set; }
    public string? SkuName { get; set; }
    public Guid FromUomId { get; set; }
    public string? FromUomCode { get; set; }
    public string? FromUomName { get; set; }
    public Guid ToUomId { get; set; }
    public string? ToUomCode { get; set; }
    public string? ToUomName { get; set; }
    public decimal Factor { get; set; }
    public string Operator { get; set; } = "*";
    public decimal? InverseFactor { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public bool IsGlobal { get; set; }
}

public class CreateUomConversionDto
{
    public Guid? SkuId { get; set; }
    public Guid FromUomId { get; set; }
    public Guid ToUomId { get; set; }
    public decimal Factor { get; set; } = 1;
    public string Operator { get; set; } = "*";
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class UpdateUomConversionDto
{
    public Guid? SkuId { get; set; }
    public Guid FromUomId { get; set; }
    public Guid ToUomId { get; set; }
    public decimal Factor { get; set; }
    public string Operator { get; set; } = "*";
    public bool IsActive { get; set; }
    public string? Description { get; set; }
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
}

public class UomConversionChainResult
{
    public decimal OriginalQty { get; set; }
    public string OriginalUom { get; set; } = null!;
    public string FinalUom { get; set; } = null!;
    public decimal FinalQty { get; set; }
    public int TotalConversions { get; set; }
    public List<UomConversionStep> Steps { get; set; } = new();
}

public class UomConversionStep
{
    public int StepNumber { get; set; }
    public string FromUom { get; set; } = null!;
    public decimal FromQty { get; set; }
    public string ToUom { get; set; } = null!;
    public decimal ToQty { get; set; }
    public decimal Factor { get; set; }
    public string Operator { get; set; } = "*";
}
