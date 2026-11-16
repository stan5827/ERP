using System.Collections.Generic;
using System.Threading.Tasks;
using ErpBackend.Data;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ErpBackend.Tests.Services;

public class ToolHandlersTests
{
    public class CalculatorHandlerTests
    {
        [Theory]
        [InlineData("2+2", "4")]
        [InlineData("3*3", "9")]
        [InlineData("6+4+5", "15")]
        public async Task ExecuteAsync_ValidExpression_ReturnsResult(string expression, string expected)
        {
            var handler = new CalculatorHandler();

            var result = await handler.ExecuteAsync(new Dictionary<string, object> { ["expression"] = expression });

            Assert.True(result.Success);
            Assert.Contains($"\"Result\":\"{expected}\"", result.Data);
        }

        [Fact]
        public async Task ExecuteAsync_MissingExpression_ReturnsError()
        {
            var handler = new CalculatorHandler();

            var result = await handler.ExecuteAsync(new Dictionary<string, object>());

            Assert.False(result.Success);
            Assert.Equal("expression is required", result.Error);
        }

        [Fact]
        public async Task ExecuteAsync_InvalidExpression_ReturnsError()
        {
            var handler = new CalculatorHandler();

            var result = await handler.ExecuteAsync(new Dictionary<string, object> { ["expression"] = "foo +* bar" });

            Assert.False(result.Success);
            Assert.NotNull(result.Error);
        }
    }

    public class IssueMaterialHandlerTests
    {
        [Fact]
        public async Task ExecuteAsync_WithoutWoId_ReturnsError()
        {
            var handler = new IssueMaterialHandler();

            var result = await handler.ExecuteAsync(new Dictionary<string, object>());

            Assert.False(result.Success);
            Assert.Equal("wo_id is required", result.Error);
        }

        [Fact]
        public async Task ExecuteAsync_WithWoId_ReturnsIssuedResult()
        {
            var handler = new IssueMaterialHandler();

            var result = await handler.ExecuteAsync(new Dictionary<string, object> { ["wo_id"] = "WO-123" });

            Assert.True(result.Success);
            Assert.Contains("WO-123", result.Data);
            Assert.Contains("issued", result.Data);
        }
    }

    public class FinanceReportHandlerTests
    {
        [Fact]
        public async Task ExecuteAsync_DefaultReportType()
        {
            var handler = new FinanceReportHandler();

            var result = await handler.ExecuteAsync(new Dictionary<string, object>());

            Assert.True(result.Success);
            Assert.Contains("\"ReportType\":\"summary\"", result.Data);
            Assert.Contains("\"Format\":\"PDF\"", result.Data);
        }

        [Fact]
        public async Task ExecuteAsync_CustomReportType()
        {
            var handler = new FinanceReportHandler();

            var result = await handler.ExecuteAsync(new Dictionary<string, object> { ["report_type"] = "balance_sheet" });

            Assert.True(result.Success);
            Assert.Contains("\"ReportType\":\"balance_sheet\"", result.Data);
        }
    }

    public class SupplierRatingHandlerTests
    {
        [Fact]
        public async Task ExecuteAsync_UsesProvidedSupplierId()
        {
            var handler = new SupplierRatingHandler();

            var result = await handler.ExecuteAsync(new Dictionary<string, object> { ["supplier_id"] = "SUP-9" });

            Assert.True(result.Success);
            Assert.Contains("\"SupplierId\":\"SUP-9\"", result.Data);
            Assert.Contains("\"Rating\":85.5", result.Data);
        }

        [Fact]
        public async Task ExecuteAsync_DefaultsUnknownSupplierId()
        {
            var handler = new SupplierRatingHandler();

            var result = await handler.ExecuteAsync(new Dictionary<string, object>());

            Assert.True(result.Success);
            Assert.Contains("\"SupplierId\":\"test\"", result.Data);
        }
    }

