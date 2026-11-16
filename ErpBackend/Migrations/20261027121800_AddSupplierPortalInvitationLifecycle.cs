using ErpBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations;

[DbContext(typeof(ErpGeneralDbContext))]
[Migration("20261027121800_AddSupplierPortalInvitationLifecycle")]
public partial class AddSupplierPortalInvitationLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "InvitationAttemptCount",
            table: "SupplierUsers",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<Guid>(
            name: "InvitationCorrelationId",
            table: "SupplierUsers",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "InvitationExpiresAt",
            table: "SupplierUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "InvitationLastError",
            table: "SupplierUsers",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "InvitationSentAt",
            table: "SupplierUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PortalInvitationStatus",
            table: "SupplierUsers",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "NotInvited");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierUsers_InvitationCorrelationId",
            table: "SupplierUsers",
            column: "InvitationCorrelationId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SupplierUsers_InvitationCorrelationId",
            table: "SupplierUsers");

        migrationBuilder.DropColumn(
            name: "InvitationAttemptCount",
            table: "SupplierUsers");

        migrationBuilder.DropColumn(
            name: "InvitationCorrelationId",
            table: "SupplierUsers");

        migrationBuilder.DropColumn(
            name: "InvitationExpiresAt",
            table: "SupplierUsers");

        migrationBuilder.DropColumn(
            name: "InvitationLastError",
            table: "SupplierUsers");

        migrationBuilder.DropColumn(
            name: "InvitationSentAt",
            table: "SupplierUsers");

        migrationBuilder.DropColumn(
            name: "PortalInvitationStatus",
            table: "SupplierUsers");
    }
}
