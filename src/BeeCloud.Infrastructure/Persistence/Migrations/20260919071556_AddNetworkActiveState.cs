using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeeCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNetworkActiveState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "networks",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "networks");
        }
    }
}
