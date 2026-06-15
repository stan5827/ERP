namespace ErpBackend.DTOs;

// ========== WorkCenter ==========

public class WorkCenterDto
{
    public Guid Id { get; set; }
    public string WorkCenterCode { get; set; } = null!;
    public string WorkCenterName { get; set; } = null!;
    public decimal Capacity { get; set; }
    public decimal Efficiency { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CreateWorkCenterRequest
{
    public string WorkCenterCode { get; set; } = null!;
    public string WorkCenterName { get; set; } = null!;
    public decimal Capacity { get; set; }
    public decimal Efficiency { get; set; } = 100;
    public bool IsActive { get; set; } = true;
}

public class UpdateWorkCenterRequest
{
    public string? WorkCenterCode { get; set; }
    public string? WorkCenterName { get; set; }
    public decimal? Capacity { get; set; }
    public decimal? Efficiency { get; set; }
    public bool? IsActive { get; set; }
}

// ========== Operation ==========

public class OperationDto
{
    public Guid Id { get; set; }
    public string OperationCode { get; set; } = null!;
    public string OperationName { get; set; } = null!;
    public decimal SetupTime { get; set; }
    public decimal RunTime { get; set; }
    public decimal CostPerHour { get; set; }
    public bool IsOutsourced { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CreateOperationRequest
{
    public string OperationCode { get; set; } = null!;
    public string OperationName { get; set; } = null!;
    public decimal SetupTime { get; set; }
    public decimal RunTime { get; set; }
    public decimal CostPerHour { get; set; }
    public bool IsOutsourced { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateOperationRequest
{
    public string? OperationCode { get; set; }
    public string? OperationName { get; set; }
    public decimal? SetupTime { get; set; }
    public decimal? RunTime { get; set; }
    public decimal? CostPerHour { get; set; }
    public bool? IsOutsourced { get; set; }
    public bool? IsActive { get; set; }
}

// ========== Routing ==========

public class RoutingOperationDto
{
    public Guid Id { get; set; }
    public Guid RoutingId { get; set; }
    public int OperationSeq { get; set; }
    public Guid OperationId { get; set; }
    public Guid WorkCenterId { get; set; }
    public decimal TransferTime { get; set; }
    public decimal WaitTime { get; set; }
    public string? OperationCode { get; set; }
    public string? OperationName { get; set; }
    public string? WorkCenterCode { get; set; }
    public string? WorkCenterName { get; set; }
}

public class RoutingDto
{
    public Guid Id { get; set; }
    public string RoutingCode { get; set; } = null!;
    public string RoutingName { get; set; } = null!;
    public Guid ProductSkuId { get; set; }
    public string? ProductSkuNumber { get; set; }
    public string? ProductSkuName { get; set; }
    public string Version { get; set; } = "1.0";
    public bool IsActive { get; set; }
    public List<RoutingOperationDto> Operations { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
}

public class CreateRoutingOperationRequest
{
    public int OperationSeq { get; set; }
    public Guid OperationId { get; set; }
    public Guid WorkCenterId { get; set; }
    public decimal TransferTime { get; set; }
    public decimal WaitTime { get; set; }
}

public class CreateRoutingRequest
{
    public string RoutingCode { get; set; } = null!;
    public string RoutingName { get; set; } = null!;
    public Guid ProductSkuId { get; set; }
    public string Version { get; set; } = "1.0";
    public bool IsActive { get; set; } = true;
    public List<CreateRoutingOperationRequest> Operations { get; set; } = new();
}

public class UpdateRoutingRequest
{
    public string? RoutingCode { get; set; }
    public string? RoutingName { get; set; }
    public string? Version { get; set; }
    public bool? IsActive { get; set; }
    public List<CreateRoutingOperationRequest>? Operations { get; set; }
}
