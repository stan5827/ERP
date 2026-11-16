-- Read-only Phase 0 preflight for employee/account and RBAC integrity.
-- Run the complete script against the intended PostgreSQL database.
-- Output contains account/employee GUIDs for reconciliation; restrict access.
-- No usernames, email addresses, credentials, or employee personal fields are selected.

BEGIN TRANSACTION READ ONLY;

-- Duplicate employee links that would conflict with a future unique constraint.
SELECT
    'duplicate_employee_id' AS issue_code,
    u."EmployeeId" AS employee_id,
    COUNT(*) AS affected_account_count,
    ARRAY_AGG(u."Id"::text ORDER BY u."Id") AS user_ids
FROM "Users" AS u
WHERE u."EmployeeId" IS NOT NULL
GROUP BY u."EmployeeId"
HAVING COUNT(*) > 1
ORDER BY u."EmployeeId";

-- User rows linked to a missing employee.
SELECT
    'orphan_employee_link' AS issue_code,
    u."Id" AS user_id,
    u."EmployeeId" AS employee_id
FROM "Users" AS u
LEFT JOIN "Employees" AS e ON e."Id" = u."EmployeeId"
WHERE u."EmployeeId" IS NOT NULL
  AND e."Id" IS NULL
ORDER BY u."Id";

-- Accounts without an employee link; these are for review, not automatic deletion.
SELECT
    'account_without_employee' AS issue_code,
    u."Id" AS user_id,
    u."IsActive" AS account_is_active
FROM "Users" AS u
WHERE u."EmployeeId" IS NULL
ORDER BY u."Id";

-- Users with no role assignment.
SELECT
    'user_without_role' AS issue_code,
    u."Id" AS user_id,
    u."IsActive" AS account_is_active
FROM "Users" AS u
WHERE NOT EXISTS (
    SELECT 1
    FROM "UserRoles" AS ur
    WHERE ur."UserId" = u."Id"
)
ORDER BY u."Id";

-- Orphaned UserRole references (normally prevented by foreign keys).
SELECT
    'orphan_user_role_user' AS issue_code,
    ur."Id" AS user_role_id,
    ur."UserId" AS user_id,
    ur."RoleId" AS role_id
FROM "UserRoles" AS ur
LEFT JOIN "Users" AS u ON u."Id" = ur."UserId"
WHERE u."Id" IS NULL
ORDER BY ur."Id";

SELECT
    'orphan_user_role_role' AS issue_code,
    ur."Id" AS user_role_id,
    ur."UserId" AS user_id,
    ur."RoleId" AS role_id
FROM "UserRoles" AS ur
LEFT JOIN "Roles" AS r ON r."Id" = ur."RoleId"
WHERE r."Id" IS NULL
ORDER BY ur."Id";

-- Orphaned RolePermission references (normally prevented by foreign keys).
SELECT
    'orphan_role_permission_role' AS issue_code,
    rp."Id" AS role_permission_id,
    rp."RoleId" AS role_id,
    rp."PermissionId" AS permission_id
FROM "RolePermissions" AS rp
LEFT JOIN "Roles" AS r ON r."Id" = rp."RoleId"
WHERE r."Id" IS NULL
ORDER BY rp."Id";

SELECT
    'orphan_role_permission_permission' AS issue_code,
    rp."Id" AS role_permission_id,
    rp."RoleId" AS role_id,
    rp."PermissionId" AS permission_id
FROM "RolePermissions" AS rp
LEFT JOIN "Permissions" AS p ON p."Id" = rp."PermissionId"
WHERE p."Id" IS NULL
ORDER BY rp."Id";

-- Orphaned parent links in the permission tree.
SELECT
    'orphan_permission_parent' AS issue_code,
    p."Id" AS permission_id,
    p."ParentId" AS parent_permission_id
FROM "Permissions" AS p
LEFT JOIN "Permissions" AS parent ON parent."Id" = p."ParentId"
WHERE p."ParentId" IS NOT NULL
  AND parent."Id" IS NULL
ORDER BY p."Id";

-- Status and account-state distributions; these contain counts only.
SELECT
    'employee_status' AS metric,
    e."Status" AS status,
    COUNT(*) AS row_count
FROM "Employees" AS e
GROUP BY e."Status"
ORDER BY e."Status";

SELECT
    'account_active_state' AS metric,
    u."IsActive"::text AS state,
    COUNT(*) AS row_count
FROM "Users" AS u
GROUP BY u."IsActive"
ORDER BY u."IsActive";

SELECT
    'linked_employee_account_state' AS metric,
    e."Status" AS employee_status,
    u."IsActive" AS account_is_active,
    COUNT(*) AS row_count
FROM "Users" AS u
JOIN "Employees" AS e ON e."Id" = u."EmployeeId"
GROUP BY e."Status", u."IsActive"
ORDER BY e."Status", u."IsActive";

ROLLBACK;
