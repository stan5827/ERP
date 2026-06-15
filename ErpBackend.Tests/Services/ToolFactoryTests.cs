using Xunit;
using Moq;
using ErpBackend.Services;
using ErpBackend.Data;
using Microsoft.Extensions.Logging;

namespace ErpBackend.Tests.Services;

public class ToolFactoryTests
{
    private readonly Mock<ErpGeneralDbContext> _mockDb;
    private readonly Mock<ILogger<ToolFactory>> _mockLogger;
    private readonly Mock<IErpIntegrationService> _mockErpIntegration;

    public ToolFactoryTests()
    {
        _mockDb = new Mock<ErpGeneralDbContext>(new Microsoft.EntityFrameworkCore.DbContextOptions<ErpGeneralDbContext>());
        _mockLogger = new Mock<ILogger<ToolFactory>>();
        _mockErpIntegration = new Mock<IErpIntegrationService>();
    }

    [Fact]
    public void GetAvailableTools_ReturnsToolList()
    {
        var factory = new ToolFactory(_mockDb.Object, _mockLogger.Object, _mockErpIntegration.Object);

        var tools = factory.GetAvailableTools();

        Assert.NotNull(tools);
        Assert.True(tools.Count > 0);
    }

    [Fact]
    public void GetAvailableTools_ContainsSearchTools()
    {
        var factory = new ToolFactory(_mockDb.Object, _mockLogger.Object, _mockErpIntegration.Object);

        var tools = factory.GetAvailableTools();

        Assert.Contains(tools, t => t.Category == "search");
    }

    [Fact]
    public void GetAvailableTools_ContainsComputeTools()
    {
        var factory = new ToolFactory(_mockDb.Object, _mockLogger.Object, _mockErpIntegration.Object);

        var tools = factory.GetAvailableTools();

        Assert.Contains(tools, t => t.Category == "compute");
    }

    [Fact]
    public void GetAvailableTools_AllTools_AreEnabled()
    {
        var factory = new ToolFactory(_mockDb.Object, _mockLogger.Object, _mockErpIntegration.Object);

        var tools = factory.GetAvailableTools();

        foreach (var tool in tools)
        {
            Assert.True(tool.IsEnabled, $"Tool {tool.Id} should be enabled");
        }
    }

    [Fact]
    public async Task ExecuteToolAsync_WithInvalidToolId_ReturnsError()
    {
        var factory = new ToolFactory(_mockDb.Object, _mockLogger.Object, _mockErpIntegration.Object);

        var result = await factory.ExecuteToolAsync("invalid_tool", new Dictionary<string, object>());

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ExecuteToolAsync_Calculator_ReturnsResult()
    {
        var factory = new ToolFactory(_mockDb.Object, _mockLogger.Object, _mockErpIntegration.Object);

        var result = await factory.ExecuteToolAsync("calculator", new Dictionary<string, object>
        {
            ["expression"] = "2+2"
        });

        Assert.True(result.Success);
        Assert.True(result.ElapsedMs >= 0);
    }

    [Fact]
    public void ToolFactory_InitializesAllToolDefinitions()
    {
        var factory = new ToolFactory(_mockDb.Object, _mockLogger.Object, _mockErpIntegration.Object);

        var tools = factory.GetAvailableTools();
        
        Assert.Contains(tools, t => t.Id == "erp_inventory_search");
        Assert.Contains(tools, t => t.Id == "erp_po_search");
        Assert.Contains(tools, t => t.Id == "erp_so_search");
        Assert.Contains(tools, t => t.Id == "erp_customer_search");
        Assert.Contains(tools, t => t.Id == "calculator");
    }
}

public class ErpToolDefinitionTests
{
    [Fact]
    public void ErpToolDefinition_Default_IsEnabled()
    {
        var tool = new ErpToolDefinition();
        Assert.True(tool.IsEnabled);
    }

    [Fact]
    public void ErpToolDefinition_Parameters_DefaultEmpty()
    {
        var tool = new ErpToolDefinition();
        Assert.NotNull(tool.Parameters);
        Assert.Empty(tool.Parameters);
    }
}

public class ToolResultTests
{
    [Fact]
    public void ToolResult_Success_DefaultFalse()
    {
        var result = new ToolResult();
        Assert.False(result.Success);
    }

    [Fact]
    public void ToolResult_ElapsedMs_DefaultZero()
    {
        var result = new ToolResult();
        Assert.Equal(0, result.ElapsedMs);
    }
}