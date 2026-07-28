using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecnicoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBillingAndPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Plan",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Plan",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAt",
                table: "Users",
                type: "timestamp without time zone",
                nullable: true);
        }
    }
}
