using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierApSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierApSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedBySupplierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CurrentRevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAccountsPayableId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdempotencyRequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SupplierInvoiceNumberKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierApSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissions_AccountsPayables_GeneratedAccountsPay~",
                        column: x => x.GeneratedAccountsPayableId,
                        principalTable: "AccountsPayables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissions_SupplierUsers_SubmittedBySupplierUser~",
                        column: x => x.SubmittedBySupplierUserId,
                        principalTable: "SupplierUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissions_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierApSubmissionRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    SupplierInvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SupplierInvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    InvoiceAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedBySupplierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierApSubmissionRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissionRevisions_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissionRevisions_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissionRevisions_SupplierApSubmissions_Submiss~",
                        column: x => x.SubmissionId,
                        principalTable: "SupplierApSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissionRevisions_SupplierUsers_SubmittedBySupp~",
                        column: x => x.SubmittedBySupplierUserId,
                        principalTable: "SupplierUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierApSubmissionAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedBySupplierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ScanStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScanResult = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ScannedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierApSubmissionAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissionAttachments_SupplierApSubmissionRevisio~",
                        column: x => x.RevisionId,
                        principalTable: "SupplierApSubmissionRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissionAttachments_SupplierApSubmissions_Submi~",
                        column: x => x.SubmissionId,
                        principalTable: "SupplierApSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissionAttachments_SupplierUsers_UploadedBySup~",
                        column: x => x.UploadedBySupplierUserId,
                        principalTable: "SupplierUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierApSubmissionReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReviewerRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MatchResult = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MatchRuleVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MatchSnapshot = table.Column<string>(type: "jsonb", nullable: true),
                    AccountsPayableId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierApSubmissionReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissionReviews_AccountsPayables_AccountsPayabl~",
                        column: x => x.AccountsPayableId,
                        principalTable: "AccountsPayables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissionReviews_SupplierApSubmissionRevisions_R~",
                        column: x => x.RevisionId,
                        principalTable: "SupplierApSubmissionRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissionReviews_SupplierApSubmissions_Submissio~",
                        column: x => x.SubmissionId,
                        principalTable: "SupplierApSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissionReviews_Users_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierApSubmissionRevisionLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoicedQuantity = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierApSubmissionRevisionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissionRevisionLines_GoodsReceiptItems_GoodsRe~",
                        column: x => x.GoodsReceiptItemId,
                        principalTable: "GoodsReceiptItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissionRevisionLines_PurchaseOrderItems_Purcha~",
                        column: x => x.PurchaseOrderItemId,
                        principalTable: "PurchaseOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierApSubmissionRevisionLines_SupplierApSubmissionRevis~",
                        column: x => x.RevisionId,
                        principalTable: "SupplierApSubmissionRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissionAttachments_RevisionId_ScanStatus",
                table: "SupplierApSubmissionAttachments",
                columns: new[] { "RevisionId", "ScanStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissionAttachments_SubmissionId_RevokedAt",
                table: "SupplierApSubmissionAttachments",
                columns: new[] { "SubmissionId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissionAttachments_UploadedBySupplierUserId",
                table: "SupplierApSubmissionAttachments",
                column: "UploadedBySupplierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissionReviews_AccountsPayableId",
                table: "SupplierApSubmissionReviews",
                column: "AccountsPayableId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissionReviews_ReviewerUserId",
                table: "SupplierApSubmissionReviews",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissionReviews_RevisionId",
                table: "SupplierApSubmissionReviews",
                column: "RevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissionReviews_SubmissionId_CreatedAt",
                table: "SupplierApSubmissionReviews",
                columns: new[] { "SubmissionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissionRevisionLines_GoodsReceiptItemId",
                table: "SupplierApSubmissionRevisionLines",
                column: "GoodsReceiptItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissionRevisionLines_PurchaseOrderItemId",
                table: "SupplierApSubmissionRevisionLines",
                column: "PurchaseOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissionRevisionLines_RevisionId_GoodsReceiptIt~",
                table: "SupplierApSubmissionRevisionLines",
                columns: new[] { "RevisionId", "GoodsReceiptItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissionRevisions_GoodsReceiptId",
                table: "SupplierApSubmissionRevisions",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissionRevisions_PurchaseOrderId",
                table: "SupplierApSubmissionRevisions",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissionRevisions_SubmissionId_RevisionNumber",
                table: "SupplierApSubmissionRevisions",
                columns: new[] { "SubmissionId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissionRevisions_SubmittedBySupplierUserId",
                table: "SupplierApSubmissionRevisions",
                column: "SubmittedBySupplierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissions_GeneratedAccountsPayableId",
                table: "SupplierApSubmissions",
                column: "GeneratedAccountsPayableId",
                unique: true,
                filter: "\"GeneratedAccountsPayableId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissions_SubmissionNumber",
                table: "SupplierApSubmissions",
                column: "SubmissionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissions_SubmittedBySupplierUserId",
                table: "SupplierApSubmissions",
                column: "SubmittedBySupplierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissions_SupplierId_IdempotencyKey",
                table: "SupplierApSubmissions",
                columns: new[] { "SupplierId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissions_SupplierId_Status_CreatedAt",
                table: "SupplierApSubmissions",
                columns: new[] { "SupplierId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierApSubmissions_SupplierId_SupplierInvoiceNumberKey",
                table: "SupplierApSubmissions",
                columns: new[] { "SupplierId", "SupplierInvoiceNumberKey" },
                unique: true,
                filter: "\"SupplierInvoiceNumberKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierApSubmissionAttachments");

            migrationBuilder.DropTable(
                name: "SupplierApSubmissionReviews");

            migrationBuilder.DropTable(
                name: "SupplierApSubmissionRevisionLines");

            migrationBuilder.DropTable(
                name: "SupplierApSubmissionRevisions");

            migrationBuilder.DropTable(
                name: "SupplierApSubmissions");
        }
    }
}
