using ErpBackend.Data;
using ErpBackend.Models.System;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public static class RolePermissionCatalog
{
    private const string SeedVersion = "rbac.default-role-catalog.v1";
    private const string FqcWarehousePermissionSeedVersion = "rbac.fqc-warehouse-permissions.v1";
    private const string FqcWarehouseManagerPermissionSeedVersion = "rbac.fqc-warehouse-manager-permissions.v1";
    private const string QualityManagerInspectionLookupPermissionSeedVersion = "rbac.quality-manager-inspection-lookup-permissions.v1";
    private const string WarehouseInspectionLookupPermissionSeedVersion = "rbac.warehouse-inspection-lookup-permissions.v1";
    private const string QualityInspectorFqcCreatePermissionSeedVersion = "rbac.quality-inspector-fqc-create-permission.v1";
    private const string EngineeringRoleSeedVersion = "rbac.specialized-engineering-roles.v1";
    private const string EngineeringManagerBomPermissionSeedVersion = "rbac.engineering-manager-bom-permissions.v1";
    private const string SalesRepresentativeSkuLookupPermissionSeedVersion = "rbac.sales-representative-sku-lookup-permission.v1";
    private const string ProductionSupervisorWorkOrderTransferPermissionSeedVersion = "rbac.production-supervisor-work-order-transfer-permissions.v1";
    private static readonly string[] CommonPermissions = ["dashboard:view", "kpi:view"];
    private static readonly IReadOnlyDictionary<string, string> ExistingUatRoleDescriptions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["UAT Warehouse Approver"] = "隔離 UAT 環境的合成倉儲覆核角色，僅供測試使用，不得作為正式帳號權限範本。",
            ["UAT Warehouse Operator"] = "隔離 UAT 環境的合成倉儲作業角色，僅供測試使用，不得作為正式帳號權限範本。"
        };

    private static readonly RoleDefinition[] Definitions =
    [
        Define("Admin", "系統管理員，負責帳號、角色、權限與跨模組系統維運；此角色具全域最高權限，應限少數受控管理者使用。"),
        Define("HR Manager", "人資主管，管理員工、組織、人事資料與出勤請假，並負責薪資政策送審；不授予刪除或計薪執行權。",
            Grant("hr", "view", "create", "edit", "approve", "export"),
            Grant("org", "view", "create", "edit"),
            Grant("calendar", "view", "create", "edit"),
            Grant("collaboration", "view")),
        Define("Production Supervisor", "生產主管，管理工單與產線執行、審核報工及監控生產；不得刪除資料，報工仍須遵守不同人覆核。",
            Grant("manufacturing", "view", "create", "edit", "approve", "export", "execute"),
            Grant("mps", "view", "edit", "execute"),
            Grant("rccp", "view", "edit", "execute"),
            Grant("mrp", "view", "execute"),
            Grant("aps", "view", "edit", "execute"),
            Grant("engineering", "view", "approve"),
            Grant("inventory", "view"),
            Grant("quality", "view"),
            Grant("tpm", "view"),
            Grant("collaboration", "view", "approve"),
            Grant("calendar", "view", "create", "edit")),
        Define("Operator", "現場作業員，檢視工單並提交本人或班組報工，使用員工自助服務；不具報工審核、刪除或庫存調整權。",
            Grant("manufacturing", "view", "create", "execute"),
            Grant("calendar", "view")),
        Define("Finance", "財務人員，處理財務日常資料並查詢成本、預算與成本中心；不具核准、刪除或沖銷權。",
            Grant("finance", "view", "create", "edit", "export"),
            Grant("budget", "view"),
            Grant("actual_costing", "view"),
            Grant("costcenter", "view"),
            Grant("calendar", "view")),
        Define("Finance Manager", "財務主管，負責會計、預算、成本結轉與成本中心覆核及匯出；具核准權但不授予刪除權。",
            Grant("finance", "view", "create", "edit", "approve", "export", "execute"),
            Grant("budget", "view", "create", "edit", "approve", "export", "execute"),
            Grant("actual_costing", "view", "create", "edit", "approve", "export", "execute"),
            Grant("costcenter", "view", "create", "edit", "approve", "export"),
            Grant("ediexport", "view", "export", "execute"),
            Grant("collaboration", "view", "approve"),
            Grant("calendar", "view", "create", "edit")),
        Define("Sales Manager", "業務主管，管理客戶、報價、訂單與出貨流程，負責業務核准及必要資料匯出；不授予刪除權。",
            Grant("sales", "view", "create", "edit", "approve", "export", "execute"),
            Grant("ediexport", "view", "export", "execute"),
            Grant("collaboration", "view", "create", "approve"),
            Grant("calendar", "view", "create", "edit")),
        Define("Sales Representative", "業務人員，維護客戶往來、報價與訂單資料並匯出業務報表；不具核准或刪除權。",
            Grant("sales", "view", "create", "edit", "export"),
            Grant("calendar", "view")),
        Define("Purchasing Manager", "採購主管，管理採購與供應商協作，負責採購核准及必要沖銷；不授予刪除權。",
            Grant("procurement", "view", "create", "edit", "approve", "export", "execute", "reverse"),
            Grant("suppliers", "view", "create", "edit", "approve", "export"),
            Grant("inventory", "view"),
            Grant("ediexport", "view", "export", "execute"),
            Grant("collaboration", "view", "approve"),
            Grant("calendar", "view", "create", "edit")),
        Define("Purchasing Specialist", "採購專員，建立與維護請購、採購及供應商資料，執行日常採購作業；不具核准、沖銷或刪除權。",
            Grant("procurement", "view", "create", "edit", "export", "execute"),
            Grant("suppliers", "view", "create", "edit"),
            Grant("inventory", "view"),
            Grant("calendar", "view")),
        Define("Warehouse Manager", "倉儲主管，管理庫存、條碼與倉儲作業，覆核必要庫存流程；可唯讀查詢工藝與產品主檔以執行工藝檢驗；不授予刪除權。",
            Grant("inventory", "view", "create", "edit", "approve", "export", "execute"),
            Grant("barcode", "view", "create", "edit", "execute"),
            Grant("procurement", "view"),
            Grant("quality", "view"),
            Grant("engineering", "view"),
            Grant("collaboration", "view", "approve"),
            Grant("calendar", "view", "create", "edit")),
        Define("Warehouse Operator", "倉管人員，檢視庫存並執行收發、調撥及盤點等日常作業；可唯讀查詢工藝與產品主檔以執行工藝檢驗；不具核准、刪除或沖銷權。",
            Grant("inventory", "view", "create", "edit", "execute"),
            Grant("barcode", "view", "create", "execute"),
            Grant("procurement", "view", "create", "execute"),
            Grant("quality", "view"),
            Grant("engineering", "view"),
            Grant("calendar", "view")),
        Define("Quality Manager", "品質主管，管理檢驗與品質異常，覆核品質判定並查詢供應商品質事項；不授予刪除權。",
            Grant("quality", "view", "create", "edit", "approve", "export", "execute"),
            Grant("suppliers", "view", "approve"),
            Grant("manufacturing", "view"),
            Grant("collaboration", "view", "approve"),
            Grant("calendar", "view", "create", "edit")),
        Define("Quality Inspector", "品檢人員，執行進料、製程與成品檢驗並登錄檢驗結果；不具品質核准或刪除權。",
            Grant("quality", "view", "create", "edit", "execute"),
            Grant("manufacturing", "view"),
            Grant("calendar", "view")),
        Define("Production Planner", "生管與物料規劃人員，維護主排程、產能、物料需求與細排程；不具核准或刪除權。",
            Grant("mps", "view", "create", "edit", "execute"),
            Grant("rccp", "view", "create", "edit", "execute"),
            Grant("mrp", "view", "create", "edit", "execute"),
            Grant("aps", "view", "create", "edit", "execute"),
            Grant("manufacturing", "view"),
            Grant("engineering", "view"),
            Grant("calendar", "view", "create", "edit")),
        Define("Engineering Manager", "工程主管，維護工藝、BOM、工作中心與量測主檔，覆核工程變更；不授予刪除權。",
            Grant("engineering", "view", "create", "edit", "approve", "export"),
            Grant("productengineering", "view", "create", "edit"),
            Grant("inventory", "view"),
            Grant("uom", "view", "create", "edit"),
            Grant("rd", "view"),
            Grant("assets", "view"),
            Grant("calendar", "view", "create", "edit")),
        Define("Engineering Engineer", "工程人員，建立與維護工藝、BOM、工作中心及研發資料；不具核准或刪除權。",
            Grant("engineering", "view", "create", "edit"),
            Grant("uom", "view"),
            Grant("rd", "view", "create", "edit", "execute"),
            Grant("calendar", "view")),
        Define("Maintenance Technician", "設備維護人員，登錄設備維修、保養與資產查詢；不具資產核准或刪除權。",
            Grant("tpm", "view", "create", "edit", "execute"),
            Grant("assets", "view"),
            Grant("inventory", "view"),
            Grant("calendar", "view")),
        Define("Asset Manager", "資產管理人員，維護資產資料並執行折舊與處置流程，必要事項須經核准；不授予刪除權。",
            Grant("assets", "view", "create", "edit", "approve", "export", "execute"),
            Grant("finance", "view"),
            Grant("calendar", "view")),
        Define("R&D Engineer", "研發人員，建立與維護研發專案資料並執行核准的研發流程；不具核准或刪除權。",
            Grant("rd", "view", "create", "edit", "execute"),
            Grant("engineering", "view"),
            Grant("calendar", "view")),
        Define("ESG Specialist", "永續管理人員，維護碳盤查與 ESG 資料、提交報告供覆核；不授予刪除權。",
            Grant("esg", "view", "create", "edit", "approve", "export"),
            Grant("calendar", "view")),
        Define("AI Analyst", "AI 分析人員，使用 AI 監控與決策查詢功能；不具系統設定、代理流程編輯或跨模組資料管理權。",
            Grant("ai", "view", "execute"),
            Grant("agentflows", "view"),
            Grant("aidecision", "view", "execute"),
            Grant("aischeduling", "view"),
            Grant("calendar", "view")),
        Define("Employee", "一般員工，檢視系統總覽與行事曆；個人資料及員工自助服務依員工身分另行控管。",
            Grant("calendar", "view"))
    ];

    public static async Task SeedAsync(ErpGeneralDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.SystemConfigs.AnyAsync(
                item => item.ConfigKey == SeedVersion,
                cancellationToken))
        {
            await SeedFqcWarehousePermissionAsync(db, cancellationToken);
            await SeedFqcWarehouseManagerPermissionAsync(db, cancellationToken);
            await SeedQualityManagerInspectionLookupPermissionsAsync(db, cancellationToken);
            await SeedWarehouseInspectionLookupPermissionsAsync(db, cancellationToken);
            await SeedQualityInspectorFqcCreatePermissionAsync(db, cancellationToken);
            await SeedSpecializedEngineeringRolesAsync(db, cancellationToken);
            await SeedEngineeringManagerBomPermissionsAsync(db, cancellationToken);
            await SeedSalesRepresentativeSkuLookupPermissionAsync(db, cancellationToken);
            await SeedProductionSupervisorWorkOrderTransferPermissionsAsync(db, cancellationToken);
            return;
        }

        var permissionIds = await db.Permissions
            .Where(permission => permission.IsActive)
            .ToDictionaryAsync(
                permission => permission.PermissionCode,
                permission => permission.Id,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);
        var roles = await db.Roles.ToListAsync(cancellationToken);
        var rolesByName = roles.ToDictionary(role => role.RoleName, StringComparer.OrdinalIgnoreCase);
        var affectedRoleIds = new HashSet<Guid>();

        foreach (var role in roles)
        {
            if (ExistingUatRoleDescriptions.TryGetValue(role.RoleName, out var description))
                role.Description = description;
        }

        foreach (var definition in Definitions)
        {
            if (!rolesByName.TryGetValue(definition.Name, out var role))
            {
                role = new Role
                {
                    Id = Guid.NewGuid(),
                    RoleName = definition.Name,
                    IsSystem = true,
                    IsActive = true
                };
                db.Roles.Add(role);
                rolesByName.Add(role.RoleName, role);
            }

            role.Description = definition.Description;
            role.IsSystem = true;

            if (string.Equals(definition.Name, "Admin", StringComparison.OrdinalIgnoreCase))
                continue;

            var requestedCodes = new HashSet<string>(CommonPermissions, StringComparer.OrdinalIgnoreCase);
            foreach (var (module, actions) in definition.Grants)
            foreach (var action in actions)
                requestedCodes.Add(PermissionCatalog.GetActionCode(module, action));

            var missingCode = requestedCodes.FirstOrDefault(code => !permissionIds.ContainsKey(code));
            if (missingCode != null)
                throw new InvalidOperationException($"Role catalog references missing permission '{missingCode}'.");

            var desiredPermissionIds = requestedCodes
                .Select(code => permissionIds[code])
                .ToHashSet();
            var currentRolePermissions = await db.RolePermissions
                .Where(item => item.RoleId == role.Id)
                .ToListAsync(cancellationToken);
            var currentPermissionIds = currentRolePermissions
                .Select(item => item.PermissionId)
                .ToHashSet();

            if (currentPermissionIds.SetEquals(desiredPermissionIds))
                continue;

            affectedRoleIds.Add(role.Id);
            db.RolePermissions.RemoveRange(currentRolePermissions);
            foreach (var permissionId in desiredPermissionIds)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    PermissionId = permissionId
                });
            }
        }

        if (affectedRoleIds.Count > 0)
        {
            var users = await db.Users
                .Where(user => db.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id && affectedRoleIds.Contains(userRole.RoleId)))
                .ToListAsync(cancellationToken);
            foreach (var user in users)
                user.SecurityVersion = checked(user.SecurityVersion + 1);
        }

        db.SystemConfigs.Add(new SystemConfig
        {
            Id = Guid.NewGuid(),
            ConfigKey = SeedVersion,
            ConfigValue = DateTime.UtcNow.ToString("O"),
            DataType = "string",
            Description = "全系統繁體中文角色及最小權限預設已初始化。"
        });
        await db.SaveChangesAsync(cancellationToken);
        await SeedFqcWarehousePermissionAsync(db, cancellationToken);
        await SeedFqcWarehouseManagerPermissionAsync(db, cancellationToken);
        await SeedQualityManagerInspectionLookupPermissionsAsync(db, cancellationToken);
        await SeedWarehouseInspectionLookupPermissionsAsync(db, cancellationToken);
        await SeedQualityInspectorFqcCreatePermissionAsync(db, cancellationToken);
        await SeedSpecializedEngineeringRolesAsync(db, cancellationToken);
        await SeedEngineeringManagerBomPermissionsAsync(db, cancellationToken);
        await SeedSalesRepresentativeSkuLookupPermissionAsync(db, cancellationToken);
        await SeedProductionSupervisorWorkOrderTransferPermissionsAsync(db, cancellationToken);
    }

    private static async Task SeedProductionSupervisorWorkOrderTransferPermissionsAsync(
        ErpGeneralDbContext db,
        CancellationToken cancellationToken)
    {
        if (await db.SystemConfigs.AnyAsync(
                item => item.ConfigKey == ProductionSupervisorWorkOrderTransferPermissionSeedVersion,
                cancellationToken))
            return;

        var role = await db.Roles
            .SingleOrDefaultAsync(item => item.RoleName == "Production Supervisor", cancellationToken);
        var permissionCodes = new[]
        {
            "sales:workordertransfer:page",
            "sales:workordertransfer:execute"
        };
        var permissions = await db.Permissions
            .Where(permission => permissionCodes.Contains(permission.PermissionCode)
                && permission.IsActive)
            .ToDictionaryAsync(permission => permission.PermissionCode, cancellationToken);
        if (role is null || permissionCodes.Any(code => !permissions.ContainsKey(code)))
            return;

        var assignedPermissionIds = await db.RolePermissions
            .Where(item => item.RoleId == role.Id)
            .Select(item => item.PermissionId)
            .ToListAsync(cancellationToken);
        var missingPermissions = permissionCodes
            .Select(code => permissions[code])
            .Where(permission => !assignedPermissionIds.Contains(permission.Id))
            .ToList();
        if (missingPermissions.Count > 0)
        {
            foreach (var permission in missingPermissions)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    PermissionId = permission.Id
                });
            }

            var users = await db.Users
                .Where(user => db.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id && userRole.RoleId == role.Id))
                .ToListAsync(cancellationToken);
            foreach (var user in users)
                user.SecurityVersion = checked(user.SecurityVersion + 1);
        }

        db.SystemConfigs.Add(new SystemConfig
        {
            Id = Guid.NewGuid(),
            ConfigKey = ProductionSupervisorWorkOrderTransferPermissionSeedVersion,
            ConfigValue = DateTime.UtcNow.ToString("O"),
            DataType = "string",
            Description = "生產主管僅能檢視符合條件的銷售訂單並轉發工單，不授予一般銷售模組權限。"
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSalesRepresentativeSkuLookupPermissionAsync(
        ErpGeneralDbContext db,
        CancellationToken cancellationToken)
    {
        if (await db.SystemConfigs.AnyAsync(
                item => item.ConfigKey == SalesRepresentativeSkuLookupPermissionSeedVersion,
                cancellationToken))
            return;

        var salesRepresentativeRole = await db.Roles
            .SingleOrDefaultAsync(role => role.RoleName == "Sales Representative", cancellationToken);
        var skuViewPermission = await db.Permissions
            .SingleOrDefaultAsync(permission => permission.PermissionCode == "inventory:skus:view"
                && permission.IsActive, cancellationToken);
        if (salesRepresentativeRole is null || skuViewPermission is null)
            return;

        var hasSkuViewPermission = await db.RolePermissions.AnyAsync(
            item => item.RoleId == salesRepresentativeRole.Id
                && item.PermissionId == skuViewPermission.Id,
            cancellationToken);
        if (!hasSkuViewPermission)
        {
            db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = salesRepresentativeRole.Id,
                PermissionId = skuViewPermission.Id
            });

            var users = await db.Users
                .Where(user => db.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id && userRole.RoleId == salesRepresentativeRole.Id))
                .ToListAsync(cancellationToken);
            foreach (var user in users)
                user.SecurityVersion = checked(user.SecurityVersion + 1);
        }

        db.SystemConfigs.Add(new SystemConfig
        {
            Id = Guid.NewGuid(),
            ConfigKey = SalesRepresentativeSkuLookupPermissionSeedVersion,
            ConfigValue = DateTime.UtcNow.ToString("O"),
            DataType = "string",
            Description = "業務人員可唯讀查詢商品 SKU，不授予庫存管理權限。"
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedEngineeringManagerBomPermissionsAsync(
        ErpGeneralDbContext db,
        CancellationToken cancellationToken)
    {
        if (await db.SystemConfigs.AnyAsync(
                item => item.ConfigKey == EngineeringManagerBomPermissionSeedVersion,
                cancellationToken))
            return;

        var engineeringManagerRole = await db.Roles
            .SingleOrDefaultAsync(role => role.RoleName == "Engineering Manager", cancellationToken);
        var permissions = await db.Permissions
            .Where(permission => permission.PermissionCode == "inventory:view"
                || permission.PermissionCode == "productengineering:view"
                || permission.PermissionCode == "productengineering:create"
                || permission.PermissionCode == "productengineering:edit")
            .ToDictionaryAsync(permission => permission.PermissionCode, cancellationToken);
        var requiredPermissionCodes = new[]
        {
            "inventory:view",
            "productengineering:view",
            "productengineering:create",
            "productengineering:edit"
        };
        if (engineeringManagerRole is null
            || requiredPermissionCodes.Any(code => !permissions.ContainsKey(code)))
            return;

        var assignedPermissionIds = await db.RolePermissions
            .Where(item => item.RoleId == engineeringManagerRole.Id)
            .Select(item => item.PermissionId)
            .ToListAsync(cancellationToken);
        var missingPermissions = requiredPermissionCodes
            .Select(code => permissions[code])
            .Where(permission => !assignedPermissionIds.Contains(permission.Id))
            .ToList();
        if (missingPermissions.Count > 0)
        {
            foreach (var permission in missingPermissions)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = engineeringManagerRole.Id,
                    PermissionId = permission.Id
                });
            }

            var users = await db.Users
                .Where(user => db.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id && userRole.RoleId == engineeringManagerRole.Id))
                .ToListAsync(cancellationToken);
            foreach (var user in users)
                user.SecurityVersion = checked(user.SecurityVersion + 1);
        }

        db.SystemConfigs.Add(new SystemConfig
        {
            Id = Guid.NewGuid(),
            ConfigKey = EngineeringManagerBomPermissionSeedVersion,
            ConfigValue = DateTime.UtcNow.ToString("O"),
            DataType = "string",
            Description = "工程主管可檢視 SKU 並新增、修改 BOM，不具庫存異動或 BOM 刪除權。"
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSpecializedEngineeringRolesAsync(
        ErpGeneralDbContext db,
        CancellationToken cancellationToken)
    {
        if (await db.SystemConfigs.AnyAsync(
                item => item.ConfigKey == EngineeringRoleSeedVersion,
                cancellationToken))
            return;

        var permissionIds = await db.Permissions
            .Where(permission => permission.IsActive)
            .ToDictionaryAsync(
                permission => permission.PermissionCode,
                permission => permission.Id,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);
        var roles = await db.Roles.ToListAsync(cancellationToken);
        var rolesByName = roles.ToDictionary(role => role.RoleName, StringComparer.OrdinalIgnoreCase);
        var definitions = new[]
        {
            new EngineeringRoleDefinition(
                "Process Engineer",
                "製程工程師，建立與維護工序、工藝路線、工作中心及工程變更；可檢視產品資料，不具 BOM 維護、核准或刪除權。",
                ["dashboard:view", "kpi:view", "engineering:view", "inventory:view",
                    "processengineering:view", "processengineering:create", "processengineering:edit"]),
            new EngineeringRoleDefinition(
                "Product Engineer",
                "產品工程師，建立與維護產品 BOM；可檢視產品及製程資料，不具工藝路線維護、核准或刪除權。",
                ["dashboard:view", "kpi:view", "engineering:view", "inventory:view",
                    "productengineering:view", "productengineering:create", "productengineering:edit"]),
            new EngineeringRoleDefinition(
                "Quality Engineer",
                "品質工程師，維護檢驗規格與品質資料，並唯讀查詢產品、BOM 及製程資料；不具品質核准或刪除權。",
                ["dashboard:view", "kpi:view", "quality:view", "quality:create", "quality:edit",
                    "manufacturing:view", "inventory:view", "engineering:view",
                    "productengineering:view", "processengineering:view"])
        };
        var affectedRoleIds = new HashSet<Guid>();

        foreach (var definition in definitions)
        {
            var missingCode = definition.PermissionCodes.FirstOrDefault(code => !permissionIds.ContainsKey(code));
            if (missingCode != null)
                throw new InvalidOperationException($"Role catalog references missing permission '{missingCode}'.");

            if (!rolesByName.TryGetValue(definition.Name, out var role))
            {
                role = new Role
                {
                    Id = Guid.NewGuid(),
                    RoleName = definition.Name,
                    IsSystem = true,
                    IsActive = true
                };
                db.Roles.Add(role);
                rolesByName.Add(role.RoleName, role);
            }

            role.Description = definition.Description;
            role.IsSystem = true;

            var desiredPermissionIds = definition.PermissionCodes
                .Select(code => permissionIds[code])
                .ToHashSet();
            var currentRolePermissions = await db.RolePermissions
                .Where(item => item.RoleId == role.Id)
                .ToListAsync(cancellationToken);
            if (currentRolePermissions.Select(item => item.PermissionId).ToHashSet().SetEquals(desiredPermissionIds))
                continue;

            affectedRoleIds.Add(role.Id);
            db.RolePermissions.RemoveRange(currentRolePermissions);
            foreach (var permissionId in desiredPermissionIds)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    PermissionId = permissionId
                });
            }
        }

        if (affectedRoleIds.Count > 0)
        {
            var users = await db.Users
                .Where(user => db.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id && affectedRoleIds.Contains(userRole.RoleId)))
                .ToListAsync(cancellationToken);
            foreach (var user in users)
                user.SecurityVersion = checked(user.SecurityVersion + 1);
        }

        db.SystemConfigs.Add(new SystemConfig
        {
            Id = Guid.NewGuid(),
            ConfigKey = EngineeringRoleSeedVersion,
            ConfigValue = DateTime.UtcNow.ToString("O"),
            DataType = "string",
            Description = "製程工程師、產品工程師及品質工程師的最小權限角色已初始化。"
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private sealed record EngineeringRoleDefinition(
        string Name,
        string Description,
        string[] PermissionCodes);

    private static async Task SeedFqcWarehousePermissionAsync(
        ErpGeneralDbContext db,
        CancellationToken cancellationToken)
    {
        if (await db.SystemConfigs.AnyAsync(
                item => item.ConfigKey == FqcWarehousePermissionSeedVersion,
                cancellationToken))
            return;

        var warehouseRole = await db.Roles
            .SingleOrDefaultAsync(role => role.RoleName == "Warehouse Operator", cancellationToken);
        var qualityView = await db.Permissions
            .SingleOrDefaultAsync(permission => permission.PermissionCode == "quality:view", cancellationToken);
        if (warehouseRole is null || qualityView is null)
            return;

        var hasQualityView = await db.RolePermissions.AnyAsync(
            item => item.RoleId == warehouseRole.Id && item.PermissionId == qualityView.Id,
            cancellationToken);
        if (!hasQualityView)
        {
            db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = warehouseRole.Id,
                PermissionId = qualityView.Id
            });

            var users = await db.Users
                .Where(user => db.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id && userRole.RoleId == warehouseRole.Id))
                .ToListAsync(cancellationToken);
            foreach (var user in users)
                user.SecurityVersion = checked(user.SecurityVersion + 1);
        }

        db.SystemConfigs.Add(new SystemConfig
        {
            Id = Guid.NewGuid(),
            ConfigKey = FqcWarehousePermissionSeedVersion,
            ConfigValue = DateTime.UtcNow.ToString("O"),
            DataType = "string",
            Description = "倉管角色可唯讀檢視 FQC 批次以執行合格品入庫。"
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedFqcWarehouseManagerPermissionAsync(
        ErpGeneralDbContext db,
        CancellationToken cancellationToken)
    {
        if (await db.SystemConfigs.AnyAsync(
                item => item.ConfigKey == FqcWarehouseManagerPermissionSeedVersion,
                cancellationToken))
            return;

        var warehouseRole = await db.Roles
            .SingleOrDefaultAsync(role => role.RoleName == "Warehouse Manager", cancellationToken);
        var qualityView = await db.Permissions
            .SingleOrDefaultAsync(permission => permission.PermissionCode == "quality:view", cancellationToken);
        if (warehouseRole is null || qualityView is null)
            return;

        var hasQualityView = await db.RolePermissions.AnyAsync(
            item => item.RoleId == warehouseRole.Id && item.PermissionId == qualityView.Id,
            cancellationToken);
        if (!hasQualityView)
        {
            db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = warehouseRole.Id,
                PermissionId = qualityView.Id
            });

            var users = await db.Users
                .Where(user => db.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id && userRole.RoleId == warehouseRole.Id))
                .ToListAsync(cancellationToken);
            foreach (var user in users)
                user.SecurityVersion = checked(user.SecurityVersion + 1);
        }

        db.SystemConfigs.Add(new SystemConfig
        {
            Id = Guid.NewGuid(),
            ConfigKey = FqcWarehouseManagerPermissionSeedVersion,
            ConfigValue = DateTime.UtcNow.ToString("O"),
            DataType = "string",
            Description = "倉儲主管可唯讀檢視 FQC 批次以執行合格品入庫。"
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedQualityManagerInspectionLookupPermissionsAsync(
        ErpGeneralDbContext db,
        CancellationToken cancellationToken)
    {
        if (await db.SystemConfigs.AnyAsync(
                item => item.ConfigKey == QualityManagerInspectionLookupPermissionSeedVersion,
                cancellationToken))
            return;

        var qualityManagerRole = await db.Roles
            .SingleOrDefaultAsync(role => role.RoleName == "Quality Manager", cancellationToken);
        var lookupPermissions = await db.Permissions
            .Where(permission => permission.PermissionCode == "engineering:view"
                || permission.PermissionCode == "inventory:view")
            .ToDictionaryAsync(permission => permission.PermissionCode, cancellationToken);
        if (qualityManagerRole is null
            || !lookupPermissions.TryGetValue("engineering:view", out var engineeringView)
            || !lookupPermissions.TryGetValue("inventory:view", out var inventoryView))
            return;

        var assignedPermissionIds = await db.RolePermissions
            .Where(item => item.RoleId == qualityManagerRole.Id)
            .Select(item => item.PermissionId)
            .ToListAsync(cancellationToken);
        var missingPermissions = new[] { engineeringView, inventoryView }
            .Where(permission => !assignedPermissionIds.Contains(permission.Id))
            .ToList();
        if (missingPermissions.Count > 0)
        {
            foreach (var permission in missingPermissions)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = qualityManagerRole.Id,
                    PermissionId = permission.Id
                });
            }

            var users = await db.Users
                .Where(user => db.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id && userRole.RoleId == qualityManagerRole.Id))
                .ToListAsync(cancellationToken);
            foreach (var user in users)
                user.SecurityVersion = checked(user.SecurityVersion + 1);
        }

        db.SystemConfigs.Add(new SystemConfig
        {
            Id = Guid.NewGuid(),
            ConfigKey = QualityManagerInspectionLookupPermissionSeedVersion,
            ConfigValue = DateTime.UtcNow.ToString("O"),
            DataType = "string",
            Description = "品質主管可唯讀查詢工藝與產品主檔以維護工藝檢驗範本。"
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedWarehouseInspectionLookupPermissionsAsync(
        ErpGeneralDbContext db,
        CancellationToken cancellationToken)
    {
        if (await db.SystemConfigs.AnyAsync(
                item => item.ConfigKey == WarehouseInspectionLookupPermissionSeedVersion,
                cancellationToken))
            return;

        var warehouseRoles = await db.Roles
            .Where(role => role.RoleName == "Warehouse Operator"
                || role.RoleName == "Warehouse Manager")
            .ToListAsync(cancellationToken);
        var engineeringView = await db.Permissions
            .SingleOrDefaultAsync(permission => permission.PermissionCode == "engineering:view", cancellationToken);
        if (warehouseRoles.Count == 0 || engineeringView is null)
            return;

        var rolesMissingPermission = new List<Guid>();
        foreach (var role in warehouseRoles)
        {
            var hasEngineeringView = await db.RolePermissions.AnyAsync(
                item => item.RoleId == role.Id && item.PermissionId == engineeringView.Id,
                cancellationToken);
            if (hasEngineeringView)
                continue;

            db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = role.Id,
                PermissionId = engineeringView.Id
            });
            rolesMissingPermission.Add(role.Id);
        }

        if (rolesMissingPermission.Count > 0)
        {
            var users = await db.Users
                .Where(user => db.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id && rolesMissingPermission.Contains(userRole.RoleId)))
                .ToListAsync(cancellationToken);
            foreach (var user in users)
                user.SecurityVersion = checked(user.SecurityVersion + 1);
        }

        db.SystemConfigs.Add(new SystemConfig
        {
            Id = Guid.NewGuid(),
            ConfigKey = WarehouseInspectionLookupPermissionSeedVersion,
            ConfigValue = DateTime.UtcNow.ToString("O"),
            DataType = "string",
            Description = "倉管角色可唯讀查詢工藝主檔以執行工藝檢驗。"
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedQualityInspectorFqcCreatePermissionAsync(
        ErpGeneralDbContext db,
        CancellationToken cancellationToken)
    {
        if (await db.SystemConfigs.AnyAsync(
                item => item.ConfigKey == QualityInspectorFqcCreatePermissionSeedVersion,
                cancellationToken))
            return;

        var qualityInspectorRole = await db.Roles
            .SingleOrDefaultAsync(role => role.RoleName == "Quality Inspector", cancellationToken);
        var qualityCreate = await db.Permissions
            .SingleOrDefaultAsync(permission => permission.PermissionCode == "quality:create", cancellationToken);
        if (qualityInspectorRole is null || qualityCreate is null)
            return;

        var hasCreatePermission = await db.RolePermissions.AnyAsync(
            item => item.RoleId == qualityInspectorRole.Id && item.PermissionId == qualityCreate.Id,
            cancellationToken);
        if (!hasCreatePermission)
        {
            db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = qualityInspectorRole.Id,
                PermissionId = qualityCreate.Id
            });

            var users = await db.Users
                .Where(user => db.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id && userRole.RoleId == qualityInspectorRole.Id))
                .ToListAsync(cancellationToken);
            foreach (var user in users)
                user.SecurityVersion = checked(user.SecurityVersion + 1);
        }

        db.SystemConfigs.Add(new SystemConfig
        {
            Id = Guid.NewGuid(),
            ConfigKey = QualityInspectorFqcCreatePermissionSeedVersion,
            ConfigValue = DateTime.UtcNow.ToString("O"),
            DataType = "string",
            Description = "品檢人員可建立 FQC 檢驗及隔離品報廢申請，不含品質核准權。"
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static RoleDefinition Define(
        string name,
        string description,
        params (string Module, string[] Actions)[] grants)
        => new(name, description, grants);

    private static (string Module, string[] Actions) Grant(string module, params string[] actions)
        => (module, actions);

    private sealed record RoleDefinition(
        string Name,
        string Description,
        (string Module, string[] Actions)[] Grants);
}
