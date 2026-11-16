using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderCostStandardBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "StandardLaborCost",
                table: "work_order_costs",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardMaterialCost",
                table: "work_order_costs",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardOverheadCost",
                table: "work_order_costs",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StandardLaborCost",
                table: "work_order_costs");

            migrationBuilder.DropColumn(
                name: "StandardMaterialCost",
                table: "work_order_costs");

            migrationBuilder.DropColumn(
                name: "StandardOverheadCost",
                table: "work_order_costs");
        }
    }
}
