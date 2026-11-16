using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddWipReportAccountability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EquipmentUsageRecordId",
                table: "work_order_machine_actuals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "WipReports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldDefaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "ProxyReason",
                table: "WipReports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportType",
                table: "WipReports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReviewComments",
                table: "WipReports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "WipReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "WipReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "WipReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByUserId",
                table: "WipReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "WipReports"
                SET "Status" = 'approved',
                    "ReportType" = 'individual',
                    "SubmittedAt" = "CreatedAt",
                    "SubmittedByUserId" = (
                        SELECT "Id"
                        FROM "Users"
                        WHERE "Users"."EmployeeId" = "WipReports"."EmployeeId"
                        LIMIT 1
                    );
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "WipReports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ReportType",
                table: "WipReports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "");

            migrationBuilder.CreateTable(
                name: "EquipmentUsageRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationSeq = table.Column<int>(type: "integer", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftClass = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MachineHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentUsageRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentUsageRecords_Employees_RecordedByEmployeeId",
                        column: x => x.RecordedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentUsageRecords_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentUsageRecords_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WipReportAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WipReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ChangesJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WipReportAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WipReportAuditEvents_WipReports_WipReportId",
                        column: x => x.WipReportId,
                        principalTable: "WipReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WipReportParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WipReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WipReportParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WipReportParticipants_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WipReportParticipants_WipReports_WipReportId",
                        column: x => x.WipReportId,
                        principalTable: "WipReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_work_order_machine_actuals_EquipmentUsageRecordId",
                table: "work_order_machine_actuals",
                column: "EquipmentUsageRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUsageRecords_EquipmentId",
                table: "EquipmentUsageRecords",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUsageRecords_RecordedByEmployeeId",
                table: "EquipmentUsageRecords",
                column: "RecordedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUsageRecords_WorkOrderId_OperationSeq_EquipmentId_~",
                table: "EquipmentUsageRecords",
                columns: new[] { "WorkOrderId", "OperationSeq", "EquipmentId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_WipReportAuditEvents_WipReportId",
                table: "WipReportAuditEvents",
                column: "WipReportId");

            migrationBuilder.CreateIndex(
                name: "IX_WipReportParticipants_EmployeeId",
                table: "WipReportParticipants",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WipReportParticipants_WipReportId_EmployeeId",
                table: "WipReportParticipants",
                columns: new[] { "WipReportId", "EmployeeId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_work_order_machine_actuals_EquipmentUsageRecords_EquipmentU~",
                table: "work_order_machine_actuals",
                column: "EquipmentUsageRecordId",
                principalTable: "EquipmentUsageRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_work_order_machine_actuals_EquipmentUsageRecords_EquipmentU~",
                table: "work_order_machine_actuals");

            migrationBuilder.DropTable(
                name: "EquipmentUsageRecords");

            migrationBuilder.DropTable(
                name: "WipReportAuditEvents");

            migrationBuilder.DropTable(
                name: "WipReportParticipants");

            migrationBuilder.DropIndex(
                name: "IX_work_order_machine_actuals_EquipmentUsageRecordId",
                table: "work_order_machine_actuals");

            migrationBuilder.DropColumn(
                name: "EquipmentUsageRecordId",
                table: "work_order_machine_actuals");

            migrationBuilder.DropColumn(
                name: "ProxyReason",
                table: "WipReports");

            migrationBuilder.DropColumn(
                name: "ReportType",
                table: "WipReports");

            migrationBuilder.DropColumn(
                name: "ReviewComments",
                table: "WipReports");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "WipReports");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "WipReports");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "WipReports");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "WipReports");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "WipReports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
