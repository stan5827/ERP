namespace ErpBackend.DTOs;

public class PlanningSuggestionDto
{
    public string SkuCode { get; set; } = null!;
    public string SkuName { get; set; } = null!;
    public decimal CurrentQty { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal MaxStock { get; set; }
    public decimal Moq { get; set; }
    public decimal SuggestedQty { get; set; }
    public string Reason { get; set; } = null!;
}
