using System.Security.Claims;
using ErpBackend.Data;
using ErpBackend.Filters;
using ErpBackend.Models.System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ErpBackend.Tests.Filters;

public class AuditLogFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_AwaitsAuditLogWriteBeforeReturning()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        var logger = new Mock<ILogger<AuditLogFilter>>();
        var userId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                "test"))
        };
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/api/erp/planning/plan";

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var executingContext = new ActionExecutingContext(
            actionContext,
            Array.Empty<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
        ActionExecutionDelegate next = () => Task.FromResult(
            new ActionExecutedContext(actionContext, Array.Empty<IFilterMetadata>(), new object()));

        var filter = new AuditLogFilter(db, logger.Object);
        await filter.OnActionExecutionAsync(executingContext, next);

        var auditLog = Assert.Single(await db.AuditLogs.ToListAsync());
        Assert.Equal(userId, auditLog.UserId);
        Assert.Equal("create /api/erp/planning/plan", auditLog.Action);
    }

    [Fact]
    public async Task OnActionExecutionAsync_TruncatesLongActionToAuditColumnLimit()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        var logger = new Mock<ILogger<AuditLogFilter>>();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                "test"))
        };
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/api/erp/manufacturing/production-receipts/123456789012345";

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var executingContext = new ActionExecutingContext(
            actionContext,
            Array.Empty<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
        ActionExecutionDelegate next = () => Task.FromResult(
            new ActionExecutedContext(actionContext, Array.Empty<IFilterMetadata>(), new object()));

        await new AuditLogFilter(db, logger.Object).OnActionExecutionAsync(executingContext, next);

        var auditLog = Assert.Single(await db.AuditLogs.ToListAsync());
        Assert.Equal(50, auditLog.Action.Length);
        Assert.StartsWith("create /api/erp/manufacturing/production-", auditLog.Action);
    }
}
