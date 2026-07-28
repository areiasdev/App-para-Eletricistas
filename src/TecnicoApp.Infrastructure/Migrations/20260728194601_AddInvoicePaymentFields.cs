using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecnicoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PayTokenExpiresAt",
                table: "Invoices",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayTokenHash",
                table: "Invoices",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeCheckoutSessionId",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_StripeCheckoutSessionId",
                table: "Invoices",
                column: "StripeCheckoutSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_StripeCheckoutSessionId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PayTokenExpiresAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PayTokenHash",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "StripeCheckoutSessionId",
                table: "Invoices");
        }
    }
}
