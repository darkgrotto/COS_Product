using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountOrSell.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDarkModePreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "dark_mode",
                table: "user_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dark_mode",
                table: "user_preferences");
        }
    }
}
