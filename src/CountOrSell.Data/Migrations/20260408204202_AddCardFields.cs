using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountOrSell.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCardFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "card_type",
                table: "cards",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "cmc",
                table: "cards",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "color_identity",
                table: "cards",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "keywords",
                table: "cards",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "layout",
                table: "cards",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mana_cost",
                table: "cards",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "oracle_text",
                table: "cards",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cmc",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "color_identity",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "keywords",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "layout",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "mana_cost",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "oracle_text",
                table: "cards");

            migrationBuilder.AlterColumn<string>(
                name: "card_type",
                table: "cards",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
