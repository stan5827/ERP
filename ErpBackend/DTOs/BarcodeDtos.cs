namespace ErpBackend.DTOs;

public class GenerateBarcodeRequestDto
{
    public string? Prefix { get; set; }
    public string BarcodeType { get; set; } = "sku";
    public Guid? RefId { get; set; }
    public string? RefType { get; set; }
    public string? RefName { get; set; }
    public string? LocationCode { get; set; }
    public decimal? Qty { get; set; }
}

public class ScanBarcodeRequestDto
{
    public string Barcode { get; set; } = "";
    public string? BarcodeType { get; set; }
    public string? Action { get; set; }
    public decimal? Qty { get; set; }
    public string? LocationCode { get; set; }
    public string? Details { get; set; }
}

public sealed class BatchBarcodeRequestDto : GenerateBarcodeRequestDto
{
    public int Quantity { get; set; }
    public decimal? QtyPerBarcode { get; set; }
}
