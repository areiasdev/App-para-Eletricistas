using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecnicoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientNotificationOptIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PhoneVerified",
                table: "Clients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppOptIn",
                table: "Clients",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneVerified",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "WhatsAppOptIn",
                table: "Clients");
        }
    }
}
