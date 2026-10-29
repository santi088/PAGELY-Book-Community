using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookCommunity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookProviderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Books",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalProvider",
                table: "Books",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_ExternalProvider_ExternalId",
                table: "Books",
                columns: new[] { "ExternalProvider", "ExternalId" },
                unique: true,
                filter: "\"ExternalProvider\" IS NOT NULL AND \"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Books_ISBN",
                table: "Books",
                column: "ISBN",
                unique: true,
                filter: "\"ISBN\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Books_ExternalProvider_ExternalId",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_ISBN",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "ExternalProvider",
                table: "Books");
        }
    }
}
