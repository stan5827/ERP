using System.Net;
using System.Net.Http.Json;
using ErpBackend.DTOs;
using Xunit;

namespace ErpBackend.IntegrationTests;

public class FallbackAuthorizationIntegrationTests
{
    [Fact]
    public async Task AnonymousBusinessRequestsAreRejectedWhileHealthAndLoginRemainAnonymous()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var businessRequest = new HttpRequestMessage(HttpMethod.Get, "/api/erp/departments");
        businessRequest.Headers.Add("X-Test-Anonymous", "true");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(businessRequest)).StatusCode);

        using var healthRequest = new HttpRequestMessage(HttpMethod.Get, "/health");
        healthRequest.Headers.Add("X-Test-Anonymous", "true");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(healthRequest)).StatusCode);

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/erp/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest
            {
                Username = "unknown-user",
                Password = "invalid"
            })
        };
        loginRequest.Headers.Add("X-Test-Anonymous", "true");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(loginRequest)).StatusCode);
    }
}
