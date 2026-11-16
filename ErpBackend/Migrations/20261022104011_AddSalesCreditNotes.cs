using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesCreditNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesCreditNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditNoteNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SalesInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseTotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ReceivableAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CustomerAdvanceAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
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
                    table.PrimaryKey("PK_SalesCreditNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesCreditNotes_DeliveryOrders_DeliveryOrderId",
                        column: x => x.DeliveryOrderId,
                        principalTable: "DeliveryOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesCreditNotes_SalesInvoices_SalesInvoiceId",
                        column: x => x.SalesInvoiceId,
                        principalTable: "SalesInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesCreditNoteApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesCreditNoteId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_SalesCreditNoteApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesCreditNoteApprovals_SalesCreditNotes_SalesCreditNoteId",
                        column: x => x.SalesCreditNoteId,
                        principalTable: "SalesCreditNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNoteApprovals_SalesCreditNoteId_ApprovalRole",
                table: "SalesCreditNoteApprovals",
                columns: new[] { "SalesCreditNoteId", "ApprovalRole" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNoteApprovals_SalesCreditNoteId_ApproverId",
                table: "SalesCreditNoteApprovals",
                columns: new[] { "SalesCreditNoteId", "ApproverId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNotes_CreditNoteNumber",
                table: "SalesCreditNotes",
                column: "CreditNoteNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNotes_DeliveryOrderId",
                table: "SalesCreditNotes",
                column: "DeliveryOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNotes_IdempotencyKey",
                table: "SalesCreditNotes",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNotes_SalesInvoiceId",
                table: "SalesCreditNotes",
                column: "SalesInvoiceId");

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION erp_prevent_posted_sales_credit_note_mutation()
                RETURNS trigger AS $$
                BEGIN
                    IF UPPER(COALESCE(OLD."Status", '')) = 'POSTED' THEN
                        RAISE EXCEPTION 'Posted sales credit notes are immutable'
                            USING ERRCODE = '55000';
                    END IF;
                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER "TR_SalesCreditNotes_ImmutablePosted"
                BEFORE UPDATE OR DELETE ON "SalesCreditNotes"
                FOR EACH ROW EXECUTE FUNCTION erp_prevent_posted_sales_credit_note_mutation();

                CREATE OR REPLACE FUNCTION erp_protect_sales_credit_note_approval()
                RETURNS trigger AS $$
                DECLARE
                    note_status text;
                BEGIN
                    IF TG_OP = 'INSERT' THEN
                        SELECT "Status" INTO note_status
                        FROM "SalesCreditNotes"
                        WHERE "Id" = NEW."SalesCreditNoteId"
                        FOR UPDATE;
                        IF UPPER(COALESCE(note_status, '')) = 'POSTED' THEN
                            RAISE EXCEPTION 'Cannot approve a posted sales credit note'
                                USING ERRCODE = '55000';
                        END IF;
                        RETURN NEW;
                    END IF;
                    RAISE EXCEPTION 'Sales credit note approvals are append-only'
                        USING ERRCODE = '55000';
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER "TR_SalesCreditNoteApprovals_Immutable"
                BEFORE INSERT OR UPDATE OR DELETE ON "SalesCreditNoteApprovals"
                FOR EACH ROW EXECUTE FUNCTION erp_protect_sales_credit_note_approval();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_SalesCreditNoteApprovals_Immutable" ON "SalesCreditNoteApprovals";
                DROP TRIGGER IF EXISTS "TR_SalesCreditNotes_ImmutablePosted" ON "SalesCreditNotes";
                DROP FUNCTION IF EXISTS erp_protect_sales_credit_note_approval();
                DROP FUNCTION IF EXISTS erp_prevent_posted_sales_credit_note_mutation();
                """);

            migrationBuilder.DropTable(
                name: "SalesCreditNoteApprovals");

            migrationBuilder.DropTable(
                name: "SalesCreditNotes");
        }
    }
}
