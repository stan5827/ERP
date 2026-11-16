using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class ArchiveLegacyRolesAndSeedCoreRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE "Roles"
                SET "IsActive" = "RoleName" IN ('Admin', 'HR Manager', 'Production Supervisor', 'Operator', 'Finance');
                """);

            migrationBuilder.Sql("""
                INSERT INTO "Roles" ("Id", "RoleName", "Description", "IsSystem", "CreatedAt", "IsActive")
                VALUES
                    ('00000000-0000-4000-8000-000000000001', 'Admin', 'Full system administration and cross-module emergency maintenance.', TRUE, CURRENT_TIMESTAMP, TRUE),
                    ('00000000-0000-4000-8000-000000000002', 'HR Manager', 'Employee records, attendance, leave approval, and payroll.', TRUE, CURRENT_TIMESTAMP, TRUE),
                    ('00000000-0000-4000-8000-000000000003', 'Production Supervisor', 'Work-order dispatch, production reporting review, BOM status approval, and line monitoring.', TRUE, CURRENT_TIMESTAMP, TRUE),
                    ('00000000-0000-4000-8000-000000000004', 'Operator', 'Personal and team production reporting and employee self-service.', TRUE, CURRENT_TIMESTAMP, TRUE),
                    ('00000000-0000-4000-8000-000000000005', 'Finance', 'Accounts receivable/payable, actual costing, and general-ledger inquiry.', TRUE, CURRENT_TIMESTAMP, TRUE)
                ON CONFLICT ("RoleName") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Preserve role rows, assignments, and system flags; rollback removes only the schema flag.
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Roles");
        }
    }
}
