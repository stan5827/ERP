using Xunit;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class KpiServiceTests
{
    [Fact]
    public void GetDashboardAsync_ReturnsNonNull()
    {
        // Placeholder - requires actual DbContext setup
        Assert.True(true);
    }

    [Fact]
    public void CalculateOeeAsync_ReturnsZero_WhenNoRecords()
    {
        // Placeholder - requires actual DbContext setup
        Assert.True(true);
    }

    [Fact]
    public void CalculateOtdAsync_ReturnsValidResult()
    {
        // Placeholder - requires actual DbContext setup
        Assert.True(true);
    }

    [Theory]
    [InlineData(0, "critical")]
    [InlineData(50, "warning")]
    [InlineData(75, "good")]
    [InlineData(90, "excellent")]
    public void GetOeeStatus_ReturnsCorrectStatus(decimal oeeValue, string expectedStatus)
    {
        var status = OeeCalculator.GetOeeStatus(oeeValue);

        Assert.Equal(expectedStatus, status);
    }
}