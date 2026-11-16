using Xunit;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class OeeCalculatorTests
{
    [Theory]
    [InlineData(85, "excellent")]
    [InlineData(84.99, "good")]
    [InlineData(70, "good")]
    [InlineData(69.99, "warning")]
    [InlineData(50, "warning")]
    [InlineData(49.99, "critical")]
    [InlineData(0, "critical")]
    public void GetOeeStatus_ReturnsExpectedLabel(decimal oeeValue, string expected)
    {
        Assert.Equal(expected, OeeCalculator.GetOeeStatus(oeeValue));
    }

    [Fact]
    public void Calculate_AllZeroInputs_ReturnsZeros()
    {
        var m = OeeCalculator.Calculate(0, 0, 0, 0, 0);

        Assert.Equal(0, m.Availability);
        Assert.Equal(0, m.Performance);
        Assert.Equal(0, m.Quality);
        Assert.Equal(0, m.OeeValue);
    }

    [Fact]
    public void Calculate_PerfectRun_ReturnsAllHundred()
    {
        var m = OeeCalculator.Calculate(100, 100, 100, 100, 100);

        Assert.Equal(100, m.Availability);
        Assert.Equal(100, m.Performance);
        Assert.Equal(100, m.Quality);
        Assert.Equal(100, m.OeeValue);
    }

    [Fact]
    public void Calculate_PartialValues_ComputesOeeCorrectly()
    {
        // availability = 50/100*100 = 50
        // performance = 80/100*100 = 80
        // quality     = 72/80*100  = 90
        // oee         = 0.5 * 0.8 * 0.9 * 100 = 36
        var m = OeeCalculator.Calculate(
            totalPlannedTime: 100,
            totalActualRunTime: 50,
            totalTheoreticalOutput: 100,
            totalActualOutput: 80,
            totalGoodOutput: 72);

        Assert.Equal(50, m.Availability);
        Assert.Equal(80, m.Performance);
        Assert.Equal(90, m.Quality);
        Assert.Equal(36, m.OeeValue);
    }

    [Fact]
    public void Calculate_ZeroPlannedTime_DoesNotDivideByZero()
    {
        var m = OeeCalculator.Calculate(0, 50, 100, 80, 90);

        Assert.Equal(0, m.Availability);
    }

    [Fact]
    public void Calculate_ZeroTheoreticalOutput_DoesNotDivideByZero()
    {
        var m = OeeCalculator.Calculate(100, 100, 0, 80, 90);

        Assert.Equal(0, m.Performance);
    }

    [Fact]
    public void Calculate_ZeroActualOutput_DoesNotDivideByZero()
    {
        var m = OeeCalculator.Calculate(100, 100, 100, 0, 0);

        Assert.Equal(0, m.Quality);
    }

    [Fact]
    public void GetOeeStatus_PerfectScore_IsExcellent()
    {
        Assert.Equal("excellent", OeeCalculator.GetOeeStatus(100m));
    }
}
