using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexBooking.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenamePaymentPolicyRefundFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "refund_percent",
                table: "payment_policy");

            migrationBuilder.AddColumn<decimal>(
                name: "late_cancellation_refund_percent",
                table: "payment_policy",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "on_time_refund_percent",
                table: "payment_policy",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 100m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "late_cancellation_refund_percent",
                table: "payment_policy");

            migrationBuilder.DropColumn(
                name: "on_time_refund_percent",
                table: "payment_policy");

            migrationBuilder.AddColumn<decimal>(
                name: "refund_percent",
                table: "payment_policy",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
