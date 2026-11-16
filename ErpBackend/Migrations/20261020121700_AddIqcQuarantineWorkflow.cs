using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddIqcQuarantineWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "InspectorId",
                table: "IqcReports",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "GoodsReceiptItemId",
                table: "IqcReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReceivedQty",
                table: "IqcReports",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseBinLocationId",
                table: "IqcReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SkuId",
                table: "IqcReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LowerLimit",
                table: "IqcReportItems",
                type: "numeric(14,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UpperLimit",
                table: "IqcReportItems",
                type: "numeric(14,4)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IqcReports_GoodsReceiptId",
                table: "IqcReports",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_IqcReports_GoodsReceiptItemId",
                table: "IqcReports",
                column: "GoodsReceiptItemId",
                unique: true,
                filter: "\"GoodsReceiptItemId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IqcReports_ReleaseBinLocationId",
                table: "IqcReports",
                column: "ReleaseBinLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_IqcReports_SkuId",
                table: "IqcReports",
                column: "SkuId");

            migrationBuilder.AddForeignKey(
                name: "FK_IqcReports_BinLocations_ReleaseBinLocationId",
                table: "IqcReports",
                column: "ReleaseBinLocationId",
                principalTable: "BinLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IqcReports_GoodsReceiptItems_GoodsReceiptItemId",
                table: "IqcReports",
                column: "GoodsReceiptItemId",
                principalTable: "GoodsReceiptItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IqcReports_GoodsReceipts_GoodsReceiptId",
                table: "IqcReports",
                column: "GoodsReceiptId",
                principalTable: "GoodsReceipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IqcReports_Skus_SkuId",
                table: "IqcReports",
                column: "SkuId",
                principalTable: "Skus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IqcReports_BinLocations_ReleaseBinLocationId",
                table: "IqcReports");

            migrationBuilder.DropForeignKey(
                name: "FK_IqcReports_GoodsReceiptItems_GoodsReceiptItemId",
                table: "IqcReports");

            migrationBuilder.DropForeignKey(
                name: "FK_IqcReports_GoodsReceipts_GoodsReceiptId",
                table: "IqcReports");

            migrationBuilder.DropForeignKey(
                name: "FK_IqcReports_Skus_SkuId",
                table: "IqcReports");

            migrationBuilder.DropIndex(
                name: "IX_IqcReports_GoodsReceiptId",
                table: "IqcReports");

            migrationBuilder.DropIndex(
                name: "IX_IqcReports_GoodsReceiptItemId",
                table: "IqcReports");

            migrationBuilder.DropIndex(
                name: "IX_IqcReports_ReleaseBinLocationId",
                table: "IqcReports");

            migrationBuilder.DropIndex(
                name: "IX_IqcReports_SkuId",
                table: "IqcReports");

            migrationBuilder.DropColumn(
                name: "GoodsReceiptItemId",
                table: "IqcReports");

            migrationBuilder.DropColumn(
                name: "ReceivedQty",
                table: "IqcReports");

            migrationBuilder.DropColumn(
                name: "ReleaseBinLocationId",
                table: "IqcReports");

            migrationBuilder.DropColumn(
                name: "SkuId",
                table: "IqcReports");

            migrationBuilder.DropColumn(
                name: "LowerLimit",
                table: "IqcReportItems");

            migrationBuilder.DropColumn(
                name: "UpperLimit",
                table: "IqcReportItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "InspectorId",
                table: "IqcReports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
