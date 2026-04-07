using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountOrSell.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultAcquisitionPricePref : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DefaultAcquisitionPriceToMarket",
                table: "user_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultAcquisitionPriceToMarket",
                table: "user_preferences");
        }
    }
}
