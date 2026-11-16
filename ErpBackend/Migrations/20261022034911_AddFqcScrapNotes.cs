using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddFqcScrapNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DispositionAt",
                table: "ScrapRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispositionBy",
                table: "ScrapRecords",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FqcInspectionLotId",
                table: "ScrapRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScrapNumber",
                table: "ScrapRecords",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScrapStatus",
                table: "ScrapRecords",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScrapRecords_FqcInspectionLotId",
                table: "ScrapRecords",
                column: "FqcInspectionLotId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ScrapRecords_FqcInspectionLots_FqcInspectionLotId",
                table: "ScrapRecords",
                column: "FqcInspectionLotId",
                principalTable: "FqcInspectionLots",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScrapRecords_FqcInspectionLots_FqcInspectionLotId",
                table: "ScrapRecords");

            migrationBuilder.DropIndex(
                name: "IX_ScrapRecords_FqcInspectionLotId",
                table: "ScrapRecords");

            migrationBuilder.DropColumn(
                name: "DispositionAt",
                table: "ScrapRecords");

            migrationBuilder.DropColumn(
                name: "DispositionBy",
                table: "ScrapRecords");

            migrationBuilder.DropColumn(
                name: "FqcInspectionLotId",
                table: "ScrapRecords");

            migrationBuilder.DropColumn(
                name: "ScrapNumber",
                table: "ScrapRecords");

            migrationBuilder.DropColumn(
                name: "ScrapStatus",
                table: "ScrapRecords");
        }
    }
}
