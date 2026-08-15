using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexBooking.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefundManualTransferRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "decision_action",
                table: "refund_requests");

            migrationBuilder.DropColumn(
                name: "is_auto_refund_eligible",
                table: "refund_requests");

            migrationBuilder.DropColumn(
                name: "owner_decided_at",
                table: "refund_requests");

            migrationBuilder.DropColumn(
                name: "owner_decided_by_user_id",
                table: "refund_requests");

            migrationBuilder.DropColumn(
                name: "PaymentMethodType",
                table: "bookings");

            migrationBuilder.RenameColumn(
                name: "automatic_refund",
                table: "payment_policy",
                newName: "refund_enabled");

            migrationBuilder.AlterColumn<string>(
                name: "customer_ewallet_provider",
                table: "refund_requests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "customer_ewallet_number",
                table: "refund_requests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_ewallet_name",
                table: "refund_requests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "receipt_url",
                table: "refund_requests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "in_visit_amount_collected",
                table: "bookings",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "service_price_at_booking",
                table: "bookings",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "customer_ewallet_name",
                table: "refund_requests");

            migrationBuilder.DropColumn(
                name: "receipt_url",
                table: "refund_requests");

            migrationBuilder.DropColumn(
                name: "in_visit_amount_collected",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "service_price_at_booking",
                table: "bookings");

            migrationBuilder.RenameColumn(
                name: "refund_enabled",
                table: "payment_policy",
                newName: "automatic_refund");

            migrationBuilder.AlterColumn<string>(
                name: "customer_ewallet_provider",
                table: "refund_requests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "customer_ewallet_number",
                table: "refund_requests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "decision_action",
                table: "refund_requests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_auto_refund_eligible",
                table: "refund_requests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "owner_decided_at",
                table: "refund_requests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "owner_decided_by_user_id",
                table: "refund_requests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodType",
                table: "bookings",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
