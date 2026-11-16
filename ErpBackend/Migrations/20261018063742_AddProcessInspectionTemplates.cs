using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessInspectionTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "InspectionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TemplateName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductSkuId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionTemplates_Operations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InspectionTemplates_Skus_ProductSkuId",
                        column: x => x.ProductSkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InspectionTemplateItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    InputType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Specification = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    LowerLimit = table.Column<decimal>(type: "numeric(20,6)", nullable: true),
                    UpperLimit = table.Column<decimal>(type: "numeric(20,6)", nullable: true),
                    OptionsJson = table.Column<string>(type: "text", nullable: true),
                    InspectionMethod = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Instrument = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SamplingPlan = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsCritical = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionTemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionTemplateItems_InspectionTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "InspectionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessInspectionReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationSeq = table.Column<int>(type: "integer", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateVersion = table.Column<int>(type: "integer", nullable: false),
                    ProductSkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InspectorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InspectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SampleQuantity = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessInspectionReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessInspectionReports_InspectionTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "InspectionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessInspectionReports_Skus_ProductSkuId",
                        column: x => x.ProductSkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessInspectionReports_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcessInspectionReportItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    InputType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Specification = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    LowerLimit = table.Column<decimal>(type: "numeric(20,6)", nullable: true),
                    UpperLimit = table.Column<decimal>(type: "numeric(20,6)", nullable: true),
                    InspectionMethod = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Instrument = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Value = table.Column<string>(type: "text", nullable: true),
                    Result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessInspectionReportItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessInspectionReportItems_InspectionTemplateItems_Templa~",
                        column: x => x.TemplateItemId,
                        principalTable: "InspectionTemplateItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProcessInspectionReportItems_ProcessInspectionReports_Repor~",
                        column: x => x.ReportId,
                        principalTable: "ProcessInspectionReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspectionTemplateItems_TemplateId",
                table: "InspectionTemplateItems",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionTemplates_OperationId",
                table: "InspectionTemplates",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionTemplates_ProductSkuId",
                table: "InspectionTemplates",
                column: "ProductSkuId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionTemplates_TemplateCode_Version",
                table: "InspectionTemplates",
                columns: new[] { "TemplateCode", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessInspectionReportItems_ReportId",
                table: "ProcessInspectionReportItems",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessInspectionReportItems_TemplateItemId",
                table: "ProcessInspectionReportItems",
                column: "TemplateItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessInspectionReports_ProductSkuId",
                table: "ProcessInspectionReports",
                column: "ProductSkuId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessInspectionReports_ReportNumber",
                table: "ProcessInspectionReports",
                column: "ReportNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessInspectionReports_TemplateId",
                table: "ProcessInspectionReports",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessInspectionReports_WorkOrderId_OperationSeq_LotNumber",
                table: "ProcessInspectionReports",
                columns: new[] { "WorkOrderId", "OperationSeq", "LotNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessInspectionReportItems");

            migrationBuilder.DropTable(
                name: "InspectionTemplateItems");

            migrationBuilder.DropTable(
                name: "ProcessInspectionReports");

            migrationBuilder.DropTable(
                name: "InspectionTemplates");

    }
    }
    }
