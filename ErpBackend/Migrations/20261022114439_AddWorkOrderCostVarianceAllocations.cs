using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderCostVarianceAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AbnormalCost",
                table: "work_order_costs",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CapitalizedActualCost",
                table: "work_order_costs",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                "UPDATE work_order_costs SET \"CapitalizedActualCost\" = \"TotalActualCost\" WHERE lower(\"Status\") = 'settled';");

            migrationBuilder.CreateTable(
                name: "work_order_cost_variance_allocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_cost_variance_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_order_cost_variance_allocations_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_order_cost_variance_allocation_approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_cost_variance_allocation_approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_order_cost_variance_allocation_approvals_work_order_co~",
                        column: x => x.AllocationId,
                        principalTable: "work_order_cost_variance_allocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_work_order_cost_variance_allocation_approvals_AllocationId_~",
                table: "work_order_cost_variance_allocation_approvals",
                columns: new[] { "AllocationId", "ApprovalRole" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_order_cost_variance_allocation_approvals_AllocationId~1",
                table: "work_order_cost_variance_allocation_approvals",
                columns: new[] { "AllocationId", "ApproverId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_order_cost_variance_allocations_SourceType_SourceId",
                table: "work_order_cost_variance_allocations",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_work_order_cost_variance_allocations_WorkOrderId_Status",
                table: "work_order_cost_variance_allocations",
                columns: new[] { "WorkOrderId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_order_cost_variance_allocation_approvals");

            migrationBuilder.DropTable(
                name: "work_order_cost_variance_allocations");

            migrationBuilder.DropColumn(
                name: "AbnormalCost",
                table: "work_order_costs");

            migrationBuilder.DropColumn(
                name: "CapitalizedActualCost",
                table: "work_order_costs");
        }
    }
}
