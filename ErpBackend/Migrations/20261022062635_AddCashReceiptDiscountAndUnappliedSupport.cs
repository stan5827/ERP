using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCashReceiptDiscountAndUnappliedSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountedAmount",
                table: "SalesInvoices",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "CashReceipts",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "TWD");

            migrationBuilder.Sql(
                """
                UPDATE "CashReceipts" AS receipt
                SET "Currency" = COALESCE(bank_account."Currency", 'TWD')
                FROM "BankAccounts" AS bank_account
                WHERE receipt."BankAccountId" = bank_account."Id";
                """);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "CashReceiptAllocations",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountedAmount",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "CashReceipts");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "CashReceiptAllocations");
        }
    }
}
