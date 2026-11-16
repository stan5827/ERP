using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class EnforceCostCenterAreaNonNegative : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_CostCenters_AreaSquareMeters_NonNegative",
                table: "CostCenters",
                sql: "\"AreaSquareMeters\" IS NULL OR \"AreaSquareMeters\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CostCenters_AreaSquareMeters_NonNegative",
                table: "CostCenters");
        }
    }
}
