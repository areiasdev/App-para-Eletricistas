using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecnicoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FieldWorkAndOnlineApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultHourlyRate",
                table: "Users",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedByName",
                table: "Quotes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalTokenExpiresAt",
                table: "Quotes",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalTokenHash",
                table: "Quotes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClientDecisionAt",
                table: "Quotes",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FollowUpSentAt",
                table: "Quotes",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Quotes",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "QuoteLines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "QuoteLines",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "un");

            migrationBuilder.AddColumn<Guid>(
                name: "InterventionId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "InvoiceLines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "InvoiceLines",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "un");

            migrationBuilder.AddColumn<string>(
                name: "ClientSignatureUrl",
                table: "Interventions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LaborHours",
                table: "Interventions",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedAt",
                table: "Interventions",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedByName",
                table: "Interventions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaintenanceIntervalMonths",
                table: "Equipment",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_ApprovalTokenHash",
                table: "Quotes",
                column: "ApprovalTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InterventionId",
                table: "Invoices",
                column: "InterventionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Interventions_InterventionId",
                table: "Invoices",
                column: "InterventionId",
                principalTable: "Interventions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Interventions_InterventionId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_ApprovalTokenHash",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_InterventionId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DefaultHourlyRate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AcceptedByName",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ApprovalTokenExpiresAt",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ApprovalTokenHash",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ClientDecisionAt",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "FollowUpSentAt",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "QuoteLines");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "QuoteLines");

            migrationBuilder.DropColumn(
                name: "InterventionId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "ClientSignatureUrl",
                table: "Interventions");

            migrationBuilder.DropColumn(
                name: "LaborHours",
                table: "Interventions");

            migrationBuilder.DropColumn(
                name: "SignedAt",
                table: "Interventions");

            migrationBuilder.DropColumn(
                name: "SignedByName",
                table: "Interventions");

            migrationBuilder.DropColumn(
                name: "MaintenanceIntervalMonths",
                table: "Equipment");
        }
    }
}
