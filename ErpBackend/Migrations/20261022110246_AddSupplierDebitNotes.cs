using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierDebitNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierDebitNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DebitNoteNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AccountsPayableId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    IqcReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReturnedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseNetAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseTaxAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseTotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    InventoryAllocation = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CogsAllocation = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseInventoryAllocation = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseCogsAllocation = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PostedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierDebitNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierDebitNotes_AccountsPayables_AccountsPayableId",
                        column: x => x.AccountsPayableId,
                        principalTable: "AccountsPayables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierDebitNotes_GoodsReceiptItems_GoodsReceiptItemId",
                        column: x => x.GoodsReceiptItemId,
                        principalTable: "GoodsReceiptItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierDebitNotes_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierDebitNotes_IqcReports_IqcReportId",
                        column: x => x.IqcReportId,
                        principalTable: "IqcReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierDebitNotes_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierDebitNotes_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierDebitNoteApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierDebitNoteId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_SupplierDebitNoteApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierDebitNoteApprovals_SupplierDebitNotes_SupplierDebit~",
                        column: x => x.SupplierDebitNoteId,
                        principalTable: "SupplierDebitNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_RelatedDocType_RelatedDocId",
                table: "InventoryTransactions",
                columns: new[] { "RelatedDocType", "RelatedDocId" },
                unique: true,
                filter: "\"RelatedDocType\" = 'SupplierDebitNote'");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDebitNoteApprovals_SupplierDebitNoteId_ApprovalRole",
                table: "SupplierDebitNoteApprovals",
                columns: new[] { "SupplierDebitNoteId", "ApprovalRole" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDebitNoteApprovals_SupplierDebitNoteId_ApproverId",
                table: "SupplierDebitNoteApprovals",
                columns: new[] { "SupplierDebitNoteId", "ApproverId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDebitNotes_AccountsPayableId",
                table: "SupplierDebitNotes",
                column: "AccountsPayableId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDebitNotes_DebitNoteNumber",
                table: "SupplierDebitNotes",
                column: "DebitNoteNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDebitNotes_GoodsReceiptId",
                table: "SupplierDebitNotes",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDebitNotes_GoodsReceiptItemId",
                table: "SupplierDebitNotes",
                column: "GoodsReceiptItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDebitNotes_IdempotencyKey",
                table: "SupplierDebitNotes",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDebitNotes_IqcReportId",
                table: "SupplierDebitNotes",
                column: "IqcReportId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDebitNotes_SkuId",
                table: "SupplierDebitNotes",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDebitNotes_SupplierId",
                table: "SupplierDebitNotes",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierDebitNoteApprovals");

            migrationBuilder.DropTable(
                name: "SupplierDebitNotes");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_RelatedDocType_RelatedDocId",
                table: "InventoryTransactions");
        }
    }
}
