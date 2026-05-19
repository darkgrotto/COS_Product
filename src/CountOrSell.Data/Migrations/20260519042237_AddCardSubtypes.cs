using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountOrSell.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCardSubtypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "card_subtypes",
                table: "cards",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // Backfill from existing card_type values. Strips everything up to and
            // including the last U+2014 em-dash, splits the remainder on whitespace,
            // deduplicates, and joins with commas. Single-face cards (the vast
            // majority) are populated perfectly; multi-face cards keep only the last
            // face's subtypes until the next content update reapplies the C# parser.
            migrationBuilder.Sql(@"
                UPDATE cards
                SET card_subtypes = (
                    SELECT array_to_string(
                        array_agg(DISTINCT word ORDER BY word),
                        ','
                    )
                    FROM unnest(string_to_array(
                        regexp_replace(card_type, '^.*—\s*', ''),
                        ' '
                    )) AS word
                    WHERE word <> ''
                )
                WHERE card_type LIKE '%—%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "card_subtypes",
                table: "cards");
        }
    }
}
