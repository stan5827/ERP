using System.Globalization;

namespace ErpBackend.DTOs;

public sealed record AgentWipStringFactDto(
    string FactId,
    string Value,
    string SourceTable,
    string SourceField,
    DateTime? AsOf,
    string? Unit = null,
    string? NotApplicableReason = null,
    string DataClassification = "internal");

public sealed record AgentWipIntFactDto(
    string FactId,
    int Value,
    string SourceTable,
    string SourceField,
    DateTime? AsOf,
    string? Unit = null,
    string? NotApplicableReason = null,
    string DataClassification = "internal")
{
    public string DisplayValue => Value.ToString(CultureInfo.InvariantCulture);
}

public sealed record AgentWipDecimalFactDto(
    string FactId,
    decimal Value,
    string SourceTable,
    string SourceField,
    DateTime? AsOf,
    string? Unit = null,
    string? NotApplicableReason = null,
    string DataClassification = "internal")
{
    public string DisplayValue => Value.ToString("G29", CultureInfo.InvariantCulture);
}

public sealed record AgentWipNullableDecimalFactDto(
    string FactId,
    decimal? Value,
    string SourceTable,
    string SourceField,
    DateTime? AsOf,
    string? Unit = null,
    string? NotApplicableReason = null,
    string DataClassification = "internal")
{
    public string? DisplayValue => Value?.ToString("G29", CultureInfo.InvariantCulture);
}

public sealed record AgentWipOperationDto(
    AgentWipIntFactDto OperationSequence,
    AgentWipStringFactDto Status);

public sealed record AgentWipFinalOperationDto(
    AgentWipIntFactDto OperationSequence,
    AgentWipStringFactDto Status,
    AgentWipDecimalFactDto PlannedQuantity,
    AgentWipDecimalFactDto GoodQuantity,
    AgentWipDecimalFactDto DefectQuantity,
    AgentWipNullableDecimalFactDto DefectRatePercent,
    AgentWipDecimalFactDto RemainingQuantity);

public sealed record AgentWipWorkOrderDto(
    AgentWipStringFactDto WorkOrderNumber,
    AgentWipStringFactDto ProductNumber,
    AgentWipStringFactDto ProductName,
    AgentWipStringFactDto ProductionUnit,
    AgentWipStringFactDto Status,
    IReadOnlyList<AgentWipOperationDto> AuthorizedOperations,
    AgentWipFinalOperationDto? FinalOperation);

public sealed record AgentDisclosureAuditEntryDto(
    Guid? ActorUserId,
    string TraceId,
    string Outcome,
    string? DecisionReason,
    Guid? ResourceId,
    IReadOnlyCollection<string> DisclosedFields,
    string? IpAddress,
    string? UserAgent,
    string ToolName = "agent.wip.work-order.read.v1",
    string PolicyVersion = "wip-scope-v1",
    string DataClassification = "internal");

public enum AgentWipQueryStatus
{
    Success,
    FeatureDisabled,
    NotInSeedCohort,
    ScopeUnavailable,
    NotFound,
    DataQualityBlocked
}

public sealed record AgentWipQueryResultDto(
    AgentWipQueryStatus Status,
    AgentWipWorkOrderDto? Data = null);

public sealed record AgentWipNumericClaimDto(
    string FactId,
    decimal Value,
    string? Unit);

public sealed record AgentWipAnswerDraftDto(
    string SessionId,
    string TurnId,
    IReadOnlyList<AgentWipNumericClaimDto> NumericClaims);
