using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddFqcReworkDispositionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DispositionAt",
                table: "FqcInspectionLots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispositionBy",
                table: "FqcInspectionLots",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReworkWorkOrderId",
                table: "FqcInspectionLots",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DispositionAt",
                table: "FqcInspectionLots");

            migrationBuilder.DropColumn(
                name: "DispositionBy",
                table: "FqcInspectionLots");

            migrationBuilder.DropColumn(
                name: "ReworkWorkOrderId",
                table: "FqcInspectionLots");
        }
    }
}
