using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecnicoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WidenLineUnitPricePrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "QuoteLines",
                type: "numeric(12,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "InvoiceLines",
                type: "numeric(12,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "QuoteLines",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "InvoiceLines",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)");
        }
    }
}
