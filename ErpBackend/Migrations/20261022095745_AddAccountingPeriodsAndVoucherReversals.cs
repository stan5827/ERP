using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingPeriodsAndVoucherReversals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReversalOfVoucherId",
                table: "AccountingVouchers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountingPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPeriods", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingVouchers_ReversalOfVoucherId",
                table: "AccountingVouchers",
                column: "ReversalOfVoucherId",
                unique: true,
                filter: "\"ReversalOfVoucherId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriods_PeriodCode",
                table: "AccountingPeriods",
                column: "PeriodCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountingVouchers_AccountingVouchers_ReversalOfVoucherId",
                table: "AccountingVouchers",
                column: "ReversalOfVoucherId",
                principalTable: "AccountingVouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION erp_prevent_posted_voucher_mutation()
                RETURNS trigger AS $$
                BEGIN
                    IF UPPER(OLD."Status") = 'POSTED' THEN
                        RAISE EXCEPTION 'Posted accounting vouchers are immutable; create a reversal voucher'
                            USING ERRCODE = '55000';
                    END IF;
                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER "TR_AccountingVouchers_ImmutablePosted"
                BEFORE UPDATE OR DELETE ON "AccountingVouchers"
                FOR EACH ROW EXECUTE FUNCTION erp_prevent_posted_voucher_mutation();

                CREATE OR REPLACE FUNCTION erp_prevent_posted_entry_mutation()
                RETURNS trigger AS $$
                DECLARE
                    old_voucher_status text;
                    new_voucher_status text;
                BEGIN
                    IF TG_OP <> 'INSERT' THEN
                        SELECT "Status" INTO old_voucher_status
                        FROM "AccountingVouchers"
                        WHERE "Id" = OLD."VoucherId"
                        FOR UPDATE;
                    END IF;
                    IF TG_OP <> 'DELETE' THEN
                        IF TG_OP = 'UPDATE' AND NEW."VoucherId" = OLD."VoucherId" THEN
                            new_voucher_status := old_voucher_status;
                        ELSE
                            SELECT "Status" INTO new_voucher_status
                            FROM "AccountingVouchers"
                            WHERE "Id" = NEW."VoucherId"
                            FOR UPDATE;
                        END IF;
                    END IF;

                    IF UPPER(COALESCE(old_voucher_status, '')) = 'POSTED'
                       OR UPPER(COALESCE(new_voucher_status, '')) = 'POSTED' THEN
                        RAISE EXCEPTION 'Entries of posted accounting vouchers are immutable; create a reversal voucher'
                            USING ERRCODE = '55000';
                    END IF;
                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER "TR_AccountingEntries_ImmutablePosted"
                BEFORE INSERT OR UPDATE OR DELETE ON "AccountingEntries"
                FOR EACH ROW EXECUTE FUNCTION erp_prevent_posted_entry_mutation();

                CREATE OR REPLACE FUNCTION erp_validate_accounting_voucher_period()
                RETURNS trigger AS $$
                DECLARE
                    period_closed boolean;
                    source_is_posted boolean;
                    source_is_reversal boolean;
                BEGIN
                    IF UPPER(NEW."Status") = 'POSTED' THEN
                        SELECT "IsClosed" INTO period_closed
                        FROM "AccountingPeriods"
                        WHERE "StartDate" <= (NEW."VoucherDate" AT TIME ZONE 'UTC')::date
                          AND "EndDate" >= (NEW."VoucherDate" AT TIME ZONE 'UTC')::date
                        FOR UPDATE;

                        IF COALESCE(period_closed, false) THEN
                            RAISE EXCEPTION 'Cannot post a voucher into a closed accounting period'
                                USING ERRCODE = '55000';
                        END IF;

                        IF (
                            SELECT COUNT(*) FROM "AccountingEntries"
                            WHERE "VoucherId" = NEW."Id"
                        ) < 2 OR EXISTS (
                            SELECT 1 FROM "AccountingEntries"
                            WHERE "VoucherId" = NEW."Id"
                              AND ("DebitAmount" < 0 OR "CreditAmount" < 0
                                   OR (("DebitAmount" > 0) = ("CreditAmount" > 0)))
                        ) OR ROUND(COALESCE((
                            SELECT SUM("DebitAmount") FROM "AccountingEntries"
                            WHERE "VoucherId" = NEW."Id"
                        ), 0), 2) <> ROUND(COALESCE((
                            SELECT SUM("CreditAmount") FROM "AccountingEntries"
                            WHERE "VoucherId" = NEW."Id"
                        ), 0), 2) THEN
                            RAISE EXCEPTION 'Posted accounting vouchers must have valid balanced entries'
                                USING ERRCODE = '23514';
                        END IF;
                    END IF;

                    IF NEW."ReversalOfVoucherId" IS NOT NULL THEN
                        SELECT UPPER("Status") = 'POSTED', "ReversalOfVoucherId" IS NOT NULL
                        INTO source_is_posted, source_is_reversal
                        FROM "AccountingVouchers"
                        WHERE "Id" = NEW."ReversalOfVoucherId"
                        FOR UPDATE;

                        IF NOT COALESCE(source_is_posted, false)
                           OR COALESCE(source_is_reversal, false)
                           OR UPPER(NEW."Status") <> 'POSTED' THEN
                            RAISE EXCEPTION 'A reversal must reference an unreversed posted voucher and be posted'
                                USING ERRCODE = '55000';
                        END IF;
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER "TR_AccountingVouchers_ValidatePosting"
                BEFORE INSERT OR UPDATE ON "AccountingVouchers"
                FOR EACH ROW EXECUTE FUNCTION erp_validate_accounting_voucher_period();

                CREATE OR REPLACE FUNCTION erp_protect_closed_accounting_period()
                RETURNS trigger AS $$
                BEGIN
                    IF OLD."IsClosed" THEN
                        RAISE EXCEPTION 'Closed accounting periods are immutable'
                            USING ERRCODE = '55000';
                    END IF;
                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER "TR_AccountingPeriods_ImmutableClosed"
                BEFORE UPDATE OR DELETE ON "AccountingPeriods"
                FOR EACH ROW EXECUTE FUNCTION erp_protect_closed_accounting_period();

                CREATE OR REPLACE FUNCTION erp_prevent_overlapping_accounting_periods()
                RETURNS trigger AS $$
                BEGIN
                    PERFORM pg_advisory_xact_lock(710240621);
                    IF EXISTS (
                        SELECT 1 FROM "AccountingPeriods"
                        WHERE "Id" <> NEW."Id"
                          AND "StartDate" <= NEW."EndDate"
                          AND "EndDate" >= NEW."StartDate"
                    ) THEN
                        RAISE EXCEPTION 'Accounting period date ranges cannot overlap'
                            USING ERRCODE = '23P01';
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER "TR_AccountingPeriods_NoOverlap"
                BEFORE INSERT OR UPDATE ON "AccountingPeriods"
                FOR EACH ROW EXECUTE FUNCTION erp_prevent_overlapping_accounting_periods();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_AccountingPeriods_NoOverlap" ON "AccountingPeriods";
                DROP TRIGGER IF EXISTS "TR_AccountingPeriods_ImmutableClosed" ON "AccountingPeriods";
                DROP TRIGGER IF EXISTS "TR_AccountingVouchers_ValidatePosting" ON "AccountingVouchers";
                DROP TRIGGER IF EXISTS "TR_AccountingEntries_ImmutablePosted" ON "AccountingEntries";
                DROP TRIGGER IF EXISTS "TR_AccountingVouchers_ImmutablePosted" ON "AccountingVouchers";
                DROP FUNCTION IF EXISTS erp_prevent_overlapping_accounting_periods();
                DROP FUNCTION IF EXISTS erp_protect_closed_accounting_period();
                DROP FUNCTION IF EXISTS erp_validate_accounting_voucher_period();
                DROP FUNCTION IF EXISTS erp_prevent_posted_entry_mutation();
                DROP FUNCTION IF EXISTS erp_prevent_posted_voucher_mutation();
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_AccountingVouchers_AccountingVouchers_ReversalOfVoucherId",
                table: "AccountingVouchers");

            migrationBuilder.DropTable(
                name: "AccountingPeriods");

            migrationBuilder.DropIndex(
                name: "IX_AccountingVouchers_ReversalOfVoucherId",
                table: "AccountingVouchers");

            migrationBuilder.DropColumn(
                name: "ReversalOfVoucherId",
                table: "AccountingVouchers");
        }
    }
}
