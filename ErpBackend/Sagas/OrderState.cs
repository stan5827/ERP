using MassTransit;

namespace ErpBackend.Sagas;

public class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;

    public string OrderNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? InventoryReservedAt { get; set; }
    public string? FailureReason { get; set; }
}
