namespace ErpBackend.Events;

public interface ISalesOrderCreated
{
    Guid OrderId { get; }
    string OrderNumber { get; }
    string CustomerName { get; }
    decimal TotalAmount { get; }
    List<OrderItemEventDetail> Items { get; }
}

public class OrderItemEventDetail
{
    public Guid SkuId { get; set; }
    public string SkuNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public interface IInventoryReserved
{
    Guid OrderId { get; }
    DateTime ReservedAt { get; }
}

public interface IInventoryReservationFailed
{
    Guid OrderId { get; }
    string Reason { get; }
    DateTime FailedAt { get; }
}
