using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexBooking.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomaticRefundToPaymentPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "automatic_refund",
                table: "payment_policy",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodType",
                table: "bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "refund_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    booking_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    requested_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    is_auto_refund_eligible = table.Column<bool>(type: "bit", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    decided_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    decided_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    decision_action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    rejection_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    owner_decided_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    owner_decided_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    customer_ewallet_provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    customer_ewallet_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refund_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_refund_requests_booking_id",
                table: "refund_requests",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_refund_requests_tenant_id_status",
                table: "refund_requests",
                columns: new[] { "tenant_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refund_requests");

            migrationBuilder.DropColumn(
                name: "automatic_refund",
                table: "payment_policy");

            migrationBuilder.DropColumn(
                name: "PaymentMethodType",
                table: "bookings");
        }
    }
}
