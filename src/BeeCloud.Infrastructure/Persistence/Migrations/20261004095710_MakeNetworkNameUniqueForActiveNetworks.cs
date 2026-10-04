using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeeCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeNetworkNameUniqueForActiveNetworks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_networks_Name",
                table: "networks");

            migrationBuilder.CreateIndex(
                name: "IX_networks_Name",
                table: "networks",
                column: "Name",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_networks_Name",
                table: "networks");

            migrationBuilder.CreateIndex(
                name: "IX_networks_Name",
                table: "networks",
                column: "Name",
                unique: true);
        }
    }
}
