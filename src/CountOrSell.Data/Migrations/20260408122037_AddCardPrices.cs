using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountOrSell.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCardPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "card_prices",
                columns: table => new
                {
                    card_identifier = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    treatment_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    price_usd = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    captured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_prices", x => new { x.card_identifier, x.treatment_key });
                    table.ForeignKey(
                        name: "FK_card_prices_cards_card_identifier",
                        column: x => x.card_identifier,
                        principalTable: "cards",
                        principalColumn: "identifier",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_card_prices_treatments_treatment_key",
                        column: x => x.treatment_key,
                        principalTable: "treatments",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_card_prices_card_identifier",
                table: "card_prices",
                column: "card_identifier");

            migrationBuilder.CreateIndex(
                name: "IX_card_prices_treatment_key",
                table: "card_prices",
                column: "treatment_key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "card_prices");
        }
    }
}
