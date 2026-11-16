using System.Net;
using Xunit;

namespace ErpBackend.IntegrationTests;

public class WorkOrderCostingControllerIntegrationTests
{
    [Fact]
    public async Task VersionedCostingEndpoints_RequireAuthentication()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        var workOrderId = Guid.NewGuid();
        var allocationId = Guid.NewGuid();

        var responses = new[]
        {
            await client.GetAsync(
                $"/api/v1/erp/workorders/{workOrderId}/costing/variance-allocations"),
            await client.PostAsync(
                $"/api/v1/erp/workorders/{workOrderId}/costing/variance-allocations",
                content: null),
            await client.PostAsync(
                $"/api/v1/erp/workorders/{workOrderId}/costing/variance-allocations/{allocationId}/approvals",
                content: null),
            await client.PostAsync(
                $"/api/v1/erp/workorders/{workOrderId}/costing/settle",
                content: null)
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
    }
}
