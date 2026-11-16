namespace ErpBackend.DTOs;

public class PrItemDto
{
    public string SkuId { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Notes { get; set; }
}
