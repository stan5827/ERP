using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddFqcProductionReceiptWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReceivedQty",
                table: "WorkOrders",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FinishedLotNumber",
                table: "WipReports",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FqcInspectionLots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductSkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProducedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    SampledQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DefectiveQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ApprovedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CriticalDefectFound = table.Column<bool>(type: "boolean", nullable: false),
                    InspectionStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DispositionStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DefectCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InspectorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InspectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecisionBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QuarantineWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    QuarantineBinLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    QuarantinedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    QuarantinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FqcInspectionLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FqcInspectionLots_BinLocations_QuarantineBinLocationId",
                        column: x => x.QuarantineBinLocationId,
                        principalTable: "BinLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FqcInspectionLots_Skus_ProductSkuId",
                        column: x => x.ProductSkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FqcInspectionLots_Warehouses_QuarantineWarehouseId",
                        column: x => x.QuarantineWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FqcInspectionLots_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionReceipts_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FqcInspectionFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FqcInspectionLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    FindingName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DefectCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DefectQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    IsCritical = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FqcInspectionFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FqcInspectionFindings_FqcInspectionLots_FqcInspectionLotId",
                        column: x => x.FqcInspectionLotId,
                        principalTable: "FqcInspectionLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionReceiptItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    FqcInspectionLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    BinLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionReceiptItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionReceiptItems_BinLocations_BinLocationId",
                        column: x => x.BinLocationId,
                        principalTable: "BinLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionReceiptItems_FqcInspectionLots_FqcInspectionLotId",
                        column: x => x.FqcInspectionLotId,
                        principalTable: "FqcInspectionLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionReceiptItems_ProductionReceipts_ProductionReceipt~",
                        column: x => x.ProductionReceiptId,
                        principalTable: "ProductionReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionReceiptItems_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionReceiptItems_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FqcInspectionFindings_FqcInspectionLotId",
                table: "FqcInspectionFindings",
                column: "FqcInspectionLotId");

            migrationBuilder.CreateIndex(
                name: "IX_FqcInspectionLots_InspectionNumber",
                table: "FqcInspectionLots",
                column: "InspectionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FqcInspectionLots_ProductSkuId",
                table: "FqcInspectionLots",
                column: "ProductSkuId");

            migrationBuilder.CreateIndex(
                name: "IX_FqcInspectionLots_QuarantineBinLocationId",
                table: "FqcInspectionLots",
                column: "QuarantineBinLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_FqcInspectionLots_QuarantineWarehouseId",
                table: "FqcInspectionLots",
                column: "QuarantineWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_FqcInspectionLots_WorkOrderId_LotNumber",
                table: "FqcInspectionLots",
                columns: new[] { "WorkOrderId", "LotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionReceiptItems_BinLocationId",
                table: "ProductionReceiptItems",
                column: "BinLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionReceiptItems_FqcInspectionLotId",
                table: "ProductionReceiptItems",
                column: "FqcInspectionLotId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionReceiptItems_ProductionReceiptId_FqcInspectionLot~",
                table: "ProductionReceiptItems",
                columns: new[] { "ProductionReceiptId", "FqcInspectionLotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionReceiptItems_SkuId",
                table: "ProductionReceiptItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionReceiptItems_WarehouseId",
                table: "ProductionReceiptItems",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionReceipts_ReceiptNumber",
                table: "ProductionReceipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionReceipts_WorkOrderId",
                table: "ProductionReceipts",
                column: "WorkOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FqcInspectionFindings");

            migrationBuilder.DropTable(
                name: "ProductionReceiptItems");

            migrationBuilder.DropTable(
                name: "FqcInspectionLots");

            migrationBuilder.DropTable(
                name: "ProductionReceipts");

            migrationBuilder.DropColumn(
                name: "ReceivedQty",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "FinishedLotNumber",
                table: "WipReports");
        }
    }
}
