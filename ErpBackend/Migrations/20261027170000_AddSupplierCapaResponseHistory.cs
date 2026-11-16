using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierCapaResponseHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierCapaResponseRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierCapaId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    SupplierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    D1_Team = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    D2_ProblemDescription = table.Column<string>(type: "text", nullable: true),
                    D3_InterimContainment = table.Column<string>(type: "text", nullable: true),
                    D4_RootCause = table.Column<string>(type: "text", nullable: true),
                    D5_PermanentAction = table.Column<string>(type: "text", nullable: true),
                    D6_Verification = table.Column<string>(type: "text", nullable: true),
                    D7_Prevention = table.Column<string>(type: "text", nullable: true),
                    D8_Closure = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierCapaResponseRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierCapaResponseRevisions_SupplierCapas_SupplierCapaId",
                        column: x => x.SupplierCapaId,
                        principalTable: "SupplierCapas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCapaResponseRevisions_SupplierCapaId_RevisionNumber",
                table: "SupplierCapaResponseRevisions",
                columns: new[] { "SupplierCapaId", "RevisionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierCapaResponseRevisions");
        }
    }
}
