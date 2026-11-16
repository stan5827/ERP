using Xunit;
using ErpBackend.DTOs;

namespace ErpBackend.IntegrationTests;

public class ServiceDefinitionTests
{
    [Fact]
    public void ToolFactory_Implements_IToolFactory()
    {
        var type = typeof(ErpBackend.Services.ToolFactory);
        var interfaceType = typeof(ErpBackend.Services.IToolFactory);
        
        Assert.True(interfaceType.IsAssignableFrom(type), "ToolFactory should implement IToolFactory");
    }

    [Fact]
    public void ErpIntegrationService_Implements_IErpIntegrationService()
    {
        var type = typeof(ErpBackend.Services.ErpIntegrationService);
        var interfaceType = typeof(ErpBackend.Services.IErpIntegrationService);
        
        Assert.True(interfaceType.IsAssignableFrom(type), "ErpIntegrationService should implement IErpIntegrationService");
    }

    [Fact]
    public void AIPlanningService_Implements_IAIPlanningService()
    {
        var type = typeof(ErpBackend.Services.AIPlanningService);
        var interfaceType = typeof(ErpBackend.Services.IAIPlanningService);
        
        Assert.True(interfaceType.IsAssignableFrom(type), "AIPlanningService should implement IAIPlanningService");
    }

    [Fact]
    public void LlmConfig_HasRequiredProperties()
    {
        var config = new LlmConfig
        {
            Provider = "openai",
            ApiKey = "test-key",
            BaseUrl = "https://api.openai.com/v1",
            DefaultModel = "gpt-4",
            MaxTokens = 2000,
            Temperature = 0.7
        };
        
        Assert.Equal("openai", config.Provider);
        Assert.Equal("test-key", config.ApiKey);
        Assert.Equal("gpt-4", config.DefaultModel);
    }

    [Fact]
    public void ToolResult_HasRequiredProperties()
    {
        var result = new ErpBackend.Services.ToolResult
        {
            Success = true,
            Data = "test data",
            ElapsedMs = 100
        };
        
        Assert.True(result.Success);
        Assert.Equal("test data", result.Data);
        Assert.Equal(100, result.ElapsedMs);
    }

    [Fact]
    public void PlanningRequest_HasRequiredProperties()
    {
        var request = new PlanningRequest
        {
            TaskDescription = "test task",
            Context = "test context"
        };
        
        Assert.Equal("test task", request.TaskDescription);
        Assert.Equal("test context", request.Context);
    }

    [Fact]
    public void ExecutionRequest_HasDefaultValues()
    {
        var request = new ExecutionRequest();
        
        Assert.False(request.ParallelExecution);
    }

    [Fact]
    public void ReviewRequest_HasDefaultValues()
    {
        var request = new ReviewRequest();
        
        Assert.True(request.AutoApproval);
    }
}