using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecnicoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientPortalTokenVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PortalTokenVersion",
                table: "Clients",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PortalTokenVersion",
                table: "Clients");
        }
    }
}
