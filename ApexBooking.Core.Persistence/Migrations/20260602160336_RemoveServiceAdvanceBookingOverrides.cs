using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexBooking.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveServiceAdvanceBookingOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_advance_booking_days",
                table: "services");

            migrationBuilder.DropColumn(
                name: "min_advance_booking_hours",
                table: "services");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "max_advance_booking_days",
                table: "services",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "min_advance_booking_hours",
                table: "services",
                type: "int",
                nullable: true);
        }
    }
}
