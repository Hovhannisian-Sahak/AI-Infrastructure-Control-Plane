using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeeCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNetworks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "networks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_networks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "network_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComputeNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttachedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_network_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_network_attachments_compute_nodes_ComputeNodeId",
                        column: x => x.ComputeNodeId,
                        principalTable: "compute_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_network_attachments_networks_NetworkId",
                        column: x => x.NetworkId,
                        principalTable: "networks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_network_attachments_ComputeNodeId_NetworkId",
                table: "network_attachments",
                columns: new[] { "ComputeNodeId", "NetworkId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_network_attachments_NetworkId",
                table: "network_attachments",
                column: "NetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_networks_Name",
                table: "networks",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "network_attachments");

            migrationBuilder.DropTable(
                name: "networks");
        }
    }
}
