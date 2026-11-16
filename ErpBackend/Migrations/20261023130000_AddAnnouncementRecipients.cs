using ErpBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations;

[DbContext(typeof(ErpGeneralDbContext))]
[Migration("20261023130000_AddAnnouncementRecipients")]
public partial class AddAnnouncementRecipients : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AnnouncementRecipients",
            columns: table => new
            {
                AnnouncementId = table.Column<Guid>(type: "uuid", nullable: false),
                EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AnnouncementRecipients", recipient => new { recipient.AnnouncementId, recipient.EmployeeId });
                table.ForeignKey(
                    name: "FK_AnnouncementRecipients_Announcements_AnnouncementId",
                    column: recipient => recipient.AnnouncementId,
                    principalTable: "Announcements",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AnnouncementRecipients_Employees_EmployeeId",
                    column: recipient => recipient.EmployeeId,
                    principalTable: "Employees",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AnnouncementRecipients_EmployeeId",
            table: "AnnouncementRecipients",
            column: "EmployeeId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AnnouncementRecipients");
    }
}
