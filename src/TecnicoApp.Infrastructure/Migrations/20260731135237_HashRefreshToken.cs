using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecnicoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HashRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RefreshToken",
                table: "Users",
                newName: "RefreshTokenHash");

            // Pre-existing values are raw plaintext tokens, not hashes, and cannot be
            // rehashed without the original token. Clear them so no plaintext secret is
            // left at rest under the new column name; affected users simply need to log
            // in again to get a hashed token.
            migrationBuilder.Sql(
                "UPDATE \"Users\" SET \"RefreshTokenHash\" = NULL, \"RefreshTokenExpiresAt\" = NULL " +
                "WHERE \"RefreshTokenHash\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RefreshTokenHash",
                table: "Users",
                newName: "RefreshToken");
        }
    }
}
