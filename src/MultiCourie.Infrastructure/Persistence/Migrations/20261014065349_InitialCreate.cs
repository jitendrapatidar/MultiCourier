using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiCourie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bulk_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SucceededCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bulk_batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "shipment_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CourierPartner = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CourierOrderId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AwbNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CourierRequestPayload = table.Column<string>(type: "TEXT", nullable: true),
                    CourierResponsePayload = table.Column<string>(type: "TEXT", nullable: true),
                    LastErrorCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bulk_batch_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BulkBatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ShipmentOrderId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bulk_batch_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bulk_batch_items_bulk_batches_BulkBatchId",
                        column: x => x.BulkBatchId,
                        principalTable: "bulk_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tracking_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShipmentOrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CourierStatusText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RawPayload = table.Column<string>(type: "TEXT", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracking_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tracking_history_shipment_orders_ShipmentOrderId",
                        column: x => x.ShipmentOrderId,
                        principalTable: "shipment_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bulk_batch_items_BulkBatchId_OrderId",
                table: "bulk_batch_items",
                columns: new[] { "BulkBatchId", "OrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipment_orders_OrderId",
                table: "shipment_orders",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tracking_history_ShipmentOrderId",
                table: "tracking_history",
                column: "ShipmentOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bulk_batch_items");

            migrationBuilder.DropTable(
                name: "tracking_history");

            migrationBuilder.DropTable(
                name: "bulk_batches");

            migrationBuilder.DropTable(
                name: "shipment_orders");
        }
    }
}
