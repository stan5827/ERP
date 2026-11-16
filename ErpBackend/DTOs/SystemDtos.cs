using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public class LoginRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginResponse
{
    public Guid UserId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string Username { get; set; } = null!;
    public string? Email { get; set; }
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public bool MustChangePassword { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string? Email { get; set; }
    public Guid? EmployeeId { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class RoleSummaryDto
{
    public Guid Id { get; set; }
    public string RoleName { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
}

public class CreateUserRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Email { get; set; }
    public Guid? EmployeeId { get; set; }
    public List<Guid> RoleIds { get; set; } = new();
}

public class UpdateUserRequestDto
{
    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }
}

public class CustomerDto
{
    public Guid Id { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public string? TaxId { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? InvoiceAddress { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Currency { get; set; }
    public decimal CreditLimit { get; set; }
    public bool IsActive { get; set; }
}

public class WarehouseDto
{
    public Guid Id { get; set; }
    public string? WarehouseCode { get; set; }
    public string? WarehouseName { get; set; }
    public string? Address { get; set; }
    public string? Type { get; set; }
    public bool IsActive { get; set; }
    public int BinLocationCount { get; set; }
}

public class UserWithRolesDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string? Email { get; set; }
    public Guid? EmployeeId { get; set; }
    public bool IsActive { get; set; }
    public List<RoleDto> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string? RoleName { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<Guid> PermissionIds { get; set; } = new();
}

public class RoleWithPermissionsDto
{
    public Guid Id { get; set; }
    public string? RoleName { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<Guid> PermissionIds { get; set; } = new();
}

public class PermissionTreeDto
{
    public Guid Id { get; set; }
    public string? PermissionCode { get; set; }
    public string? ModuleName { get; set; }
    public string? ActionType { get; set; }
    public string? PermissionType { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public List<PermissionTreeDto> Children { get; set; } = new();
}

public class CreatePermissionRequest
{
    public string PermissionCode { get; set; } = null!;
    public string ModuleName { get; set; } = null!;
    public string ActionType { get; set; } = null!;
    public string PermissionType { get; set; } = "action";
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }
}

public class UpdatePermissionRequest
{
    public string PermissionCode { get; set; } = null!;
    public string ModuleName { get; set; } = null!;
    public string ActionType { get; set; } = null!;
    public string PermissionType { get; set; } = "action";
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }
}

public class CreateRoleRequest
{
    public string RoleName { get; set; } = null!;
    public string? Description { get; set; }
    public List<Guid> PermissionIds { get; set; } = new();
}

public class UpdateRolePermissionsRequest
{
    public List<Guid> PermissionIds { get; set; } = new();
}

public class ThemeDto
{
    public string Name { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public ThemeColorsDto? Colors { get; set; }
}

public class ThemeColorsDto
{
    public string Primary { get; set; } = null!;
    public string Accent { get; set; } = null!;
    public string Background { get; set; } = null!;
    public string Surface { get; set; } = null!;
    public string Text { get; set; } = null!;
    public string Border { get; set; } = null!;
}

public class UpdateThemeRequest
{
    public string ThemeName { get; set; } = null!;
    public string? CustomPrimaryColor { get; set; }
    public string? CustomAccentColor { get; set; }
}

public class UserThemePreferenceDto
{
    public string ThemeName { get; set; } = "modern-dark";
    public string? CustomPrimaryColor { get; set; }
    public string? CustomAccentColor { get; set; }
}

public class ModuleAccessDto
{
    public string ModuleId { get; set; } = null!;
    public List<string> Permissions { get; set; } = new();
}

public class UpdateUserRolesRequest
{
    public List<Guid> RoleIds { get; set; } = new();

    [Required, MaxLength(500)]
    public string Reason { get; set; } = null!;
}

public class MonthlyPeriodStatusDto
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }
    public string? Notes { get; set; }
}
