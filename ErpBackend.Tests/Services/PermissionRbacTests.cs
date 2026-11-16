using System.Security.Claims;
using ErpBackend.Data;
using ErpBackend.Filters;
using ErpBackend.Models.System;
using ErpBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ErpBackend.Tests.Services;

public class PermissionRbacTests
{
    private static ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_CreatesPermissionTreeAndIsIdempotent()
    {
        using var db = CreateInMemoryDbContext();

        await PermissionCatalog.SeedAsync(db);
        await PermissionCatalog.SeedAsync(db);

        Assert.Equal(PermissionCatalog.Modules.Count * (PermissionCatalog.Actions.Count + 2) + 1, await db.Permissions.CountAsync());
    }

    [Fact]
    public async Task UpdateRolePermissionsAsync_ExpandsParentAndInvalidatesAssignedUsers()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User { Username = "rbac-user", PasswordHash = "hash" };
        var role = new Role { RoleName = "sales-user" };
        var module = new Permission { PermissionCode = "sales", ModuleName = "銷售管理", ActionType = "module", PermissionType = "module" };
        var page = new Permission { PermissionCode = "sales:page", ModuleName = "銷售與 CRM", ActionType = "view", PermissionType = "page", ParentId = module.Id };
        var view = new Permission { PermissionCode = "sales:view", ModuleName = "檢閱", ActionType = "view", PermissionType = "action", ParentId = page.Id };
        var create = new Permission { PermissionCode = "sales:create", ModuleName = "新增", ActionType = "create", PermissionType = "action", ParentId = page.Id };
        db.AddRange(user, role, module, page, view, create);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync();

        var updated = await new SystemSettingsService(db).UpdateRolePermissionsAsync(role.Id, [module.Id]);

