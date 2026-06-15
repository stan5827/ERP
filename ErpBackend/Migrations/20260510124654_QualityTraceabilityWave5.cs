using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class QualityTraceabilityWave5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "SupplierComplaints",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "SupplierComplaints",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosureReason",
                table: "SupplierComplaints",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsiblePerson",
                table: "SupplierComplaints",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "SupplierCapas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "SupplierCapas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosureReason",
                table: "SupplierCapas",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsiblePerson",
                table: "SupplierCapas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "NonConformanceReports",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "NonConformanceReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosureReason",
                table: "NonConformanceReports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsiblePerson",
                table: "NonConformanceReports",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QaReviewLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResponsiblePerson = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AttachmentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaReviewLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountsPayables_GoodsReceiptId",
                table: "AccountsPayables",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountsPayables_PurchaseOrderId",
                table: "AccountsPayables",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountsPayables_SupplierId",
                table: "AccountsPayables",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountsPayableItems_SkuId",
                table: "AccountsPayableItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_QaReviewLogs_TargetType_TargetId_CreatedAt",
                table: "QaReviewLogs",
                columns: new[] { "TargetType", "TargetId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_AccountsPayableItems_Skus_SkuId",
                table: "AccountsPayableItems",
                column: "SkuId",
                principalTable: "Skus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountsPayables_GoodsReceipts_GoodsReceiptId",
                table: "AccountsPayables",
                column: "GoodsReceiptId",
                principalTable: "GoodsReceipts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountsPayables_PurchaseOrders_PurchaseOrderId",
                table: "AccountsPayables",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountsPayables_Suppliers_SupplierId",
                table: "AccountsPayables",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountsPayableItems_Skus_SkuId",
                table: "AccountsPayableItems");

            migrationBuilder.DropForeignKey(
                name: "FK_AccountsPayables_GoodsReceipts_GoodsReceiptId",
                table: "AccountsPayables");

            migrationBuilder.DropForeignKey(
                name: "FK_AccountsPayables_PurchaseOrders_PurchaseOrderId",
                table: "AccountsPayables");

            migrationBuilder.DropForeignKey(
                name: "FK_AccountsPayables_Suppliers_SupplierId",
                table: "AccountsPayables");

            migrationBuilder.DropTable(
                name: "QaReviewLogs");

            migrationBuilder.DropIndex(
                name: "IX_AccountsPayables_GoodsReceiptId",
                table: "AccountsPayables");

            migrationBuilder.DropIndex(
                name: "IX_AccountsPayables_PurchaseOrderId",
                table: "AccountsPayables");

            migrationBuilder.DropIndex(
                name: "IX_AccountsPayables_SupplierId",
                table: "AccountsPayables");

            migrationBuilder.DropIndex(
                name: "IX_AccountsPayableItems_SkuId",
                table: "AccountsPayableItems");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "SupplierComplaints");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "SupplierComplaints");

            migrationBuilder.DropColumn(
                name: "ClosureReason",
                table: "SupplierComplaints");

            migrationBuilder.DropColumn(
                name: "ResponsiblePerson",
                table: "SupplierComplaints");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "SupplierCapas");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "SupplierCapas");

            migrationBuilder.DropColumn(
                name: "ClosureReason",
                table: "SupplierCapas");

            migrationBuilder.DropColumn(
                name: "ResponsiblePerson",
                table: "SupplierCapas");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "NonConformanceReports");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "NonConformanceReports");

            migrationBuilder.DropColumn(
                name: "ClosureReason",
                table: "NonConformanceReports");

            migrationBuilder.DropColumn(
                name: "ResponsiblePerson",
                table: "NonConformanceReports");
        }
    }
}
