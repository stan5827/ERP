using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddManufacturingCostAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AreaSquareMeters",
                table: "CostCenters",
                type: "numeric(14,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkOrderId",
                table: "AccountingEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_WorkOrderId",
                table: "AccountingEntries",
                column: "WorkOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountingEntries_WorkOrders_WorkOrderId",
                table: "AccountingEntries",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountingEntries_WorkOrders_WorkOrderId",
                table: "AccountingEntries");

            migrationBuilder.DropIndex(
                name: "IX_AccountingEntries_WorkOrderId",
                table: "AccountingEntries");

            migrationBuilder.DropColumn(
                name: "AreaSquareMeters",
                table: "CostCenters");

            migrationBuilder.DropColumn(
                name: "WorkOrderId",
                table: "AccountingEntries");
        }
    }
}