        Assert.True(updated);
        Assert.Equal(1, user.SecurityVersion);
        var assignedPermissionIds = (await db.RolePermissions
            .Where(item => item.RoleId == role.Id)
            .Select(item => item.PermissionId)
            .ToListAsync()).ToHashSet();
        Assert.Equal(new HashSet<Guid> { module.Id, page.Id, view.Id, create.Id }, assignedPermissionIds);
    }

    [Fact]
    public async Task UpdateRolePermissionsAsync_ProtectsAdminRole()
    {
        using var db = CreateInMemoryDbContext();
        var adminRole = new Role { RoleName = "Admin", IsSystem = true };
        db.Roles.Add(adminRole);
        await db.SaveChangesAsync();

        var updated = await new SystemSettingsService(db).UpdateRolePermissionsAsync(adminRole.Id, []);

        Assert.False(updated);
    }

    [Fact]
    public async Task CreateRoleAsync_ReservesAdminRoleName()
    {
        using var db = CreateInMemoryDbContext();
        var service = new SystemSettingsService(db);

        var role = await service.CreateRoleAsync(new ErpBackend.DTOs.CreateRoleRequest
        {
            RoleName = "admin"
        });

        Assert.Null(role);
        Assert.Empty(await db.Roles.ToListAsync());
    }

    [Fact]
    public async Task GetCurrentUserModulesAsync_ReturnsOnlyGrantedModulesAndActions()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User { Username = "rbac-user", PasswordHash = "hash" };
        var role = new Role { RoleName = "sales-user" };
        var view = new Permission { PermissionCode = "sales:view", ModuleName = "檢閱", ActionType = "view" };
        var inventoryView = new Permission { PermissionCode = "inventory:view", ModuleName = "檢閱", ActionType = "view" };
        db.AddRange(user, role, view, inventoryView);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = view.Id });
        await db.SaveChangesAsync();

        var modules = await new SystemSettingsService(db).GetCurrentUserModulesAsync(user.Id);

        var module = Assert.Single(modules);
        Assert.Equal("sales", module.ModuleId);
        Assert.Equal(new[] { "view" }, module.Permissions);
    }

    [Fact]
    public async Task PermissionFilter_AllowsOnlyAssignedActionPermissions()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User { Username = "rbac-user", PasswordHash = "hash" };
        var role = new Role { RoleName = "inventory-viewer" };
        var permission = new Permission
        {
            PermissionCode = "inventory:view",
            ModuleName = "檢閱",
            ActionType = "view"
        };
        db.AddRange(user, role, permission);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await db.SaveChangesAsync();

        var filter = new PermissionAuthorizeFilter(db, NullLogger<PermissionAuthorizeFilter>.Instance);
        var allowedContext = CreateAuthorizationContext(user.Id, "Inventory", "GetAll");
        await filter.OnAuthorizationAsync(allowedContext);
        Assert.Null(allowedContext.Result);

        var deniedContext = CreateAuthorizationContext(user.Id, "Finance", "GetAll");
        await filter.OnAuthorizationAsync(deniedContext);
        Assert.IsType<ForbidResult>(deniedContext.Result);
    }

    [Fact]
    public async Task PermissionFilter_AllowsAdminWithMappedNameIdentifierClaim()
    {
        using var db = CreateInMemoryDbContext();
        var filter = new PermissionAuthorizeFilter(db, NullLogger<PermissionAuthorizeFilter>.Instance);
        var context = CreateAuthorizationContext(Guid.NewGuid(), "Suppliers", "ApproveSupplier", "Admin");

        await filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task PermissionFilter_AllowsAuthenticatedEmployeeSelfServiceWithoutHrModulePermission()
    {
        using var db = CreateInMemoryDbContext();
        var employeeId = Guid.NewGuid();
        var filter = new PermissionAuthorizeFilter(db, NullLogger<PermissionAuthorizeFilter>.Instance);
        var context = CreateAuthorizationContext(Guid.NewGuid(), "EmployeeEss", "GetPayroll");
        var identity = (ClaimsIdentity)context.HttpContext.User.Identity!;
        identity.AddClaim(new Claim("employee_id", employeeId.ToString()));

        await filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task PermissionFilter_RequiresSupplierApprovalPermissionForNonAdmin()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User { Username = "supplier-reviewer", PasswordHash = "hash" };
        var role = new Role { RoleName = "supplier-viewer" };
        var permission = new Permission
        {
            PermissionCode = "suppliers:view",
            ModuleName = "供應商管理",
            ActionType = "view"
        };
        db.AddRange(user, role, permission);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await db.SaveChangesAsync();

        var filter = new PermissionAuthorizeFilter(db, NullLogger<PermissionAuthorizeFilter>.Instance);
        var context = CreateAuthorizationContext(user.Id, "Suppliers", "ApproveSupplier");

        await filter.OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task PermissionFilter_RequiresAiExecutionPermissionForLlmCompletion()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User { Username = "ai-user", PasswordHash = "hash" };
        var role = new Role { RoleName = "ai-viewer" };
        var viewPermission = new Permission
        {
            PermissionCode = "ai:view",
            ModuleName = "AI",
            ActionType = "view"
        };
        var executePermission = new Permission
        {
            PermissionCode = "ai:execute",
            ModuleName = "AI",
            ActionType = "execute"
        };
        db.AddRange(user, role, viewPermission, executePermission);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = viewPermission.Id });
        await db.SaveChangesAsync();

        var filter = new PermissionAuthorizeFilter(db, NullLogger<PermissionAuthorizeFilter>.Instance);
        var context = CreateAuthorizationContext(
            user.Id,
            "Llm",
            "Complete",
            method: HttpMethods.Post,
            permission: new PermissionAuthorizeAttribute("ai", "execute"));

        await filter.OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);

        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = executePermission.Id });
        await db.SaveChangesAsync();
        context = CreateAuthorizationContext(
            user.Id,
            "Llm",
            "Complete",
            method: HttpMethods.Post,
            permission: new PermissionAuthorizeAttribute("ai", "execute"));

        await filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task PermissionFilter_WritesPairedAuditWhenAgentWipPermissionIsDenied()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User { Username = "agent-wip-denied", PasswordHash = "hash" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var context = CreateAuthorizationContext(
            user.Id,
            "AgentWip",
            "GetWorkOrderAsync",
            permission: new PermissionAuthorizeAttribute("manufacturing", "view"));
        var httpContextAccessor = new HttpContextAccessor { HttpContext = context.HttpContext };
        var auditService = new AgentDisclosureAuditService(db, httpContextAccessor);
        var filter = new PermissionAuthorizeFilter(
            db,
            NullLogger<PermissionAuthorizeFilter>.Instance,
            auditService);

        await filter.OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
        Assert.True(context.HttpContext.Items[AgentDisclosureAuditService.AuditRecordedItemKey] is true);
        var disclosure = await db.DisclosureLogs.SingleAsync();
        var audit = await db.AuditLogs.SingleAsync();
        Assert.Equal("permission_blocked", disclosure.Outcome);
        Assert.Equal("module_permission_denied", disclosure.DecisionReason);
        Assert.Equal(disclosure.CreatedAt, audit.CreatedAt);
    }

    [Fact]
    public async Task PermissionFilter_WritesPairedAuditForUnauthenticatedAgentWipRequest()
    {
        using var db = CreateInMemoryDbContext();
        var context = CreateAuthorizationContext(
            Guid.NewGuid(),
            "AgentWip",
            "GetWorkOrderAsync",
            permission: new PermissionAuthorizeAttribute("manufacturing", "view"));
        context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        var httpContextAccessor = new HttpContextAccessor { HttpContext = context.HttpContext };
        var auditService = new AgentDisclosureAuditService(db, httpContextAccessor);
        var filter = new PermissionAuthorizeFilter(
            db,
            NullLogger<PermissionAuthorizeFilter>.Instance,
            auditService);

        await filter.OnAuthorizationAsync(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
        var disclosure = await db.DisclosureLogs.SingleAsync();
        var audit = await db.AuditLogs.SingleAsync();
        Assert.Null(disclosure.ActorUserId);
        Assert.Equal("permission_blocked", disclosure.Outcome);
        Assert.Equal("unauthenticated", disclosure.DecisionReason);
        Assert.Equal(disclosure.CreatedAt, audit.CreatedAt);
    }

    private static AuthorizationFilterContext CreateAuthorizationContext(
        Guid userId,
        string controllerName,
        string actionName,
        string? role = null,
        string method = "GET",
        PermissionAuthorizeAttribute? permission = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        if (role != null)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var httpContext = new DefaultHttpContext
        {
            Request = { Method = method },
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
        var metadata = permission == null
            ? new EndpointMetadataCollection()
            : new EndpointMetadataCollection(permission);
        httpContext.SetEndpoint(new Endpoint(null, metadata, "test"));
        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerName = controllerName,
            ActionName = actionName
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor, new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
        return new AuthorizationFilterContext(actionContext, []);
    }
}
