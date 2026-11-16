using ErpBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    [DbContext(typeof(ErpGeneralDbContext))]
    [Migration("20261021100000_RestoreBomMaterialUnits")]
    public partial class RestoreBomMaterialUnits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "WorkOrderMaterials" AS wom
                SET "Unit" = bom_item."Unit"
                FROM "WorkOrders" AS wo,
                (
                    SELECT "BomId", "ChildSkuId", MIN("Unit") AS "Unit"
                    FROM "BomItems"
                    WHERE "Unit" IS NOT NULL
                    GROUP BY "BomId", "ChildSkuId"
                    HAVING COUNT(DISTINCT "Unit") = 1
                ) AS bom_item
                WHERE wo."Id" = wom."WorkOrderId"
                  AND bom_item."BomId" = wo."BomId"
                  AND bom_item."ChildSkuId" = wom."SkuId"
                  AND wom."Unit" IS DISTINCT FROM bom_item."Unit";

                UPDATE "MaterialIssueItems" AS item
                SET "Unit" = wom."Unit"
                FROM "WorkOrderMaterials" AS wom, "MaterialIssues" AS issue
                WHERE item."WorkOrderMaterialId" = wom."Id"
                  AND item."Unit" IS DISTINCT FROM wom."Unit"
                  AND issue."Id" = item."IssueId"
                  AND issue."Status" = 'draft'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "MaterialIssueItemAllocations" AS allocation
                      WHERE allocation."IssueItemId" = item."Id"
                  );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "WorkOrderMaterials" AS wom
                SET "Unit" = sku."Unit"
                FROM "Skus" AS sku
                WHERE wom."SkuId" = sku."Id";

                UPDATE "MaterialIssueItems" AS item
                SET "Unit" = sku."Unit"
                FROM "Skus" AS sku, "MaterialIssues" AS issue
                WHERE item."SkuId" = sku."Id"
                  AND issue."Id" = item."IssueId"
                  AND issue."Status" = 'draft'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "MaterialIssueItemAllocations" AS allocation
                      WHERE allocation."IssueItemId" = item."Id"
                  );
                """);
        }
    }
}
