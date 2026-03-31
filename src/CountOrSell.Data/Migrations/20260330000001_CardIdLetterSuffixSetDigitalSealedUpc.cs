using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountOrSell.Data.Migrations
{
    /// <inheritdoc />
    public partial class CardIdLetterSuffixSetDigitalSealedUpc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Extend card identifier columns from varchar(8) to varchar(9)
            // to support optional trailing letter (e.g. "pala001a", "eoe001b")

            migrationBuilder.DropCheckConstraint(
                name: "CK_cards_identifier",
                table: "cards");

            migrationBuilder.AlterColumn<string>(
                name: "identifier",
                table: "cards",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8);

            migrationBuilder.AddCheckConstraint(
                name: "CK_cards_identifier",
                table: "cards",
                sql: @"identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$' AND identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}[a-z]?$'");

            migrationBuilder.DropCheckConstraint(
                name: "CK_collection_entries_card_identifier",
                table: "collection_entries");

            migrationBuilder.AlterColumn<string>(
                name: "card_identifier",
                table: "collection_entries",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8);

            migrationBuilder.AddCheckConstraint(
                name: "CK_collection_entries_card_identifier",
                table: "collection_entries",
                sql: @"card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}[a-z]?$'");

            migrationBuilder.DropCheckConstraint(
                name: "CK_serialized_entries_card_identifier",
                table: "serialized_entries");

            migrationBuilder.AlterColumn<string>(
                name: "card_identifier",
                table: "serialized_entries",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8);

            migrationBuilder.AddCheckConstraint(
                name: "CK_serialized_entries_card_identifier",
                table: "serialized_entries",
                sql: @"card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}[a-z]?$'");

            migrationBuilder.DropCheckConstraint(
                name: "CK_slab_entries_card_identifier",
                table: "slab_entries");

            migrationBuilder.AlterColumn<string>(
                name: "card_identifier",
                table: "slab_entries",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8);

            migrationBuilder.AddCheckConstraint(
                name: "CK_slab_entries_card_identifier",
                table: "slab_entries",
                sql: @"card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}[a-z]?$'");

            migrationBuilder.DropCheckConstraint(
                name: "CK_wishlist_entries_card_identifier",
                table: "wishlist_entries");

            migrationBuilder.AlterColumn<string>(
                name: "card_identifier",
                table: "wishlist_entries",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8);

            migrationBuilder.AddCheckConstraint(
                name: "CK_wishlist_entries_card_identifier",
                table: "wishlist_entries",
                sql: @"card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}[a-z]?$'");

            // Add digital flag to sets (default false - existing sets are not digital-only)
            migrationBuilder.AddColumn<bool>(
                name: "digital",
                table: "sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Add optional UPC field to sealed products (UPC-A 12 digits or EAN-13 13 digits)
            migrationBuilder.AddColumn<string>(
                name: "upc",
                table: "sealed_products",
                type: "character varying(13)",
                maxLength: 13,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_sealed_products_upc",
                table: "sealed_products",
                sql: @"upc IS NULL OR upc ~ '^\d{12,13}$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_sealed_products_upc",
                table: "sealed_products");

            migrationBuilder.DropColumn(
                name: "upc",
                table: "sealed_products");

            migrationBuilder.DropColumn(
                name: "digital",
                table: "sets");

            // Revert card identifier columns back to varchar(8)

            migrationBuilder.DropCheckConstraint(
                name: "CK_wishlist_entries_card_identifier",
                table: "wishlist_entries");

            migrationBuilder.AlterColumn<string>(
                name: "card_identifier",
                table: "wishlist_entries",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(9)",
                oldMaxLength: 9);

            migrationBuilder.AddCheckConstraint(
                name: "CK_wishlist_entries_card_identifier",
                table: "wishlist_entries",
                sql: @"card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}$'");

            migrationBuilder.DropCheckConstraint(
                name: "CK_slab_entries_card_identifier",
                table: "slab_entries");

            migrationBuilder.AlterColumn<string>(
                name: "card_identifier",
                table: "slab_entries",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(9)",
                oldMaxLength: 9);

            migrationBuilder.AddCheckConstraint(
                name: "CK_slab_entries_card_identifier",
                table: "slab_entries",
                sql: @"card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}$'");

            migrationBuilder.DropCheckConstraint(
                name: "CK_serialized_entries_card_identifier",
                table: "serialized_entries");

            migrationBuilder.AlterColumn<string>(
                name: "card_identifier",
                table: "serialized_entries",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(9)",
                oldMaxLength: 9);

            migrationBuilder.AddCheckConstraint(
                name: "CK_serialized_entries_card_identifier",
                table: "serialized_entries",
                sql: @"card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}$'");

            migrationBuilder.DropCheckConstraint(
                name: "CK_collection_entries_card_identifier",
                table: "collection_entries");

            migrationBuilder.AlterColumn<string>(
                name: "card_identifier",
                table: "collection_entries",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(9)",
                oldMaxLength: 9);

            migrationBuilder.AddCheckConstraint(
                name: "CK_collection_entries_card_identifier",
                table: "collection_entries",
                sql: @"card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}$'");

            migrationBuilder.DropCheckConstraint(
                name: "CK_cards_identifier",
                table: "cards");

            migrationBuilder.AlterColumn<string>(
                name: "identifier",
                table: "cards",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(9)",
                oldMaxLength: 9);

            migrationBuilder.AddCheckConstraint(
                name: "CK_cards_identifier",
                table: "cards",
                sql: @"identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}$' AND identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}$'");
        }
    }
}
