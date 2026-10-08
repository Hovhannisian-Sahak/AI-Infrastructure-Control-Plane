using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeeCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentOccurrenceTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "incidents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OccurrenceCount",
                table: "incidents",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE incidents
                SET "LastSeenAt" = "CreatedAt",
                    "OccurrenceCount" = 1;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastSeenAt",
                table: "incidents",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OccurrenceCount",
                table: "incidents",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "OccurrenceCount",
                table: "incidents");
        }
    }
}
