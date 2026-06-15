using System.Text.Json.Serialization;

namespace ErpBackend.DTOs.Events;

/// <summary>
/// 工單核心狀態資料主體 (對齊 WorkOrderStatus JSON Schema)
/// </summary>
public class WorkOrderStatusData
{
    public string WoId { get; set; } = null!;
    public string WoNumber { get; set; } = null!;
    public string CurrentStatus { get; set; } = null!; // New, Released, InProcess, etc.
    public string? PreviousStatus { get; set; }
    public DateTime StatusTimestamp { get; set; } = DateTime.UtcNow;
    public string? WorkCenterId { get; set; }
    public string? BatchId { get; set; }
    public decimal QuantityProduced { get; set; }
    public decimal QuantityScrap { get; set; }
    public decimal QuantityRework { get; set; }
}

/// <summary>
/// 基礎事件契約
/// </summary>
public abstract class BaseWorkOrderEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public abstract string EventType { get; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "MES";
}

/// <summary>
/// 1. 工單狀態變更事件
/// </summary>
public class WorkOrderStatusChangedEvent : BaseWorkOrderEvent
{
    public override string EventType => "WorkOrderStatusChanged";
    public WorkOrderStatusData Data { get; set; } = null!;
}

/// <summary>
/// 2. 工單開工事件
/// </summary>
public class WorkOrderStartedEvent : BaseWorkOrderEvent
{
    public override string EventType => "WorkOrderStarted";
    public WorkOrderStartData Data { get; set; } = null!;
}

public class WorkOrderStartData
{
    public string WoId { get; set; } = null!;
    public string WoNumber { get; set; } = null!;
    public string WorkCenterId { get; set; } = null!;
    public string OperatorId { get; set; } = null!;
    public string Shift { get; set; } = "A";
    public DateTime StartTime { get; set; }
}

/// <summary>
/// 3. 工序報工完成事件
/// </summary>
public class WorkOrderPointCompletedEvent : BaseWorkOrderEvent
{
    public override string EventType => "WorkOrderPointCompleted";
    public WorkOrderPointData Data { get; set; } = null!;
}

public class WorkOrderPointData
{
    public string WoId { get; set; } = null!;
    public string WoNumber { get; set; } = null!;
    public int OperationIndex { get; set; }
    public string OperationId { get; set; } = null!;
    public decimal QuantityProduced { get; set; }
    public decimal QuantityDefect { get; set; }
    public decimal QuantityScrap { get; set; }
    public string WorkCenterId { get; set; } = null!;
    public string OperatorId { get; set; } = null!;
    public decimal ActualLaborMinutes { get; set; }
}

/// <summary>
/// 4. 工單完工事件
/// </summary>
public class WorkOrderCompletedEvent : BaseWorkOrderEvent
{
    public override string EventType => "WorkOrderCompleted";
    public WorkOrderCompletionData Data { get; set; } = null!;
}

public class WorkOrderCompletionData
{
    public string WoId { get; set; } = null!;
    public string WoNumber { get; set; } = null!;
    public decimal QuantityProduced { get; set; }
    public decimal QuantityDefect { get; set; }
    public decimal QuantityScrap { get; set; }
    public decimal TotalLaborMinutes { get; set; }
    public decimal TotalMachineMinutes { get; set; }
    public DateTime CompletionDate { get; set; }
}

/// <summary>
/// 5. 工單結案事件
/// </summary>
public class WorkOrderClosedEvent : BaseWorkOrderEvent
{
    public override string EventType => "WorkOrderClosed";
    public WorkOrderClosingData Data { get; set; } = null!;
}

public class WorkOrderClosingData
{
    public string WoId { get; set; } = null!;
    public string WoNumber { get; set; } = null!;
    public decimal CostActualMaterial { get; set; }
    public decimal CostActualLabor { get; set; }
    public decimal CostActualOverhead { get; set; }
    public decimal CostVariance { get; set; }
    public DateTime ClosingDate { get; set; }
}
