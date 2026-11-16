using ErpBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations;

[DbContext(typeof(ErpGeneralDbContext))]
[Migration("20261103120000_AddAgentWipDisclosureLog")]
public partial class AddAgentWipDisclosureLog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DisclosureLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                TraceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ToolName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                PolicyVersion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                DataClassification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                DisclosedFields = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                DecisionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
            },
            constraints: table => table.PrimaryKey("PK_DisclosureLogs", log => log.Id));

        migrationBuilder.CreateIndex(
            name: "IX_DisclosureLogs_ActorUserId_CreatedAt",
            table: "DisclosureLogs",
            columns: new[] { "ActorUserId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_DisclosureLogs_TraceId",
            table: "DisclosureLogs",
            column: "TraceId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DisclosureLogs");
    }
}
