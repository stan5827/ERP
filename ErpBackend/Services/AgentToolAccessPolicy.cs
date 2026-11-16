namespace ErpBackend.Services;

public static class AgentToolAccessPolicy
{
    // No tool is approved until it enforces the caller's permissions and data scope.
    private static readonly IReadOnlyDictionary<string, string> AllowedTools =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> ReadOnlyCategories =
        new(StringComparer.OrdinalIgnoreCase) { "search", "check" };

    public static bool IsAllowed(string toolId, string category)
        => ReadOnlyCategories.Contains(category)
            && AllowedTools.TryGetValue(toolId, out var allowedCategory)
            && string.Equals(category, allowedCategory, StringComparison.OrdinalIgnoreCase);
}
