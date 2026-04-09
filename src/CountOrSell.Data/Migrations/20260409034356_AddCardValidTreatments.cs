using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountOrSell.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCardValidTreatments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "valid_treatments",
                table: "cards",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "valid_treatments",
                table: "cards");
        }
    }
}
