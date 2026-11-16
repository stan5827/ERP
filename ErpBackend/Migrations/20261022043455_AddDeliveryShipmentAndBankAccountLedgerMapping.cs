using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryShipmentAndBankAccountLedgerMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "DeliveryOrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GLAccountId",
                table: "BankAccounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrderItems_BinLocationId",
                table: "DeliveryOrderItems",
                column: "BinLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrderItems_WarehouseId",
                table: "DeliveryOrderItems",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_CashReceipts_BankAccountId",
                table: "CashReceipts",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_GLAccountId",
                table: "BankAccounts",
                column: "GLAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_BankAccounts_GLAccounts_GLAccountId",
                table: "BankAccounts",
                column: "GLAccountId",
                principalTable: "GLAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashReceipts_BankAccounts_BankAccountId",
                table: "CashReceipts",
                column: "BankAccountId",
                principalTable: "BankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryOrderItems_BinLocations_BinLocationId",
                table: "DeliveryOrderItems",
                column: "BinLocationId",
                principalTable: "BinLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryOrderItems_Warehouses_WarehouseId",
                table: "DeliveryOrderItems",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankAccounts_GLAccounts_GLAccountId",
                table: "BankAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_CashReceipts_BankAccounts_BankAccountId",
                table: "CashReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryOrderItems_BinLocations_BinLocationId",
                table: "DeliveryOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryOrderItems_Warehouses_WarehouseId",
                table: "DeliveryOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryOrderItems_BinLocationId",
                table: "DeliveryOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryOrderItems_WarehouseId",
                table: "DeliveryOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_CashReceipts_BankAccountId",
                table: "CashReceipts");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_GLAccountId",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "DeliveryOrderItems");

            migrationBuilder.DropColumn(
                name: "GLAccountId",
                table: "BankAccounts");
        }
    }
}
