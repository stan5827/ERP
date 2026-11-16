using System.Data;
using System.Text.Json;

namespace ErpBackend.Services;

public sealed class CalculatorHandler : IToolHandler
{
    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var expression = parameters.GetValueOrDefault("expression")?.ToString();

        if (string.IsNullOrEmpty(expression))
            return Task.FromResult(new ToolResult { Success = false, Error = "expression is required" });

        try
        {
            var dt = new DataTable();
            var result = dt.Compute(expression, null);
            return Task.FromResult(new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { Result = result.ToString() }) });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult { Success = false, Error = ex.Message });
        }
    }
}

public sealed class MrpRunHandler : IToolHandler
{
    private readonly IMrpEngine _mrpEngine;

    public MrpRunHandler(IMrpEngine mrpEngine) => _mrpEngine = mrpEngine;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var horizonStartStr = parameters.GetValueOrDefault("horizon_start")?.ToString();
        var horizonEndStr = parameters.GetValueOrDefault("horizon_end")?.ToString();

        var horizonStart = DateTime.TryParse(horizonStartStr, out var hs) ? hs : DateTime.Today;
        var horizonEnd = DateTime.TryParse(horizonEndStr, out var he) ? he : DateTime.Today.AddMonths(3);

        try
        {
            var result = await _mrpEngine.RunMrpAsync(horizonStart, horizonEnd);
            return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { result.RunNumber, result.Status, result.TotalItemsProcessed }) };
        }
        catch (Exception ex)
        {
            return new ToolResult { Success = false, Error = ex.Message };
        }
    }
}
