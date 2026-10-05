using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeeCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeComputeNodeNameUniqueForActiveNodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_compute_nodes_Name",
                table: "compute_nodes");

            migrationBuilder.CreateIndex(
                name: "IX_compute_nodes_Name",
                table: "compute_nodes",
                column: "Name",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_compute_nodes_Name",
                table: "compute_nodes");

            migrationBuilder.CreateIndex(
                name: "IX_compute_nodes_Name",
                table: "compute_nodes",
                column: "Name",
                unique: true);
        }
    }
}
