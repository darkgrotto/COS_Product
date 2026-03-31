using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountOrSell.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFlavorTextToCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "flavor_text",
                table: "cards",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "flavor_text",
                table: "cards");
        }
    }
}
