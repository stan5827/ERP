namespace ErpBackend.Configs;

public sealed class AgentWipAccessOptions
{
    public bool Enabled { get; set; }
    public Guid[] SeedUserIds { get; set; } = [];
}
