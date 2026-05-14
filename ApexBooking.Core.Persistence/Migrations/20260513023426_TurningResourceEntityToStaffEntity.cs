using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexBooking.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TurningResourceEntityToStaffEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resource_availability_exceptions");

            migrationBuilder.DropTable(
                name: "resource_break_periods");

            migrationBuilder.DropTable(
                name: "service_resources");

            migrationBuilder.DropTable(
                name: "resource_availability_schedules");

            migrationBuilder.DropTable(
                name: "resources");

            migrationBuilder.RenameColumn(
                name: "resource_id",
                table: "bookings",
                newName: "staff_id");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_tenant_id_resource_id_scheduled_date_status",
                table: "bookings",
                newName: "IX_bookings_tenant_id_staff_id_scheduled_date_status");

            migrationBuilder.CreateTable(
                name: "service_staffs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    staff_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_staffs", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_staffs_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Staffs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    capacity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staffs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staff_availability_exceptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    staff_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    exception_date = table.Column<DateOnly>(type: "date", nullable: false),
                    type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_availability_exceptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_staff_availability_exceptions_Staffs_staff_id",
                        column: x => x.staff_id,
                        principalTable: "Staffs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "staff_availability_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    staff_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    day_of_week = table.Column<int>(type: "int", nullable: false),
                    is_available = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_availability_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_staff_availability_schedules_Staffs_staff_id",
                        column: x => x.staff_id,
                        principalTable: "Staffs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "staff_break_periods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    staff_availability_schedule_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    resource_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    break_start = table.Column<TimeOnly>(type: "time", nullable: false),
                    break_end = table.Column<TimeOnly>(type: "time", nullable: false),
                    label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_break_periods", x => x.id);
                    table.ForeignKey(
                        name: "FK_staff_break_periods_staff_availability_schedules_staff_availability_schedule_id",
                        column: x => x.staff_availability_schedule_id,
                        principalTable: "staff_availability_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_staffs_service_id",
                table: "service_staffs",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_staffs_service_id_staff_id",
                table: "service_staffs",
                columns: new[] { "service_id", "staff_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_staffs_staff_id",
                table: "service_staffs",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_staffs_tenant_id",
                table: "service_staffs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_availability_exceptions_exception_date",
                table: "staff_availability_exceptions",
                column: "exception_date");

            migrationBuilder.CreateIndex(
                name: "IX_staff_availability_exceptions_staff_id",
                table: "staff_availability_exceptions",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_availability_exceptions_staff_id_exception_date_type",
                table: "staff_availability_exceptions",
                columns: new[] { "staff_id", "exception_date", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_availability_exceptions_tenant_id",
                table: "staff_availability_exceptions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_availability_schedules_staff_id",
                table: "staff_availability_schedules",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_availability_schedules_staff_id_day_of_week",
                table: "staff_availability_schedules",
                columns: new[] { "staff_id", "day_of_week" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_availability_schedules_tenant_id",
                table: "staff_availability_schedules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_break_periods_resource_id",
                table: "staff_break_periods",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_break_periods_staff_availability_schedule_id",
                table: "staff_break_periods",
                column: "staff_availability_schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_break_periods_tenant_id",
                table: "staff_break_periods",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_tenant_id",
                table: "Staffs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_tenant_id_is_active",
                table: "Staffs",
                columns: new[] { "tenant_id", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_staffs");

            migrationBuilder.DropTable(
                name: "staff_availability_exceptions");

            migrationBuilder.DropTable(
                name: "staff_break_periods");

            migrationBuilder.DropTable(
                name: "staff_availability_schedules");

            migrationBuilder.DropTable(
                name: "Staffs");

            migrationBuilder.RenameColumn(
                name: "staff_id",
                table: "bookings",
                newName: "resource_id");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_tenant_id_staff_id_scheduled_date_status",
                table: "bookings",
                newName: "IX_bookings_tenant_id_resource_id_scheduled_date_status");

            migrationBuilder.CreateTable(
                name: "resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    capacity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    resource_type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    resource_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_resources", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_resources_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource_availability_exceptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    exception_date = table.Column<DateOnly>(type: "date", nullable: false),
                    type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    resource_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_availability_exceptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_resource_availability_exceptions_resources_resource_id",
                        column: x => x.resource_id,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource_availability_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    day_of_week = table.Column<int>(type: "int", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    is_available = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    resource_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_availability_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_resource_availability_schedules_resources_resource_id",
                        column: x => x.resource_id,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource_break_periods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    break_end = table.Column<TimeOnly>(type: "time", nullable: false),
                    break_start = table.Column<TimeOnly>(type: "time", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    resource_availability_schedule_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    resource_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_break_periods", x => x.id);
                    table.ForeignKey(
                        name: "FK_resource_break_periods_resource_availability_schedules_resource_availability_schedule_id",
                        column: x => x.resource_availability_schedule_id,
                        principalTable: "resource_availability_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_resource_availability_exceptions_exception_date",
                table: "resource_availability_exceptions",
                column: "exception_date");

            migrationBuilder.CreateIndex(
                name: "IX_resource_availability_exceptions_resource_id",
                table: "resource_availability_exceptions",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_resource_availability_exceptions_resource_id_exception_date_type",
                table: "resource_availability_exceptions",
                columns: new[] { "resource_id", "exception_date", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resource_availability_exceptions_tenant_id",
                table: "resource_availability_exceptions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_resource_availability_schedules_resource_id",
                table: "resource_availability_schedules",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_resource_availability_schedules_resource_id_day_of_week",
                table: "resource_availability_schedules",
                columns: new[] { "resource_id", "day_of_week" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resource_availability_schedules_tenant_id",
                table: "resource_availability_schedules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_resource_break_periods_resource_availability_schedule_id",
                table: "resource_break_periods",
                column: "resource_availability_schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_resource_break_periods_resource_id",
                table: "resource_break_periods",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_resource_break_periods_tenant_id",
                table: "resource_break_periods",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_resources_resource_type",
                table: "resources",
                column: "resource_type");

            migrationBuilder.CreateIndex(
                name: "IX_resources_tenant_id",
                table: "resources",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_resources_tenant_id_is_active",
                table: "resources",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_service_resources_resource_id",
                table: "service_resources",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_resources_service_id",
                table: "service_resources",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_resources_service_id_resource_id",
                table: "service_resources",
                columns: new[] { "service_id", "resource_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_resources_tenant_id",
                table: "service_resources",
                column: "tenant_id");
        }
    }
}
