namespace ErpBackend.Services;

public sealed class OeeMetrics
{
    public decimal Availability { get; init; }
    public decimal Performance { get; init; }
    public decimal Quality { get; init; }
    public decimal OeeValue { get; init; }
}

public static class OeeCalculator
{
    public static OeeMetrics Calculate(decimal totalPlannedTime,
                                       decimal totalActualRunTime,
                                       decimal totalTheoreticalOutput,
                                       decimal totalActualOutput,
                                       decimal totalGoodOutput)
    {
        var availability = totalPlannedTime > 0 ? (totalActualRunTime / totalPlannedTime) * 100 : 0;
        var performance = totalTheoreticalOutput > 0 ? (totalActualOutput / totalTheoreticalOutput) * 100 : 0;
        var quality = totalActualOutput > 0 ? (totalGoodOutput / totalActualOutput) * 100 : 0;
        var oeeValue = (availability / 100) * (performance / 100) * (quality / 100) * 100;

        return new OeeMetrics
        {
            Availability = availability,
            Performance = performance,
            Quality = quality,
            OeeValue = oeeValue
        };
    }

    public static string GetOeeStatus(decimal oeeValue)
    {
        if (oeeValue >= 85) return "excellent";
        if (oeeValue >= 70) return "good";
        if (oeeValue >= 50) return "warning";
        return "critical";
    }
}
