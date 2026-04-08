using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountOrSell.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistTreatment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_wishlist_entries_user_id_card_identifier",
                table: "wishlist_entries");

            migrationBuilder.AddColumn<string>(
                name: "treatment_key",
                table: "wishlist_entries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "regular");

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_entries_treatment_key",
                table: "wishlist_entries",
                column: "treatment_key");

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_entries_user_id_card_identifier_treatment_key",
                table: "wishlist_entries",
                columns: new[] { "user_id", "card_identifier", "treatment_key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_wishlist_entries_treatments_treatment_key",
                table: "wishlist_entries",
                column: "treatment_key",
                principalTable: "treatments",
                principalColumn: "key",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_wishlist_entries_treatments_treatment_key",
                table: "wishlist_entries");

            migrationBuilder.DropIndex(
                name: "IX_wishlist_entries_treatment_key",
                table: "wishlist_entries");

            migrationBuilder.DropIndex(
                name: "IX_wishlist_entries_user_id_card_identifier_treatment_key",
                table: "wishlist_entries");

            migrationBuilder.DropColumn(
                name: "treatment_key",
                table: "wishlist_entries");

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_entries_user_id_card_identifier",
                table: "wishlist_entries",
                columns: new[] { "user_id", "card_identifier" },
                unique: true);
        }
    }
}
