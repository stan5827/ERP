using Xunit;
using Moq;
using ErpBackend.Services;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace ErpBackend.Tests.Services;

public class LlmConfigTests
{
    [Fact]
    public void LlmConfig_DefaultValues_AreCorrect()
    {
        var config = new LlmConfig();

        Assert.Equal("openai", config.Provider);
        Assert.Equal("https://api.openai.com/v1", config.BaseUrl);
        Assert.Equal("gpt-4", config.DefaultModel);
        Assert.Equal(2000, config.MaxTokens);
        Assert.Equal(0.7, config.Temperature);
        Assert.Equal(60, config.TimeoutSeconds);
    }

    [Fact]
    public void LlmConfig_CanSetCustomValues()
    {
        var config = new LlmConfig
        {
            Provider = "anthropic",
            ApiKey = "test-key",
            BaseUrl = "https://api.anthropic.com",
            DefaultModel = "claude-3-opus",
            MaxTokens = 4000,
            Temperature = 0.5,
            TimeoutSeconds = 120
        };

        Assert.Equal("anthropic", config.Provider);
        Assert.Equal("test-key", config.ApiKey);
        Assert.Equal("claude-3-opus", config.DefaultModel);
        Assert.Equal(4000, config.MaxTokens);
        Assert.Equal(0.5, config.Temperature);
        Assert.Equal(120, config.TimeoutSeconds);
    }
}

public class LlmRequestTests
{
    [Fact]
    public void LlmRequest_DefaultValues_AreCorrect()
    {
        var request = new LlmRequest();

        Assert.Equal(2000, request.MaxTokens);
        Assert.Equal(0.7, request.Temperature);
        Assert.Equal("gpt-4", request.Model);
    }

    [Fact]
    public void LlmRequest_CanSetPrompt()
    {
        var request = new LlmRequest
        {
            Prompt = "Test prompt"
        };

        Assert.Equal("Test prompt", request.Prompt);
    }

    [Fact]
    public void LlmRequest_CanSetSystemPrompt()
    {
        var request = new LlmRequest
        {
            SystemPrompt = "You are a helpful assistant.",
            Prompt = "Hello"
        };

        Assert.Equal("You are a helpful assistant.", request.SystemPrompt);
    }
}

public class LlmResponseTests
{
    [Fact]
    public void LlmResponse_Success_DefaultFalse()
    {
        var response = new LlmResponse();
        Assert.False(response.Success);
    }

    [Fact]
    public void LlmResponse_TokensUsed_DefaultZero()
    {
        var response = new LlmResponse();
        Assert.Equal(0, response.TokensUsed);
    }

    [Fact]
    public void LlmResponse_CanSetContent()
    {
        var response = new LlmResponse
        {
            Success = true,
            Content = "Generated content",
            TokensUsed = 100,
            Model = "gpt-4"
        };

        Assert.True(response.Success);
        Assert.Equal("Generated content", response.Content);
        Assert.Equal(100, response.TokensUsed);
        Assert.Equal("gpt-4", response.Model);
    }

    [Fact]
    public void LlmResponse_CanSetError()
    {
        var response = new LlmResponse
        {
            Success = false,
            Error = "API Error"
        };

        Assert.False(response.Success);
        Assert.Equal("API Error", response.Error);
    }
}