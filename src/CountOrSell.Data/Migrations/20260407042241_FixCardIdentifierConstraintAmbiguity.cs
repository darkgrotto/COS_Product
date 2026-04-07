using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountOrSell.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCardIdentifierConstraintAmbiguity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_wishlist_entries_card_identifier",
                table: "wishlist_entries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_slab_entries_card_identifier",
                table: "slab_entries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_serialized_entries_card_identifier",
                table: "serialized_entries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_collection_entries_card_identifier",
                table: "collection_entries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_cards_identifier",
                table: "cards");

            migrationBuilder.AddCheckConstraint(
                name: "CK_wishlist_entries_card_identifier",
                table: "wishlist_entries",
                sql: "card_identifier ~ '^[a-z0-9]{3,4}([0-9]{3}|[1-9][0-9]{3})[a-z]?$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_slab_entries_card_identifier",
                table: "slab_entries",
                sql: "card_identifier ~ '^[a-z0-9]{3,4}([0-9]{3}|[1-9][0-9]{3})[a-z]?$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_serialized_entries_card_identifier",
                table: "serialized_entries",
                sql: "card_identifier ~ '^[a-z0-9]{3,4}([0-9]{3}|[1-9][0-9]{3})[a-z]?$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_collection_entries_card_identifier",
                table: "collection_entries",
                sql: "card_identifier ~ '^[a-z0-9]{3,4}([0-9]{3}|[1-9][0-9]{3})[a-z]?$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cards_identifier",
                table: "cards",
                sql: "identifier ~ '^[a-z0-9]{3,4}([0-9]{3}|[1-9][0-9]{3})[a-z]?$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_wishlist_entries_card_identifier",
                table: "wishlist_entries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_slab_entries_card_identifier",
                table: "slab_entries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_serialized_entries_card_identifier",
                table: "serialized_entries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_collection_entries_card_identifier",
                table: "collection_entries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_cards_identifier",
                table: "cards");

            migrationBuilder.AddCheckConstraint(
                name: "CK_wishlist_entries_card_identifier",
                table: "wishlist_entries",
                sql: "card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}[a-z]?$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_slab_entries_card_identifier",
                table: "slab_entries",
                sql: "card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}[a-z]?$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_serialized_entries_card_identifier",
                table: "serialized_entries",
                sql: "card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}[a-z]?$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_collection_entries_card_identifier",
                table: "collection_entries",
                sql: "card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}[a-z]?$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cards_identifier",
                table: "cards",
                sql: "identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$' AND identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}[a-z]?$'");
        }
    }
}
