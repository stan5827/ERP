using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class DepreciationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }
    public int ProcessedCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class DepreciationRecordDto
{
    public Guid AssetId { get; set; }
    public string AssetNumber { get; set; } = null!;
    public string AssetName { get; set; } = null!;
    public string Period { get; set; } = null!;
    public decimal DepreciationAmount { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal BookValue { get; set; }
}

public class DepreciationSummaryDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalAssets { get; set; }
    public decimal TotalDepreciation { get; set; }
    public decimal TotalAccumulated { get; set; }
    public decimal TotalBookValue { get; set; }
    public List<DepreciationByCategoryDto> ByCategory { get; set; } = new();
}

public class DepreciationByCategoryDto
{
    public string CategoryName { get; set; } = null!;
    public int AssetCount { get; set; }
    public decimal TotalDepreciation { get; set; }
}
