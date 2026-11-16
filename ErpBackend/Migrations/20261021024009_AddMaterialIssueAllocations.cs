using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialIssueAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ClosedShortageQty",
                table: "WorkOrderMaterials",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "WorkOrderMaterials",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BomVersion",
                table: "MaterialIssues",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "MaterialIssues",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReversalOfIssueId",
                table: "MaterialIssues",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ClosedShortageQty",
                table: "MaterialIssueItems",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ShortageClosureReason",
                table: "MaterialIssueItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "MaterialIssueItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkOrderMaterialId",
                table: "MaterialIssueItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaterialIssueItemAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    BinLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IssuedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    InventoryTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialIssueItemAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialIssueItemAllocations_BinLocations_BinLocationId",
                        column: x => x.BinLocationId,
                        principalTable: "BinLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialIssueItemAllocations_InventoryTransactions_Inventor~",
                        column: x => x.InventoryTransactionId,
                        principalTable: "InventoryTransactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialIssueItemAllocations_MaterialIssueItems_IssueItemId",
                        column: x => x.IssueItemId,
                        principalTable: "MaterialIssueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialIssueItemAllocations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssues_IssueWarehouseId",
                table: "MaterialIssues",
                column: "IssueWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssues_ReversalOfIssueId",
                table: "MaterialIssues",
                column: "ReversalOfIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssueItems_SkuId",
                table: "MaterialIssueItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssueItems_WorkOrderMaterialId",
                table: "MaterialIssueItems",
                column: "WorkOrderMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssueItemAllocations_BinLocationId",
                table: "MaterialIssueItemAllocations",
                column: "BinLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssueItemAllocations_InventoryTransactionId",
                table: "MaterialIssueItemAllocations",
                column: "InventoryTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssueItemAllocations_IssueItemId",
                table: "MaterialIssueItemAllocations",
                column: "IssueItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssueItemAllocations_WarehouseId",
                table: "MaterialIssueItemAllocations",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialIssueItems_Skus_SkuId",
                table: "MaterialIssueItems",
                column: "SkuId",
                principalTable: "Skus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialIssueItems_WorkOrderMaterials_WorkOrderMaterialId",
                table: "MaterialIssueItems",
                column: "WorkOrderMaterialId",
                principalTable: "WorkOrderMaterials",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialIssues_MaterialIssues_ReversalOfIssueId",
                table: "MaterialIssues",
                column: "ReversalOfIssueId",
                principalTable: "MaterialIssues",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialIssues_Warehouses_IssueWarehouseId",
                table: "MaterialIssues",
                column: "IssueWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                UPDATE "WorkOrderMaterials" AS wom
                SET "Unit" = sku."Unit"
                FROM "Skus" AS sku
                WHERE wom."SkuId" = sku."Id"
                  AND wom."Unit" IS NULL;

                WITH ledger_balance AS (
                    SELECT "SkuId", SUM("QtyChange") AS "Qty"
                    FROM "InventoryTransactions"
                    GROUP BY "SkuId"
                ),
                adjustments AS (
                    SELECT sku."Id" AS "SkuId",
                           sku."Qty" - COALESCE(balance."Qty", 0) AS "QtyChange",
                           COALESCE(
                               sku."DefaultWarehouseId",
                               (SELECT warehouse."Id"
                                FROM "Warehouses" AS warehouse
                                WHERE warehouse."IsActive" = TRUE
                                ORDER BY warehouse."WarehouseCode"
                                LIMIT 1)
                           ) AS "WarehouseId",
                           sku."DefaultBinLocationId" AS "DefaultBinLocationId",
                           sku."StandardCost" AS "UnitCost",
                           sku."Qty" AS "QtyAfter"
                    FROM "Skus" AS sku
                    LEFT JOIN ledger_balance AS balance ON balance."SkuId" = sku."Id"
                    WHERE sku."Qty" <> COALESCE(balance."Qty", 0)
                )
                INSERT INTO "InventoryTransactions"
                    ("Id", "TrxId", "Type", "SkuId", "QtyChange", "QtyBefore", "QtyAfter",
                     "WarehouseId", "BinLocationId", "RelatedDocType", "RelatedDocId",
                     "UnitCost", "TotalCost", "Notes", "CreatedAt")
                SELECT gen_random_uuid(),
                       'OPEN-' || substr(replace(adjustments."SkuId"::text, '-', ''), 1, 25),
                       'opening_balance',
                       adjustments."SkuId",
                       adjustments."QtyChange",
                       adjustments."QtyAfter" - adjustments."QtyChange",
                       adjustments."QtyAfter",
                       adjustments."WarehouseId",
                       CASE
                           WHEN bin."WarehouseId" = adjustments."WarehouseId"
                           THEN adjustments."DefaultBinLocationId"
                           ELSE NULL
                       END,
                       'SkuLedgerOpeningBalance',
                       adjustments."SkuId",
                       adjustments."UnitCost",
                       adjustments."QtyChange" * adjustments."UnitCost",
                       'Migration reconciliation of SKU balance to inventory ledger',
                       CURRENT_TIMESTAMP
                FROM adjustments
                LEFT JOIN "BinLocations" AS bin
                    ON bin."Id" = adjustments."DefaultBinLocationId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialIssueItems_Skus_SkuId",
                table: "MaterialIssueItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialIssueItems_WorkOrderMaterials_WorkOrderMaterialId",
                table: "MaterialIssueItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialIssues_MaterialIssues_ReversalOfIssueId",
                table: "MaterialIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialIssues_Warehouses_IssueWarehouseId",
                table: "MaterialIssues");

            migrationBuilder.DropTable(
                name: "MaterialIssueItemAllocations");

            migrationBuilder.DropIndex(
                name: "IX_MaterialIssues_IssueWarehouseId",
                table: "MaterialIssues");

            migrationBuilder.DropIndex(
                name: "IX_MaterialIssues_ReversalOfIssueId",
                table: "MaterialIssues");

            migrationBuilder.DropIndex(
                name: "IX_MaterialIssueItems_SkuId",
                table: "MaterialIssueItems");

            migrationBuilder.DropIndex(
                name: "IX_MaterialIssueItems_WorkOrderMaterialId",
                table: "MaterialIssueItems");

            migrationBuilder.DropColumn(
                name: "ClosedShortageQty",
                table: "WorkOrderMaterials");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "WorkOrderMaterials");

            migrationBuilder.DropColumn(
                name: "BomVersion",
                table: "MaterialIssues");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "MaterialIssues");

            migrationBuilder.DropColumn(
                name: "ReversalOfIssueId",
                table: "MaterialIssues");

            migrationBuilder.DropColumn(
                name: "ClosedShortageQty",
                table: "MaterialIssueItems");

            migrationBuilder.DropColumn(
                name: "ShortageClosureReason",
                table: "MaterialIssueItems");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "MaterialIssueItems");

            migrationBuilder.DropColumn(
                name: "WorkOrderMaterialId",
                table: "MaterialIssueItems");
        }
    }
}
