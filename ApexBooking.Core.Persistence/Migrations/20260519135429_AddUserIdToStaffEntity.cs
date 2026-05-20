using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexBooking.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToStaffEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "Staffs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_user_id",
                table: "Staffs",
                column: "user_id",
                unique: true,
                filter: "[user_id] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Staffs_user_user_id",
                table: "Staffs",
                column: "user_id",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Staffs_user_user_id",
                table: "Staffs");

            migrationBuilder.DropIndex(
                name: "IX_Staffs_user_id",
                table: "Staffs");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "Staffs");
        }
    }
}
