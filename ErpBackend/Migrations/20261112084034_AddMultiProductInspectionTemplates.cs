using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiProductInspectionTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InspectionTemplateSkus",
                columns: table => new
                {
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductSkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionTemplateSkus", x => new { x.TemplateId, x.ProductSkuId });
                    table.ForeignKey(
                        name: "FK_InspectionTemplateSkus_InspectionTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "InspectionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InspectionTemplateSkus_Skus_ProductSkuId",
                        column: x => x.ProductSkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspectionTemplateSkus_ProductSkuId",
                table: "InspectionTemplateSkus",
                column: "ProductSkuId");

            migrationBuilder.Sql(
                @"INSERT INTO ""InspectionTemplateSkus"" (""TemplateId"", ""ProductSkuId"")
                  SELECT ""Id"", ""ProductSkuId""
                  FROM ""InspectionTemplates""
                  WHERE ""ProductSkuId"" IS NOT NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_InspectionTemplates_Skus_ProductSkuId",
                table: "InspectionTemplates");

            migrationBuilder.DropIndex(
                name: "IX_InspectionTemplates_ProductSkuId",
                table: "InspectionTemplates");

            migrationBuilder.DropColumn(
                name: "ProductSkuId",
                table: "InspectionTemplates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductSkuId",
                table: "InspectionTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE ""InspectionTemplates"" AS template
                  SET ""ProductSkuId"" = (
                      SELECT sku.""ProductSkuId""
                      FROM ""InspectionTemplateSkus"" AS sku
                      WHERE sku.""TemplateId"" = template.""Id""
                      ORDER BY sku.""ProductSkuId""
                      LIMIT 1);");

            migrationBuilder.DropTable(
                name: "InspectionTemplateSkus");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionTemplates_ProductSkuId",
                table: "InspectionTemplates",
                column: "ProductSkuId");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionTemplates_Skus_ProductSkuId",
                table: "InspectionTemplates",
                column: "ProductSkuId",
                principalTable: "Skus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
