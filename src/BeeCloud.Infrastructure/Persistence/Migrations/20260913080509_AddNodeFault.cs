using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeeCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeFault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActiveFault",
                table: "compute_nodes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveFault",
                table: "compute_nodes");
        }
    }
}
