using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierCapaReviewAndAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierCapaAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierCapaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierCapaResponseRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_SupplierCapaAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierCapaAttachments_SupplierCapaResponseRevisions_Suppl~",
                        column: x => x.SupplierCapaResponseRevisionId,
                        principalTable: "SupplierCapaResponseRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierCapaAttachments_SupplierCapas_SupplierCapaId",
                        column: x => x.SupplierCapaId,
                        principalTable: "SupplierCapas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierCapaReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierCapaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierCapaResponseRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierCapaReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierCapaReviews_SupplierCapaResponseRevisions_SupplierC~",
                        column: x => x.SupplierCapaResponseRevisionId,
                        principalTable: "SupplierCapaResponseRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierCapaReviews_SupplierCapas_SupplierCapaId",
                        column: x => x.SupplierCapaId,
                        principalTable: "SupplierCapas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCapaAttachments_SupplierCapaId_RevokedAt",
                table: "SupplierCapaAttachments",
                columns: new[] { "SupplierCapaId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCapaAttachments_SupplierCapaResponseRevisionId",
                table: "SupplierCapaAttachments",
                column: "SupplierCapaResponseRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCapaReviews_SupplierCapaId_SupplierCapaResponseRevi~",
                table: "SupplierCapaReviews",
                columns: new[] { "SupplierCapaId", "SupplierCapaResponseRevisionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCapaReviews_SupplierCapaResponseRevisionId",
                table: "SupplierCapaReviews",
                column: "SupplierCapaResponseRevisionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierCapaAttachments");

            migrationBuilder.DropTable(
                name: "SupplierCapaReviews");
        }
    }
}
