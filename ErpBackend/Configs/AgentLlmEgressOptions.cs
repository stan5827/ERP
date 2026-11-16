namespace ErpBackend.Configs;

public sealed class AgentLlmEgressOptions
{
    public const string SectionName = "AgentLlmEgress";

    public bool Enabled { get; set; }
    public List<AgentLlmDestinationOptions> ApprovedDestinations { get; set; } = [];
}

public sealed class AgentLlmDestinationOptions
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
}
