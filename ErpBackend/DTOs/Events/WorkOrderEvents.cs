using System;

namespace ErpBackend.DTOs.Events;

public abstract class BaseWorkOrderEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public WorkOrderEventData Data { get; set; } = null!;
}

public class WorkOrderEventData
{
    public Guid WorkOrderId { get; set; }
    public string WoNumber { get; set; } = null!;
    public string? PreviousStatus { get; set; }
    public string? CurrentStatus { get; set; }
    public int? OperationIndex { get; set; }
}

public class WorkOrderPointCompletedEvent : BaseWorkOrderEvent
{
    public WorkOrderPointCompletedEvent()
    {
        EventType = "WorkOrderPointCompleted";
    }
}

public class WorkOrderStatusChangedEvent : BaseWorkOrderEvent
{
    public WorkOrderStatusChangedEvent()
    {
        EventType = "WorkOrderStatusChanged";
    }
}
