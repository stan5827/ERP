using ErpBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations;

[DbContext(typeof(ErpGeneralDbContext))]
[Migration("20261027121100_AddSupplierIdentityProviderSubject")]
public partial class AddSupplierIdentityProviderSubject : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "IdentityProviderSubject",
            table: "SupplierUsers",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_SupplierUsers_IdentityProviderSubject",
            table: "SupplierUsers",
            column: "IdentityProviderSubject",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SupplierUsers_IdentityProviderSubject",
            table: "SupplierUsers");

        migrationBuilder.DropColumn(
            name: "IdentityProviderSubject",
            table: "SupplierUsers");
    }
}
