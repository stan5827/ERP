using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddFqcScrapApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestedByName",
                table: "ScrapRecords",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedByUserId",
                table: "ScrapRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScrapReasonCode",
                table: "ScrapRecords",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "ScrapRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReworkSourceInspectionLotId",
                table: "FqcInspectionLots",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FqcScrapApprovalEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScrapRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FqcScrapApprovalEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FqcScrapApprovalEvents_ScrapRecords_ScrapRecordId",
                        column: x => x.ScrapRecordId,
                        principalTable: "ScrapRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FqcInspectionLots_ReworkSourceInspectionLotId",
                table: "FqcInspectionLots",
                column: "ReworkSourceInspectionLotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FqcScrapApprovalEvents_ScrapRecordId_OccurredAt",
                table: "FqcScrapApprovalEvents",
                columns: new[] { "ScrapRecordId", "OccurredAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_FqcInspectionLots_FqcInspectionLots_ReworkSourceInspectionL~",
                table: "FqcInspectionLots",
                column: "ReworkSourceInspectionLotId",
                principalTable: "FqcInspectionLots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FqcInspectionLots_FqcInspectionLots_ReworkSourceInspectionL~",
                table: "FqcInspectionLots");

            migrationBuilder.DropTable(
                name: "FqcScrapApprovalEvents");

            migrationBuilder.DropIndex(
                name: "IX_FqcInspectionLots_ReworkSourceInspectionLotId",
                table: "FqcInspectionLots");

            migrationBuilder.DropColumn(
                name: "RequestedByName",
                table: "ScrapRecords");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "ScrapRecords");

            migrationBuilder.DropColumn(
                name: "ScrapReasonCode",
                table: "ScrapRecords");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "ScrapRecords");

            migrationBuilder.DropColumn(
                name: "ReworkSourceInspectionLotId",
                table: "FqcInspectionLots");
        }
    }
}
