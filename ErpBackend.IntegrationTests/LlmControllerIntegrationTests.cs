using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Xunit;
using ErpBackend.DTOs;

namespace ErpBackend.IntegrationTests;

public class LlmControllerIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();

    [Fact]
    public async Task GetModel_ReturnsModelName()
    {
        var response = await _client.GetAsync("/api/erp/llm/model");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("data").GetString()));
    }

    [Fact]
    public async Task Complete_NoApiKey_ReturnsBadRequest()
    {
        var req = new LlmRequest
        {
            Prompt = "What is ERP?",
            SystemPrompt = "You are a helpful assistant.",
            MaxTokens = 100,
            Temperature = 0.7,
            Model = "gpt-4"
        };

        var response = await _client.PostAsJsonAsync("/api/erp/llm/complete", req);
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.False(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetPing_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/erp/llm/ping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("pong", await response.Content.ReadAsStringAsync());
    }
}
