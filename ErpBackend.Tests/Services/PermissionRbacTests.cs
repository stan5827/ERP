using System.Security.Claims;
using ErpBackend.Controllers;
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

        var expectedPermissionCount = PermissionCatalog.Modules.Sum(module =>
            PermissionCatalog.Actions.Count + 2 + (module.IncludeReverse ? 1 : 0))
            + PermissionCatalog.Resources.Sum(resource =>
                PermissionCatalog.Actions.Count + 1
                    + (PermissionCatalog.Modules.Single(module => module.Id == resource.ModuleId).IncludeReverse ? 1 : 0));
        Assert.Equal(expectedPermissionCount, await db.Permissions.CountAsync());
        Assert.All(
            await db.Permissions.ToListAsync(),
            permission => Assert.False(string.IsNullOrWhiteSpace(permission.Description)));

        var inventoryPage = await db.Permissions.SingleAsync(item => item.PermissionCode == "inventory:page");
        var skuPage = await db.Permissions.SingleAsync(item => item.PermissionCode == "inventory:skus:page");
        Assert.Equal(inventoryPage.Id, skuPage.ParentId);
        Assert.Equal("商品／品碼（SKU）", skuPage.ModuleName);
        Assert.True(await db.Permissions.AnyAsync(item =>
            item.PermissionCode == "inventory:skus:create" && item.ParentId == skuPage.Id));
    }

    [Fact]
    public async Task PermissionFilter_UsesResourcePermissionsForIndependentForms()
    {
        using var db = CreateInMemoryDbContext();
        await PermissionCatalog.SeedAsync(db);

        var user = new User { Username = "sku-resource-user", PasswordHash = "hash" };
        var role = new Role { RoleName = "SKU Creator" };
        var skuCreate = await db.Permissions.SingleAsync(item => item.PermissionCode == "inventory:skus:create");
        db.AddRange(user, role);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = skuCreate.Id });
        await db.SaveChangesAsync();

        var filter = new PermissionAuthorizeFilter(db, NullLogger<PermissionAuthorizeFilter>.Instance);
        var skuCreateContext = CreateAuthorizationContext(user.Id, "Skus", "CreateSku", method: "POST");
        var skuViewContext = CreateAuthorizationContext(user.Id, "Skus", "GetSkus");
        var warehouseCreateContext = CreateAuthorizationContext(user.Id, "Warehouses", "CreateWarehouse", method: "POST");
        await filter.OnAuthorizationAsync(skuCreateContext);
        await filter.OnAuthorizationAsync(skuViewContext);
        await filter.OnAuthorizationAsync(warehouseCreateContext);

        Assert.Null(skuCreateContext.Result);
        Assert.IsType<ForbidResult>(skuViewContext.Result);
        Assert.IsType<ForbidResult>(warehouseCreateContext.Result);

        var moduleAccess = await new SystemSettingsService(db).GetCurrentUserModulesAsync(user.Id);
        Assert.Contains(moduleAccess, item => item.ModuleId == "inventory:skus" && item.Permissions.Contains("create"));
        Assert.Contains(moduleAccess, item => item.ModuleId == "inventory" && item.Permissions.Contains("view"));
    }

    [Fact]
    public async Task PermissionFilter_SeparatesFormsWithinSharedControllers()
    {
        using var db = CreateInMemoryDbContext();
        await PermissionCatalog.SeedAsync(db);

        var user = new User { Username = "split-resource-user", PasswordHash = "hash" };
        var role = new Role { RoleName = "Split Resource Operator" };
        var accountingCreate = await db.Permissions.SingleAsync(item =>
            item.PermissionCode == "finance:accounting:create");
        var stocktakeExecute = await db.Permissions.SingleAsync(item =>
            item.PermissionCode == "inventory:stocktake:execute");
        db.AddRange(user, role);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        db.RolePermissions.AddRange(
            new RolePermission { RoleId = role.Id, PermissionId = accountingCreate.Id },
            new RolePermission { RoleId = role.Id, PermissionId = stocktakeExecute.Id });
        await db.SaveChangesAsync();

        var filter = new PermissionAuthorizeFilter(db, NullLogger<PermissionAuthorizeFilter>.Instance);
        var createVoucher = CreateAuthorizationContext(user.Id, "Finance", "CreateVoucher", method: "POST");
        var createGLAccount = CreateAuthorizationContext(user.Id, "Finance", "CreateGLAccount", method: "POST");
        var createLoan = CreateAuthorizationContext(user.Id, "Finance", "CreateLoan", method: "POST");
        var postStocktake = CreateAuthorizationContext(user.Id, "Inventory", "PostStocktake", method: "POST");
        var adjustInventory = CreateAuthorizationContext(user.Id, "Inventory", "AdjustBalance", method: "POST");

        await filter.OnAuthorizationAsync(createVoucher);
        await filter.OnAuthorizationAsync(createGLAccount);
        await filter.OnAuthorizationAsync(createLoan);
        await filter.OnAuthorizationAsync(postStocktake);
        await filter.OnAuthorizationAsync(adjustInventory);

        Assert.Null(createVoucher.Result);
        Assert.Null(createGLAccount.Result);
        Assert.IsType<ForbidResult>(createLoan.Result);
        Assert.Null(postStocktake.Result);
        Assert.IsType<ForbidResult>(adjustInventory.Result);

        var moduleAccess = await new SystemSettingsService(db).GetCurrentUserModulesAsync(user.Id);
        Assert.Contains(moduleAccess, item =>
            item.ModuleId == "finance:accounting" && item.Permissions.Contains("create"));
        Assert.Contains(moduleAccess, item =>
            item.ModuleId == "inventory:stocktake" && item.Permissions.Contains("execute"));
    }

    [Fact]
    public async Task WorkOrderTransferPermissions_ExposeOnlyTheScopedQueueAndTransferAction()
    {
        using var db = CreateInMemoryDbContext();
        await PermissionCatalog.SeedAsync(db);

        var user = new User { Username = "work-order-transfer-user", PasswordHash = "hash" };
        var role = new Role { RoleName = "Production Supervisor" };
        var pagePermission = await db.Permissions.SingleAsync(item =>
            item.PermissionCode == "sales:workordertransfer:page");
        var executePermission = await db.Permissions.SingleAsync(item =>
            item.PermissionCode == "sales:workordertransfer:execute");
        db.AddRange(user, role);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        db.RolePermissions.AddRange(
            new RolePermission { RoleId = role.Id, PermissionId = pagePermission.Id },
            new RolePermission { RoleId = role.Id, PermissionId = executePermission.Id });
        await db.SaveChangesAsync();

        var filter = new PermissionAuthorizeFilter(db, NullLogger<PermissionAuthorizeFilter>.Instance);
        var candidates = CreateAuthorizationContext(
            user.Id,
            "WorkOrders",
            "GetSalesOrderWorkOrderTransferCandidates",
            permission: new PermissionAuthorizeAttribute("sales", "view"));
        var transfer = CreateAuthorizationContext(
            user.Id,
            "WorkOrders",
            "CreateWorkOrdersFromSalesOrder",
            method: "POST",
            permission: new PermissionAuthorizeAttribute("sales", "execute"));
        var salesOrders = CreateAuthorizationContext(user.Id, "SalesOrders", "GetSalesOrders");
        var createWorkOrder = CreateAuthorizationContext(
            user.Id,
            "WorkOrders",
            "CreateWorkOrder",
            method: "POST");

        await filter.OnAuthorizationAsync(candidates);
        await filter.OnAuthorizationAsync(transfer);
        await filter.OnAuthorizationAsync(salesOrders);
        await filter.OnAuthorizationAsync(createWorkOrder);

        Assert.Null(candidates.Result);
        Assert.Null(transfer.Result);
        Assert.IsType<ForbidResult>(salesOrders.Result);
        Assert.IsType<ForbidResult>(createWorkOrder.Result);

        var moduleAccess = await new SystemSettingsService(db).GetCurrentUserModulesAsync(user.Id);
        Assert.Equal(
            new[] { "view" },
            Assert.Single(moduleAccess, item => item.ModuleId == "sales").Permissions);
        Assert.Equal(
            new[] { "view", "execute" },
            Assert.Single(moduleAccess, item => item.ModuleId == "sales:workordertransfer").Permissions);
    }

    [Fact]
    public async Task StartWorkOrder_RequiresManufacturingExecutePermission()
    {
        using var db = CreateInMemoryDbContext();
        await PermissionCatalog.SeedAsync(db);
        await RolePermissionCatalog.SeedAsync(db);

        var operatorRole = await db.Roles.SingleAsync(item => item.RoleName == "Operator");
        var plannerRole = await db.Roles.SingleAsync(item => item.RoleName == "Production Planner");
        var operatorUser = new User { Username = "work-order-operator", PasswordHash = "hash" };
        var plannerUser = new User { Username = "work-order-planner", PasswordHash = "hash" };
        db.AddRange(operatorUser, plannerUser);
        db.UserRoles.AddRange(
            new UserRole { UserId = operatorUser.Id, RoleId = operatorRole.Id },
            new UserRole { UserId = plannerUser.Id, RoleId = plannerRole.Id });
        await db.SaveChangesAsync();

        var startMethod = typeof(WorkOrdersController).GetMethod(nameof(WorkOrdersController.StartWorkOrder));
        Assert.NotNull(startMethod);
        var permission = Attribute.GetCustomAttribute(startMethod, typeof(PermissionAuthorizeAttribute))
            as PermissionAuthorizeAttribute;
        Assert.NotNull(permission);
        Assert.Equal("manufacturing", permission.Module);
        Assert.Equal("execute", permission.Action);

        var filter = new PermissionAuthorizeFilter(db, NullLogger<PermissionAuthorizeFilter>.Instance);
        var operatorRequest = CreateAuthorizationContext(
            operatorUser.Id,
            "WorkOrders",
            nameof(WorkOrdersController.StartWorkOrder),
            method: "PATCH",
            permission: permission);
        var plannerRequest = CreateAuthorizationContext(
            plannerUser.Id,
            "WorkOrders",
            nameof(WorkOrdersController.StartWorkOrder),
            method: "PATCH",
            permission: permission);

        await filter.OnAuthorizationAsync(operatorRequest);
        await filter.OnAuthorizationAsync(plannerRequest);

        Assert.Null(operatorRequest.Result);
        Assert.IsType<ForbidResult>(plannerRequest.Result);
    }

    [Fact]
    public async Task RolePermissionCatalog_SeedsLeastPrivilegeRolesOnceAndPreservesCustomAssignments()
    {
        using var db = CreateInMemoryDbContext();
        await PermissionCatalog.SeedAsync(db);

        var hrRole = new Role { RoleName = "HR Manager", Description = "舊描述" };
        var adminRole = new Role { RoleName = "Admin", Description = "Old admin description", IsSystem = true };
        var customRole = new Role { RoleName = "UAT Custom Role", Description = "自訂角色" };
        var uatOperatorRole = new Role { RoleName = "UAT Warehouse Operator", Description = "Isolated synthetic UAT role" };
        var user = new User { Username = "rbac-seed-user", PasswordHash = "hash" };
        var customPermission = await db.Permissions.SingleAsync(item => item.PermissionCode == "inventory:view");
        db.AddRange(hrRole, adminRole, customRole, uatOperatorRole, user);
        db.UserRoles.AddRange(
            new UserRole { UserId = user.Id, RoleId = hrRole.Id },
            new UserRole { UserId = user.Id, RoleId = customRole.Id });
        db.RolePermissions.Add(new RolePermission
        {
            RoleId = customRole.Id,
            PermissionId = customPermission.Id
        });
        await db.SaveChangesAsync();

        await RolePermissionCatalog.SeedAsync(db);

        var seededHrRole = await db.Roles.SingleAsync(item => item.RoleName == "HR Manager");
        var hrPermissionCodes = await db.RolePermissions
            .Where(item => item.RoleId == seededHrRole.Id)
            .Select(item => item.Permission.PermissionCode)
            .ToListAsync();
        Assert.Contains("hr:view", hrPermissionCodes);
        Assert.Contains("hr:approve", hrPermissionCodes);
        Assert.DoesNotContain("hr:delete", hrPermissionCodes);
        Assert.DoesNotContain("hr:execute", hrPermissionCodes);
        Assert.Contains("人資主管", seededHrRole.Description);
        Assert.Contains("隔離 UAT", uatOperatorRole.Description);
        Assert.Equal(1, user.SecurityVersion);
        Assert.Equal(2, await db.UserRoles.CountAsync(item => item.UserId == user.Id));
        Assert.Contains(
            await db.RolePermissions.Where(item => item.RoleId == customRole.Id).Select(item => item.PermissionId).ToListAsync(),
            item => item == customPermission.Id);

        var adminPermissions = await db.RolePermissions
            .Where(item => item.RoleId == adminRole.Id)
            .CountAsync();
        Assert.Equal(0, adminPermissions);
        Assert.Contains("系統管理員", adminRole.Description);

        var supervisorRole = await db.Roles.SingleAsync(item => item.RoleName == "Production Supervisor");
        var supervisorPermissions = await db.RolePermissions
            .Where(item => item.RoleId == supervisorRole.Id)
            .Select(item => item.Permission.PermissionCode)
            .ToListAsync();
        Assert.Contains("sales:workordertransfer:page", supervisorPermissions);
        Assert.Contains("sales:workordertransfer:execute", supervisorPermissions);
        Assert.DoesNotContain("sales:view", supervisorPermissions);

        var hrView = await db.Permissions.SingleAsync(item => item.PermissionCode == "hr:view");
        db.RolePermissions.Remove(await db.RolePermissions.SingleAsync(item =>
            item.RoleId == seededHrRole.Id && item.PermissionId == hrView.Id));
        await db.SaveChangesAsync();

        await RolePermissionCatalog.SeedAsync(db);

        Assert.False(await db.RolePermissions.AnyAsync(item =>
            item.RoleId == seededHrRole.Id && item.PermissionId == hrView.Id));
        Assert.Equal(1, user.SecurityVersion);
    }

    [Fact]
    public async Task RolePermissionCatalog_AddsTransferPermissionsToExistingProductionSupervisor()
    {
        using var db = CreateInMemoryDbContext();
        await PermissionCatalog.SeedAsync(db);

        var role = new Role { RoleName = "Production Supervisor", IsSystem = true };
        var user = new User { Username = "existing-production-supervisor", PasswordHash = "hash" };
        db.AddRange(role, user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        db.SystemConfigs.Add(new SystemConfig
        {
            ConfigKey = "rbac.default-role-catalog.v1",
            ConfigValue = "previous",
            DataType = "string"
        });
        await db.SaveChangesAsync();

        await RolePermissionCatalog.SeedAsync(db);

        var permissionCodes = await db.RolePermissions
            .Where(item => item.RoleId == role.Id)
            .Select(item => item.Permission.PermissionCode)
            .ToListAsync();
        Assert.Contains("sales:workordertransfer:page", permissionCodes);
        Assert.Contains("sales:workordertransfer:execute", permissionCodes);
        Assert.DoesNotContain("sales:view", permissionCodes);
        Assert.Equal(1, user.SecurityVersion);
    }

    [Fact]
    public async Task RolePermissionCatalog_SeedsScopedEngineeringAndQualityRoles()
    {
        using var db = CreateInMemoryDbContext();
        await PermissionCatalog.SeedAsync(db);
        db.SystemConfigs.Add(new SystemConfig
        {
            ConfigKey = "rbac.default-role-catalog.v1",
            ConfigValue = "previous",
            DataType = "string"
        });
        await db.SaveChangesAsync();

        await RolePermissionCatalog.SeedAsync(db);

        var roles = await db.Roles
            .Where(role => role.RoleName == "Process Engineer"
                || role.RoleName == "Product Engineer"
                || role.RoleName == "Quality Engineer")
            .ToListAsync();
        var roleCodes = new Dictionary<string, List<string>>();
        foreach (var role in roles)
        {
            roleCodes[role.RoleName] = await db.RolePermissions
                .Where(item => item.RoleId == role.Id)
                .Select(item => item.Permission.PermissionCode)
                .ToListAsync();
        }

        Assert.Contains("processengineering:create", roleCodes["Process Engineer"]);
        Assert.Contains("processengineering:edit", roleCodes["Process Engineer"]);
        Assert.DoesNotContain("productengineering:create", roleCodes["Process Engineer"]);
        Assert.Contains("productengineering:create", roleCodes["Product Engineer"]);
        Assert.Contains("productengineering:edit", roleCodes["Product Engineer"]);
        Assert.DoesNotContain("processengineering:create", roleCodes["Product Engineer"]);
        Assert.Contains("quality:create", roleCodes["Quality Engineer"]);
        Assert.Contains("quality:edit", roleCodes["Quality Engineer"]);
        Assert.DoesNotContain("quality:approve", roleCodes["Quality Engineer"]);
    }

    [Fact]
    public async Task RolePermissionCatalog_AddsSkuReadToExistingSalesRepresentativeWithoutInventoryAccess()
    {
        using var db = CreateInMemoryDbContext();
        await PermissionCatalog.SeedAsync(db);

        var salesRole = new Role { RoleName = "Sales Representative", IsSystem = true };
        var user = new User { Username = "sales-sku-user", PasswordHash = "hash" };
        var salesView = await db.Permissions.SingleAsync(item => item.PermissionCode == "sales:view");
        db.AddRange(salesRole, user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = salesRole.Id });
        db.RolePermissions.Add(new RolePermission { RoleId = salesRole.Id, PermissionId = salesView.Id });
        db.SystemConfigs.Add(new SystemConfig
        {
            ConfigKey = "rbac.default-role-catalog.v1",
            ConfigValue = "previous",
            DataType = "string"
        });
        await db.SaveChangesAsync();

        await RolePermissionCatalog.SeedAsync(db);

        var permissionCodes = await db.RolePermissions
            .Where(item => item.RoleId == salesRole.Id)
            .Select(item => item.Permission.PermissionCode)
            .ToListAsync();
        Assert.Contains("inventory:skus:view", permissionCodes);
        Assert.DoesNotContain("inventory:view", permissionCodes);
        Assert.DoesNotContain("inventory:skus:create", permissionCodes);
        Assert.Equal(1, user.SecurityVersion);

        var filter = new PermissionAuthorizeFilter(db, NullLogger<PermissionAuthorizeFilter>.Instance);
        var skuListContext = CreateAuthorizationContext(user.Id, "Skus", "GetSkus");
        var skuCreateContext = CreateAuthorizationContext(user.Id, "Skus", "CreateSku", method: "POST");
        var inventoryContext = CreateAuthorizationContext(user.Id, "Inventory", "GetSummary");
        await filter.OnAuthorizationAsync(skuListContext);
        await filter.OnAuthorizationAsync(skuCreateContext);
        await filter.OnAuthorizationAsync(inventoryContext);

        Assert.Null(skuListContext.Result);
        Assert.IsType<ForbidResult>(skuCreateContext.Result);
        Assert.IsType<ForbidResult>(inventoryContext.Result);
    }

    [Fact]
    public async Task RolePermissionCatalog_AddsBomMaintenanceAndSkuLookupToExistingEngineeringManager()
    {
        using var db = CreateInMemoryDbContext();
        await PermissionCatalog.SeedAsync(db);

        var engineeringManagerRole = new Role { RoleName = "Engineering Manager", IsSystem = true };
        var user = new User { Username = "engineering-manager-bom-user", PasswordHash = "hash" };
        var customPermission = await db.Permissions.SingleAsync(item => item.PermissionCode == "finance:view");
        db.AddRange(engineeringManagerRole, user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = engineeringManagerRole.Id });
        db.RolePermissions.Add(new RolePermission
        {
            RoleId = engineeringManagerRole.Id,
            PermissionId = customPermission.Id
        });
        db.SystemConfigs.Add(new SystemConfig
        {
            ConfigKey = "rbac.default-role-catalog.v1",
            ConfigValue = "previous",
            DataType = "string"
        });
        await db.SaveChangesAsync();

        await RolePermissionCatalog.SeedAsync(db);

        var permissionCodes = await db.RolePermissions
            .Where(item => item.RoleId == engineeringManagerRole.Id)
            .Select(item => item.Permission.PermissionCode)
            .ToListAsync();
        Assert.Contains("inventory:view", permissionCodes);
        Assert.Contains("productengineering:view", permissionCodes);
        Assert.Contains("productengineering:create", permissionCodes);
        Assert.Contains("productengineering:edit", permissionCodes);
        Assert.Contains("finance:view", permissionCodes);
        Assert.DoesNotContain("inventory:create", permissionCodes);
        Assert.DoesNotContain("inventory:edit", permissionCodes);
        Assert.DoesNotContain("productengineering:delete", permissionCodes);
        Assert.Equal(1, user.SecurityVersion);

        var filter = new PermissionAuthorizeFilter(db, NullLogger<PermissionAuthorizeFilter>.Instance);
        var skuLookup = CreateAuthorizationContext(user.Id, "Skus", "GetSkus");
        var bomCreate = CreateAuthorizationContext(user.Id, "Boms", "CreateBom", method: "POST");
        var bomUpdate = CreateAuthorizationContext(user.Id, "Boms", "UpdateBom", method: "PUT");
        var skuEdit = CreateAuthorizationContext(user.Id, "Skus", "UpdateSku", method: "PUT");
        var bomDelete = CreateAuthorizationContext(user.Id, "Boms", "DeleteBom", method: "DELETE");

        await filter.OnAuthorizationAsync(skuLookup);
        await filter.OnAuthorizationAsync(bomCreate);
        await filter.OnAuthorizationAsync(bomUpdate);
        await filter.OnAuthorizationAsync(skuEdit);
        await filter.OnAuthorizationAsync(bomDelete);

        Assert.Null(skuLookup.Result);
        Assert.Null(bomCreate.Result);
        Assert.Null(bomUpdate.Result);
        Assert.IsType<ForbidResult>(skuEdit.Result);
        Assert.IsType<ForbidResult>(bomDelete.Result);

        await RolePermissionCatalog.SeedAsync(db);
        Assert.Equal(1, user.SecurityVersion);
    }

    [Fact]
    public async Task RolePermissionCatalog_AddsReadOnlyFqcAccessToExistingWarehouseOperator()
    {
        using var db = CreateInMemoryDbContext();
        await PermissionCatalog.SeedAsync(db);

        var warehouseRole = new Role { RoleName = "Warehouse Operator", IsSystem = true };
        var user = new User { Username = "warehouse-fqc-user", PasswordHash = "hash" };
        var inventoryView = await db.Permissions.SingleAsync(item => item.PermissionCode == "inventory:view");
        var inventoryExecute = await db.Permissions.SingleAsync(item => item.PermissionCode == "inventory:execute");
        db.AddRange(warehouseRole, user);
        db.RolePermissions.AddRange(
            new RolePermission { RoleId = warehouseRole.Id, PermissionId = inventoryView.Id },
            new RolePermission { RoleId = warehouseRole.Id, PermissionId = inventoryExecute.Id });
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = warehouseRole.Id });
        db.SystemConfigs.Add(new SystemConfig
        {
            ConfigKey = "rbac.default-role-catalog.v1",
            ConfigValue = "previous",
            DataType = "string"
        });
        await db.SaveChangesAsync();

        await RolePermissionCatalog.SeedAsync(db);

        var permissionCodes = await db.RolePermissions
            .Where(item => item.RoleId == warehouseRole.Id)
            .Select(item => item.Permission.PermissionCode)
            .ToListAsync();
        Assert.Contains("quality:view", permissionCodes);
        Assert.Contains("engineering:view", permissionCodes);
        Assert.Contains("inventory:execute", permissionCodes);
        Assert.DoesNotContain("quality:create", permissionCodes);
        Assert.DoesNotContain("quality:approve", permissionCodes);
        Assert.DoesNotContain("engineering:create", permissionCodes);
        Assert.Equal(2, user.SecurityVersion);

        await RolePermissionCatalog.SeedAsync(db);
        Assert.Equal(2, user.SecurityVersion);
    }

    [Fact]
    public async Task RolePermissionCatalog_AddsReadOnlyFqcAccessToExistingWarehouseManager()
    {
        using var db = CreateInMemoryDbContext();
        await PermissionCatalog.SeedAsync(db);

        var warehouseRole = new Role { RoleName = "Warehouse Manager", IsSystem = true };
        var user = new User { Username = "warehouse-manager-fqc-user", PasswordHash = "hash" };
        var inventoryView = await db.Permissions.SingleAsync(item => item.PermissionCode == "inventory:view");
        var inventoryExecute = await db.Permissions.SingleAsync(item => item.PermissionCode == "inventory:execute");
        var qualityView = await db.Permissions.SingleAsync(item => item.PermissionCode == "quality:view");
        db.AddRange(warehouseRole, user);
        db.RolePermissions.AddRange(
            new RolePermission { RoleId = warehouseRole.Id, PermissionId = inventoryView.Id },
            new RolePermission { RoleId = warehouseRole.Id, PermissionId = inventoryExecute.Id },
            new RolePermission { RoleId = warehouseRole.Id, PermissionId = qualityView.Id });
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = warehouseRole.Id });
        db.SystemConfigs.AddRange(
            new SystemConfig
            {
                ConfigKey = "rbac.default-role-catalog.v1",
                ConfigValue = "previous",
                DataType = "string"
            },
            new SystemConfig
            {
                ConfigKey = "rbac.fqc-warehouse-permissions.v1",
                ConfigValue = "previous",
                DataType = "string"
            });
        await db.SaveChangesAsync();

        await RolePermissionCatalog.SeedAsync(db);

        var permissionCodes = await db.RolePermissions
            .Where(item => item.RoleId == warehouseRole.Id)
            .Select(item => item.Permission.PermissionCode)
            .ToListAsync();
        Assert.Contains("quality:view", permissionCodes);
        Assert.Contains("engineering:view", permissionCodes);
        Assert.Contains("inventory:execute", permissionCodes);
        Assert.DoesNotContain("quality:create", permissionCodes);
        Assert.DoesNotContain("quality:approve", permissionCodes);
        Assert.DoesNotContain("engineering:create", permissionCodes);
        Assert.Equal(1, user.SecurityVersion);

        await RolePermissionCatalog.SeedAsync(db);
        Assert.Equal(1, user.SecurityVersion);
    }

    [Fact]
    public async Task RolePermissionCatalog_AddsInspectionLookupAccessToExistingQualityManager()
    {
        using var db = CreateInMemoryDbContext();
        await PermissionCatalog.SeedAsync(db);

        var qualityManagerRole = new Role { RoleName = "Quality Manager", IsSystem = true };
        var user = new User { Username = "quality-manager-inspection-user", PasswordHash = "hash" };
        var qualityView = await db.Permissions.SingleAsync(item => item.PermissionCode == "quality:view");
        db.AddRange(qualityManagerRole, user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = qualityManagerRole.Id });
        db.RolePermissions.Add(new RolePermission
        {
            RoleId = qualityManagerRole.Id,
            PermissionId = qualityView.Id
        });
        db.SystemConfigs.Add(new SystemConfig
        {
            ConfigKey = "rbac.default-role-catalog.v1",
            ConfigValue = "previous",
            DataType = "string"
        });
        await db.SaveChangesAsync();

        await RolePermissionCatalog.SeedAsync(db);

        var permissionCodes = await db.RolePermissions
            .Where(item => item.RoleId == qualityManagerRole.Id)
            .Select(item => item.Permission.PermissionCode)
            .ToListAsync();
        Assert.Contains("engineering:view", permissionCodes);
        Assert.Contains("inventory:view", permissionCodes);
        Assert.Contains("quality:view", permissionCodes);
        Assert.DoesNotContain("engineering:create", permissionCodes);
        Assert.DoesNotContain("inventory:create", permissionCodes);
        Assert.Equal(1, user.SecurityVersion);

        await RolePermissionCatalog.SeedAsync(db);

        Assert.Equal(1, user.SecurityVersion);
    }

    [Fact]
    public async Task RolePermissionCatalog_AddsFqcCreatePermissionToExistingQualityInspector()
    {
        using var db = CreateInMemoryDbContext();
        await PermissionCatalog.SeedAsync(db);

        var qualityInspectorRole = new Role { RoleName = "Quality Inspector", IsSystem = true };
        var user = new User { Username = "quality-inspector-fqc-user", PasswordHash = "hash" };
        var qualityView = await db.Permissions.SingleAsync(item => item.PermissionCode == "quality:view");
        var qualityExecute = await db.Permissions.SingleAsync(item => item.PermissionCode == "quality:execute");
        var qualityCreate = await db.Permissions.SingleAsync(item => item.PermissionCode == "quality:create");
        db.AddRange(qualityInspectorRole, user);
        db.RolePermissions.AddRange(
            new RolePermission { RoleId = qualityInspectorRole.Id, PermissionId = qualityView.Id },
            new RolePermission { RoleId = qualityInspectorRole.Id, PermissionId = qualityExecute.Id });
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = qualityInspectorRole.Id });
        db.SystemConfigs.Add(new SystemConfig
        {
            ConfigKey = "rbac.default-role-catalog.v1",
            ConfigValue = "previous",
            DataType = "string"
        });
        await db.SaveChangesAsync();

        await RolePermissionCatalog.SeedAsync(db);

        var permissionCodes = await db.RolePermissions
            .Where(item => item.RoleId == qualityInspectorRole.Id)
            .Select(item => item.Permission.PermissionCode)
            .ToListAsync();
        Assert.Contains("quality:create", permissionCodes);
        Assert.DoesNotContain("quality:approve", permissionCodes);
        Assert.Equal(1, user.SecurityVersion);

        await RolePermissionCatalog.SeedAsync(db);
        Assert.Equal(1, user.SecurityVersion);
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
    public async Task GetCurrentUserModulesAsync_ReturnsSpecializedResourcesForAdmin()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User { Username = "admin-resource-user", PasswordHash = "hash" };
        var role = new Role { RoleName = "Admin", IsSystem = true };
        db.AddRange(user, role);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync();

        var modules = await new SystemSettingsService(db).GetCurrentUserModulesAsync(user.Id);

        Assert.Contains(modules, item =>
            item.ModuleId == "finance:accounting" && item.Permissions.Contains("create"));
        Assert.Contains(modules, item =>
            item.ModuleId == "inventory:stocktake" && item.Permissions.Contains("execute"));
        Assert.Contains(modules, item =>
            item.ModuleId == "systemsettings:themes" && item.Permissions.Contains("view"));
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
    public async Task PermissionFilter_SeparatesBomAndProcessEngineeringActions()
    {
        using var db = CreateInMemoryDbContext();
        var processUser = new User { Username = "process-engineer", PasswordHash = "hash" };
        var productUser = new User { Username = "product-engineer", PasswordHash = "hash" };
        var processRole = new Role { RoleName = "Process Engineer" };
        var productRole = new Role { RoleName = "Product Engineer" };
        var processCreate = new Permission { PermissionCode = "processengineering:create", ModuleName = "製程工程", ActionType = "create" };
        var productCreate = new Permission { PermissionCode = "productengineering:create", ModuleName = "BOM 管理", ActionType = "create" };
        db.AddRange(processUser, productUser, processRole, productRole, processCreate, productCreate);
        db.UserRoles.AddRange(
            new UserRole { UserId = processUser.Id, RoleId = processRole.Id },
            new UserRole { UserId = productUser.Id, RoleId = productRole.Id });
        db.RolePermissions.AddRange(
            new RolePermission { RoleId = processRole.Id, PermissionId = processCreate.Id },
            new RolePermission { RoleId = productRole.Id, PermissionId = productCreate.Id });
        await db.SaveChangesAsync();

        var filter = new PermissionAuthorizeFilter(db, NullLogger<PermissionAuthorizeFilter>.Instance);
        var processAllowed = CreateAuthorizationContext(processUser.Id, "Routings", "CreateRouting", method: "POST");
        var processDeniedForBom = CreateAuthorizationContext(processUser.Id, "Boms", "CreateBom", method: "POST");
        var productAllowed = CreateAuthorizationContext(productUser.Id, "Boms", "CreateBom", method: "POST");
        var productDeniedForRouting = CreateAuthorizationContext(productUser.Id, "Routings", "CreateRouting", method: "POST");

        await filter.OnAuthorizationAsync(processAllowed);
        await filter.OnAuthorizationAsync(processDeniedForBom);
        await filter.OnAuthorizationAsync(productAllowed);
        await filter.OnAuthorizationAsync(productDeniedForRouting);

        Assert.Null(processAllowed.Result);
        Assert.IsType<ForbidResult>(processDeniedForBom.Result);
        Assert.Null(productAllowed.Result);
        Assert.IsType<ForbidResult>(productDeniedForRouting.Result);
    }

    [Fact]
    public async Task PermissionFilter_DeniesEquipmentListWithoutEquipmentPermission()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User { Username = "no-equipment-access", PasswordHash = "hash" };
        var role = new Role { RoleName = "operator-without-tpm" };
        db.AddRange(user, role);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync();

        var filter = new PermissionAuthorizeFilter(db, NullLogger<PermissionAuthorizeFilter>.Instance);
        var context = CreateAuthorizationContext(user.Id, "Equipment", "GetEquipment");

        await filter.OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task PermissionFilter_AllowsInventoryOrUomViewForUomList()
    {
        using var db = CreateInMemoryDbContext();
        var inventoryUser = new User { Username = "inventory-uom-viewer", PasswordHash = "hash" };
        var uomUser = new User { Username = "uom-viewer", PasswordHash = "hash" };
        var deniedUser = new User { Username = "no-uom-view", PasswordHash = "hash" };
        var inventoryRole = new Role { RoleName = "inventory-uom-viewer" };
        var uomRole = new Role { RoleName = "uom-viewer" };
        var inventoryPermission = new Permission
        {
            PermissionCode = "inventory:view",
            ModuleName = "庫存管理",
            ActionType = "view"
        };
        var uomPermission = new Permission
        {
            PermissionCode = "uom:view",
            ModuleName = "單位量測",
            ActionType = "view"
        };
        db.AddRange(
            inventoryUser,
            uomUser,
            deniedUser,
            inventoryRole,
            uomRole,
            inventoryPermission,
            uomPermission);
        db.UserRoles.AddRange(
            new UserRole { UserId = inventoryUser.Id, RoleId = inventoryRole.Id },
            new UserRole { UserId = uomUser.Id, RoleId = uomRole.Id });
        db.RolePermissions.AddRange(
            new RolePermission { RoleId = inventoryRole.Id, PermissionId = inventoryPermission.Id },
            new RolePermission { RoleId = uomRole.Id, PermissionId = uomPermission.Id });
        await db.SaveChangesAsync();

        var filter = new PermissionAuthorizeFilter(db, NullLogger<PermissionAuthorizeFilter>.Instance);
        var permission = new PermissionAuthorizeAttribute("inventory", "view", "uom:view");
        var inventoryContext = CreateAuthorizationContext(inventoryUser.Id, "Uoms", "GetUoms", permission: permission);
        var uomContext = CreateAuthorizationContext(uomUser.Id, "Uoms", "GetUoms", permission: permission);
        var deniedContext = CreateAuthorizationContext(deniedUser.Id, "Uoms", "GetUoms", permission: permission);

        await filter.OnAuthorizationAsync(inventoryContext);
        await filter.OnAuthorizationAsync(uomContext);
        await filter.OnAuthorizationAsync(deniedContext);

        Assert.Null(inventoryContext.Result);
        Assert.Null(uomContext.Result);
        Assert.IsType<ForbidResult>(deniedContext.Result);
    }

    [Fact]
    public async Task PermissionFilter_AllowsTpmViewToReadWorkCentersWithoutEngineeringAccess()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User { Username = "tpm-work-center-reader", PasswordHash = "hash" };
        var role = new Role { RoleName = "maintenance-technician" };
        var tpmView = new Permission
        {
            PermissionCode = "tpm:view",
            ModuleName = "設備維護",
            ActionType = "view"
        };
        db.AddRange(user, role, tpmView);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = tpmView.Id });
        await db.SaveChangesAsync();

        var filter = new PermissionAuthorizeFilter(db, NullLogger<PermissionAuthorizeFilter>.Instance);
        var workCenterListPermission = new PermissionAuthorizeAttribute("engineering", "view", "tpm:view");
        var listContext = CreateAuthorizationContext(
            user.Id,
            "WorkCenters",
            "GetWorkCenters",
            permission: workCenterListPermission);
        var createContext = CreateAuthorizationContext(
            user.Id,
            "WorkCenters",
            "CreateWorkCenter",
            method: "POST");

        await filter.OnAuthorizationAsync(listContext);
        await filter.OnAuthorizationAsync(createContext);

        Assert.Null(listContext.Result);
        Assert.IsType<ForbidResult>(createContext.Result);
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
