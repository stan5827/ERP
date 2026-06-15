using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErpBackend.Models.Inventory;

public class InventoryReservation
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SkuId { get; set; }
    public decimal Qty { get; set; }
    public string OrderType { get; set; } = null!;
    public Guid OrderId { get; set; }
    public string Status { get; set; } = "reserved";
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReleasedAt { get; set; }

    [ForeignKey(nameof(SkuId))] public Sku? Sku { get; set; }
}
