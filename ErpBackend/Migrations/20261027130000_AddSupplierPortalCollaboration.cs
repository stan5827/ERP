using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations;

public partial class AddSupplierPortalCollaboration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "PromisedDeliveryDate",
            table: "RfqSupplierResponses",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "SubmittedAt",
            table: "RfqSupplierResponses",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "CURRENT_TIMESTAMP");

        migrationBuilder.CreateTable(
            name: "RfqSupplierInvitations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RfqId = table.Column<Guid>(type: "uuid", nullable: false),
                SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                InvitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RfqSupplierInvitations", row => row.Id);
                table.ForeignKey(
                    name: "FK_RfqSupplierInvitations_Rfqs_RfqId",
                    column: row => row.RfqId,
                    principalTable: "Rfqs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_RfqSupplierInvitations_Suppliers_SupplierId",
                    column: row => row.SupplierId,
                    principalTable: "Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PurchaseOrderSupplierResponses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                SupplierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                ResponseType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                PurchaseOrderVersion = table.Column<int>(type: "integer", nullable: false),
                SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PurchaseOrderSupplierResponses", row => row.Id);
                table.ForeignKey(
                    name: "FK_PurchaseOrderSupplierResponses_PurchaseOrders_PurchaseOrderId",
                    column: row => row.PurchaseOrderId,
                    principalTable: "PurchaseOrders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PurchaseOrderSupplierResponses_Suppliers_SupplierId",
                    column: row => row.SupplierId,
                    principalTable: "Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PurchaseOrderSupplierResponseItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SupplierResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                PurchaseOrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                ProposedQuantity = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                ProposedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PurchaseOrderSupplierResponseItems", row => row.Id);
                table.ForeignKey(
                    name: "FK_PurchaseOrderSupplierResponseItems_PurchaseOrderItems_PurchaseOrderItemId",
                    column: row => row.PurchaseOrderItemId,
                    principalTable: "PurchaseOrderItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PurchaseOrderSupplierResponseItems_PurchaseOrderSupplierResponses_SupplierResponseId",
                    column: row => row.SupplierResponseId,
                    principalTable: "PurchaseOrderSupplierResponses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RfqSupplierInvitations_RfqId_SupplierId",
            table: "RfqSupplierInvitations",
            columns: new[] { "RfqId", "SupplierId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RfqSupplierInvitations_SupplierId",
            table: "RfqSupplierInvitations",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseOrderSupplierResponses_PurchaseOrderId_SubmittedAt",
            table: "PurchaseOrderSupplierResponses",
            columns: new[] { "PurchaseOrderId", "SubmittedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseOrderSupplierResponses_SupplierId",
            table: "PurchaseOrderSupplierResponses",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseOrderSupplierResponseItems_PurchaseOrderItemId",
            table: "PurchaseOrderSupplierResponseItems",
            column: "PurchaseOrderItemId");

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseOrderSupplierResponseItems_SupplierResponseId",
            table: "PurchaseOrderSupplierResponseItems",
            column: "SupplierResponseId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RfqSupplierInvitations");
        migrationBuilder.DropTable(name: "PurchaseOrderSupplierResponseItems");
        migrationBuilder.DropTable(name: "PurchaseOrderSupplierResponses");

        migrationBuilder.DropColumn(
            name: "PromisedDeliveryDate",
            table: "RfqSupplierResponses");

        migrationBuilder.DropColumn(
            name: "SubmittedAt",
            table: "RfqSupplierResponses");
    }
}
