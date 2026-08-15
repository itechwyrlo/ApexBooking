using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexBooking.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingRefundTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "paymongo_payment_id",
                table: "bookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refund_status",
                table: "bookings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "refunded_amount",
                table: "bookings",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "refunded_at",
                table: "bookings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "paymongo_payment_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "refund_status",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "refunded_amount",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "refunded_at",
                table: "bookings");
        }
    }
}
