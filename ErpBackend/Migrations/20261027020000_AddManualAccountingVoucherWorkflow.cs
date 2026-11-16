using ErpBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations;

[DbContext(typeof(ErpGeneralDbContext))]
[Migration("20261027020000_AddManualAccountingVoucherWorkflow")]
public partial class AddManualAccountingVoucherWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "LineNumber",
            table: "AccountingEntries",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<DateTime>(
            name: "SubmittedAt",
            table: "AccountingVouchers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "SubmittedBy",
            table: "AccountingVouchers",
            type: "uuid",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SubmittedAt",
            table: "AccountingVouchers");

        migrationBuilder.DropColumn(
            name: "SubmittedBy",
            table: "AccountingVouchers");

        migrationBuilder.DropColumn(
            name: "LineNumber",
            table: "AccountingEntries");
    }
}
