using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingVoucherPostingPurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PostingPurpose",
                table: "AccountingVouchers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingVouchers_ReferenceType_ReferenceId_PostingPurpose",
                table: "AccountingVouchers",
                columns: new[] { "ReferenceType", "ReferenceId", "PostingPurpose" },
                unique: true,
                filter: "\"ReferenceType\" IS NOT NULL AND \"ReferenceId\" IS NOT NULL AND \"PostingPurpose\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountingVouchers_ReferenceType_ReferenceId_PostingPurpose",
                table: "AccountingVouchers");

            migrationBuilder.DropColumn(
                name: "PostingPurpose",
                table: "AccountingVouchers");
        }
    }
}
