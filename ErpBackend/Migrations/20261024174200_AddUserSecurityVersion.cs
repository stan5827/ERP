using ErpBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations;

[DbContext(typeof(ErpGeneralDbContext))]
[Migration("20261024174200_AddUserSecurityVersion")]
public partial class AddUserSecurityVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "SecurityVersion",
            table: "Users",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SecurityVersion",
            table: "Users");
    }
}
