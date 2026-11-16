namespace ErpBackend.DTOs;

public sealed record AgentLlmEgressRequestDto(
    string Provider,
    string Model,
    string Endpoint,
    string DataClassification,
    bool HasProviderCredentials = false);

public sealed record AgentLlmEgressDecisionDto(bool Allowed, string ReasonCode);
