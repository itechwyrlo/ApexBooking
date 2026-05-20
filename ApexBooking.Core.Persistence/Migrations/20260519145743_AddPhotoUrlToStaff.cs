using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexBooking.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoUrlToStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "photo_url",
                table: "Staffs",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "photo_url",
                table: "Staffs");
        }
    }
}
