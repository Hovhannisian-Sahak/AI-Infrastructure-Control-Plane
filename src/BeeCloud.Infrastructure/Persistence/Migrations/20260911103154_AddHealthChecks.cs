using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeeCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "health_checks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComputeNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsHealthy = table.Column<bool>(type: "boolean", nullable: false),
                    CpuUsagePercent = table.Column<double>(type: "double precision", nullable: true),
                    GpuUsagePercent = table.Column<double>(type: "double precision", nullable: true),
                    GpuTemperatureCelsius = table.Column<double>(type: "double precision", nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_health_checks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_health_checks_compute_nodes_ComputeNodeId",
                        column: x => x.ComputeNodeId,
                        principalTable: "compute_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_health_checks_ComputeNodeId_CheckedAt",
                table: "health_checks",
                columns: new[] { "ComputeNodeId", "CheckedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "health_checks");
        }
    }
}