    public class DelegationHandlersTests
    {
        [Fact]
        public async Task InventorySearchHandler_DelegatesToIntegrationService()
        {
            var mockErp = new Mock<IErpIntegrationService>();
            mockErp.Setup(x => x.SearchInventoryAsync("SKU-1", "WH-1"))
                .ReturnsAsync(new ToolResult { Success = true, Data = "inventory-data" })
                .Verifiable();

            var handler = new InventorySearchHandler(mockErp.Object);

            var result = await handler.ExecuteAsync(new Dictionary<string, object>
            {
                ["sku_code"] = "SKU-1",
                ["warehouse_id"] = "WH-1"
            });

            Assert.True(result.Success);
            Assert.Equal("inventory-data", result.Data);
            mockErp.Verify(x => x.SearchInventoryAsync("SKU-1", "WH-1"), Times.Once());
        }

        [Fact]
        public async Task InventorySearchHandler_PassesNullWhenOmitted()
        {
            var mockErp = new Mock<IErpIntegrationService>();
            mockErp.Setup(x => x.SearchInventoryAsync(null, null))
                .ReturnsAsync(new ToolResult { Success = true })
                .Verifiable();

            var handler = new InventorySearchHandler(mockErp.Object);

            await handler.ExecuteAsync(new Dictionary<string, object>());

            mockErp.Verify(x => x.SearchInventoryAsync(null, null), Times.Once());
        }

        [Fact]
        public async Task PurchaseOrderSearchHandler_DelegatesWithFilters()
        {
            var mockErp = new Mock<IErpIntegrationService>();
            mockErp.Setup(x => x.SearchPurchaseOrdersAsync("PO-001", "open"))
                .ReturnsAsync(new ToolResult { Success = true, Data = "po-data" });

            var handler = new PurchaseOrderSearchHandler(mockErp.Object);

            var result = await handler.ExecuteAsync(new Dictionary<string, object>
            {
                ["po_number"] = "PO-001",
                ["status"] = "open"
            });

            Assert.True(result.Success);
            Assert.Equal("po-data", result.Data);
        }
    }

    public class ToolFactoryDispatchTests
    {
        private static ToolFactory CreateFactory(Mock<IErpIntegrationService>? erpMock = null)
        {
            var mockDb = new Mock<ErpGeneralDbContext>(new DbContextOptions<ErpGeneralDbContext>());
            var mockLogger = new Mock<ILogger<ToolFactory>>();
            return new ToolFactory(mockDb.Object, mockLogger.Object, erpMock?.Object ?? new Mock<IErpIntegrationService>().Object, new Mock<IMrpEngine>().Object);
        }

        [Fact]
        public async Task ExecuteToolAsync_UnknownTool_ReturnsNotFound()
        {
            var factory = CreateFactory();

            var result = await factory.ExecuteToolAsync("does_not_exist", new Dictionary<string, object>());

            Assert.False(result.Success);
            Assert.Contains("not found", result.Error);
        }

        [Theory]
        [InlineData("calculator")]
        [InlineData("erp_iqc_report_search")]
        [InlineData("report_finance")]
        public async Task ExecuteToolAsync_RegisteredTools_AreDeniedUntilApproved(string toolId)
        {
            var factory = CreateFactory();

            var result = await factory.ExecuteToolAsync(toolId, new Dictionary<string, object>());

            Assert.False(result.Success);
            Assert.True(result.ElapsedMs >= 0);
            Assert.Contains("not approved", result.Error);
        }

        [Fact]
        public async Task ExecuteToolAsync_ErpPoSearch_IsDeniedWithoutCallingIntegrationService()
        {
            var erpMock = new Mock<IErpIntegrationService>();
            erpMock.Setup(x => x.SearchPurchaseOrdersAsync("PO-1", "open"))
                .ReturnsAsync(new ToolResult { Success = true, Data = "delegated" });

            var factory = CreateFactory(erpMock);

            var result = await factory.ExecuteToolAsync("erp_po_search", new Dictionary<string, object>
            {
                ["po_number"] = "PO-1",
                ["status"] = "open"
            });

            Assert.False(result.Success);
            Assert.Contains("not approved", result.Error);
            erpMock.Verify(x => x.SearchPurchaseOrdersAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
    }
}
