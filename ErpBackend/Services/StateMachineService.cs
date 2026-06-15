namespace ErpBackend.Services;

public interface IStateMachineService
{
    bool CanTransition(string currentStatus, string targetStatus);
    string? ValidateTransition(string documentType, string currentStatus, string targetStatus);
}

public class StateMachineService : IStateMachineService
{
    private static readonly Dictionary<string, Dictionary<string, string[]>> _transitions = new()
    {
        ["sales_order"] = new Dictionary<string, string[]>
        {
            ["draft"] = new[] { "confirmed", "cancelled" },
            ["confirmed"] = new[] { "completed", "cancelled", "partial" },
            ["partial"] = new[] { "completed", "cancelled" },
            ["completed"] = Array.Empty<string>(),
            ["cancelled"] = Array.Empty<string>()
        },
        ["purchase_order"] = new Dictionary<string, string[]>
        {
            ["draft"] = new[] { "confirmed", "cancelled" },
            ["confirmed"] = new[] { "received", "cancelled", "partial" },
            ["partial"] = new[] { "received", "cancelled" },
            ["received"] = new[] { "completed" },
            ["completed"] = Array.Empty<string>(),
            ["cancelled"] = Array.Empty<string>()
        },
        ["work_order"] = new Dictionary<string, string[]>
        {
            ["pending"] = new[] { "released", "cancelled" },
            ["released"] = new[] { "in_progress", "cancelled", "held" },
            ["in_progress"] = new[] { "completed", "cancelled", "held" },
            ["held"] = new[] { "released", "cancelled" },
            ["completed"] = Array.Empty<string>(),
            ["cancelled"] = Array.Empty<string>()
        },
        ["purchase_requisition"] = new Dictionary<string, string[]>
        {
            ["pending"] = new[] { "approved", "rejected", "cancelled" },
            ["approved"] = new[] { "converted", "cancelled" },
            ["rejected"] = Array.Empty<string>(),
            ["converted"] = Array.Empty<string>(),
            ["cancelled"] = Array.Empty<string>()
        },
        ["quote"] = new Dictionary<string, string[]>
        {
            ["draft"] = new[] { "sent", "cancelled" },
            ["sent"] = new[] { "won", "lost", "cancelled" },
            ["won"] = new[] { "expired" },
            ["lost"] = Array.Empty<string>(),
            ["expired"] = Array.Empty<string>(),
            ["cancelled"] = Array.Empty<string>()
        },
        ["mrp_planned_order"] = new Dictionary<string, string[]>
        {
            ["suggested"] = new[] { "confirmed", "cancelled" },
            ["confirmed"] = new[] { "converted" },
            ["converted"] = Array.Empty<string>(),
            ["cancelled"] = Array.Empty<string>()
        },
        ["ecn"] = new Dictionary<string, string[]>
        {
            ["draft"] = new[] { "pending_approval", "cancelled" },
            ["pending_approval"] = new[] { "approved", "rejected" },
            ["approved"] = new[] { "released", "cancelled" },
            ["released"] = Array.Empty<string>(),
            ["rejected"] = Array.Empty<string>(),
            ["cancelled"] = Array.Empty<string>()
        }
    };

    public bool CanTransition(string currentStatus, string targetStatus)
    {
        return ValidateTransition("", currentStatus, targetStatus) == null;
    }

    public string? ValidateTransition(string documentType, string currentStatus, string targetStatus)
    {
        if (string.IsNullOrEmpty(documentType))
            return null;

        if (!_transitions.TryGetValue(documentType, out var statusMap))
            return null;

        if (!statusMap.TryGetValue(currentStatus, out var allowedTargets))
            return $"未知狀態: {currentStatus}";

        if (!allowedTargets.Contains(targetStatus))
            return $"狀態 {currentStatus} 無法轉換為 {targetStatus}";

        return null;
    }
}