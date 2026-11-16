using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ErpBackend.Data;

#nullable disable

namespace ErpBackend.Migrations;

[DbContext(typeof(ErpGeneralDbContext))]
[Migration("20261020090251_AddInventoryTransactionReversalLink")]
public partial class AddInventoryTransactionReversalLink : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ReversalOfTransactionId",
            table: "InventoryTransactions",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_InventoryTransactions_ReversalOfTransactionId",
            table: "InventoryTransactions",
            column: "ReversalOfTransactionId",
            unique: true,
            filter: "\"ReversalOfTransactionId\" IS NOT NULL");

        migrationBuilder.AddForeignKey(
            name: "FK_InventoryTransactions_InventoryTransactions_ReversalOfTransactionId",
            table: "InventoryTransactions",
            column: "ReversalOfTransactionId",
            principalTable: "InventoryTransactions",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_InventoryTransactions_InventoryTransactions_ReversalOfTransactionId",
            table: "InventoryTransactions");

        migrationBuilder.DropIndex(
            name: "IX_InventoryTransactions_ReversalOfTransactionId",
            table: "InventoryTransactions");

        migrationBuilder.DropColumn(
            name: "ReversalOfTransactionId",
            table: "InventoryTransactions");
    }
}
