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
    private readonly Mock<IMrpEngine> _mockMrpEngine;

    public ToolFactoryTests()
    {
        _mockDb = new Mock<ErpGeneralDbContext>(new Microsoft.EntityFrameworkCore.DbContextOptions<ErpGeneralDbContext>());
        _mockLogger = new Mock<ILogger<ToolFactory>>();
        _mockErpIntegration = new Mock<IErpIntegrationService>();
        _mockMrpEngine = new Mock<IMrpEngine>();
    }

    [Fact]
    public void GetAvailableTools_ReturnsEmptyUntilToolsArePermissionScoped()
    {
        var factory = new ToolFactory(_mockDb.Object, _mockLogger.Object, _mockErpIntegration.Object, _mockMrpEngine.Object);

        var tools = factory.GetAvailableTools();

        Assert.NotNull(tools);
        Assert.Empty(tools);
    }

    [Fact]
    public void GetAvailableTools_DoesNotExposeUnreviewedTools()
    {
        var factory = new ToolFactory(_mockDb.Object, _mockLogger.Object, _mockErpIntegration.Object, _mockMrpEngine.Object);

        var tools = factory.GetAvailableTools();

        Assert.DoesNotContain(tools, tool => tool.Category is "create" or "update" or "action");
    }

    [Fact]
    public async Task ExecuteToolAsync_WithInvalidToolId_ReturnsError()
    {
        var factory = new ToolFactory(_mockDb.Object, _mockLogger.Object, _mockErpIntegration.Object, _mockMrpEngine.Object);

        var result = await factory.ExecuteToolAsync("invalid_tool", new Dictionary<string, object>());

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ExecuteToolAsync_Calculator_IsDeniedUntilApproved()
    {
        var factory = new ToolFactory(_mockDb.Object, _mockLogger.Object, _mockErpIntegration.Object, _mockMrpEngine.Object);

        var result = await factory.ExecuteToolAsync("calculator", new Dictionary<string, object>
        {
            ["expression"] = "2+2"
        });

        Assert.False(result.Success);
        Assert.Contains("not approved", result.Error);
        Assert.True(result.ElapsedMs >= 0);
    }

    [Fact]
    public void ToolFactory_DoesNotExposeUnreviewedSearchTools()
    {
        var factory = new ToolFactory(_mockDb.Object, _mockLogger.Object, _mockErpIntegration.Object, _mockMrpEngine.Object);

        var tools = factory.GetAvailableTools();

        Assert.Empty(tools);
    }

    [Theory]
    [InlineData("erp_wo_create")]
    [InlineData("erp_pr_create")]
    [InlineData("erp_po_create")]
    [InlineData("erp_so_create")]
    [InlineData("erp_goods_receipt_create")]
    [InlineData("erp_delivery_create")]
    [InlineData("erp_wo_update")]
    [InlineData("erp_po_update")]
    [InlineData("erp_inventory_adjust")]
    [InlineData("erp_price_update")]
    [InlineData("erp_wo_complete")]
    [InlineData("erp_material_issue")]
    [InlineData("erp_warehouse_transfer")]
    [InlineData("erp_inventory_count")]
    public async Task ExecuteToolAsync_MutatingTools_AreDenied(string toolId)
    {
        var factory = new ToolFactory(_mockDb.Object, _mockLogger.Object, _mockErpIntegration.Object, _mockMrpEngine.Object);

        var result = await factory.ExecuteToolAsync(toolId, new Dictionary<string, object>());

        Assert.False(result.Success);
        Assert.Contains("not approved", result.Error);
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