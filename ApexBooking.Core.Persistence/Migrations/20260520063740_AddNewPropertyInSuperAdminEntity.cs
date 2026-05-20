using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexBooking.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNewPropertyInSuperAdminEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "super_admin_refresh_token",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    super_admin_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    token = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    is_used = table.Column<bool>(type: "bit", nullable: false),
                    is_revoked = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_super_admin_refresh_token", x => x.Id);
                    table.ForeignKey(
                        name: "FK_super_admin_refresh_token_super_admin_super_admin_id",
                        column: x => x.super_admin_id,
                        principalTable: "super_admin",
                        principalColumn: "SuperAdminId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_super_admin_refresh_token_super_admin_id",
                table: "super_admin_refresh_token",
                column: "super_admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_super_admin_refresh_token_token",
                table: "super_admin_refresh_token",
                column: "token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "super_admin_refresh_token");
        }
    }
}
