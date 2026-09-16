using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeeCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "node_metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComputeNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CpuUsagePercent = table.Column<double>(type: "double precision", nullable: false),
                    GpuUsagePercent = table.Column<double>(type: "double precision", nullable: false),
                    GpuTemperatureCelsius = table.Column<double>(type: "double precision", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_node_metrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_node_metrics_compute_nodes_ComputeNodeId",
                        column: x => x.ComputeNodeId,
                        principalTable: "compute_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_node_metrics_ComputeNodeId_RecordedAt",
                table: "node_metrics",
                columns: new[] { "ComputeNodeId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "node_metrics");
        }
    }
}
