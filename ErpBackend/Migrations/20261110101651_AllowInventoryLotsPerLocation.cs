using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AllowInventoryLotsPerLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryLots_SkuId_LotNumber",
                table: "InventoryLots");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_SkuId_LotNumber_WarehouseId_BinLocationId",
                table: "InventoryLots",
                columns: new[] { "SkuId", "LotNumber", "WarehouseId", "BinLocationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryLots_SkuId_LotNumber_WarehouseId_BinLocationId",
                table: "InventoryLots");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_SkuId_LotNumber",
                table: "InventoryLots",
                columns: new[] { "SkuId", "LotNumber" },
                unique: true);
        }
    }
}
