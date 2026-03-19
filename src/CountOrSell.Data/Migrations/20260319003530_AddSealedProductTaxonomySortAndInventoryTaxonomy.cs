using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CountOrSell.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSealedProductTaxonomySortAndInventoryTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_notifications",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_settings", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "backup_destination_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    configuration_json = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backup_destination_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "backup_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    backup_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backup_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "grading_agencies",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    validation_url_template = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    supports_direct_lookup = table.Column<bool>(type: "boolean", nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grading_agencies", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "pending_schema_updates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    schema_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    download_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    zip_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pending_schema_updates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sealed_product_categories",
                columns: table => new
                {
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sealed_product_categories", x => x.slug);
                });

            migrationBuilder.CreateTable(
                name: "sets",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    total_cards = table.Column<int>(type: "integer", nullable: false),
                    release_date = table.Column<DateOnly>(type: "date", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sets", x => x.code);
                    table.CheckConstraint("CK_sets_code", "code ~ '^[a-z0-9]{3,4}$'");
                });

            migrationBuilder.CreateTable(
                name: "treatments",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatments", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "update_versions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    content_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    schema_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    application_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    applied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_update_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_export_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_export_files", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    auth_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_builtin_admin = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    oauth_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    oauth_provider_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "backup_destination_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    backup_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    destination_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    attempted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backup_destination_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_backup_destination_records_backup_records_backup_record_id",
                        column: x => x.backup_record_id,
                        principalTable: "backup_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sealed_product_sub_types",
                columns: table => new
                {
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category_slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sealed_product_sub_types", x => x.slug);
                    table.ForeignKey(
                        name: "FK_sealed_product_sub_types_sealed_product_categories_category~",
                        column: x => x.category_slug,
                        principalTable: "sealed_product_categories",
                        principalColumn: "slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cards",
                columns: table => new
                {
                    identifier = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    set_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    card_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    oracle_ruling_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    current_market_value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_reserved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cards", x => x.identifier);
                    table.CheckConstraint("CK_cards_identifier", "identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}$' AND identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}$'");
                    table.ForeignKey(
                        name: "FK_cards_sets_set_code",
                        column: x => x.set_code,
                        principalTable: "sets",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "collection_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_identifier = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    treatment_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    condition = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    autographed = table.Column<bool>(type: "boolean", nullable: false),
                    acquisition_date = table.Column<DateOnly>(type: "date", nullable: false),
                    acquisition_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collection_entries", x => x.id);
                    table.CheckConstraint("CK_collection_entries_card_identifier", "card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}$'");
                    table.CheckConstraint("CK_collection_entries_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "FK_collection_entries_treatments_treatment_key",
                        column: x => x.treatment_key,
                        principalTable: "treatments",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_collection_entries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "serialized_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_identifier = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    treatment_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    serial_number = table.Column<int>(type: "integer", nullable: false),
                    print_run_total = table.Column<int>(type: "integer", nullable: false),
                    condition = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    autographed = table.Column<bool>(type: "boolean", nullable: false),
                    acquisition_date = table.Column<DateOnly>(type: "date", nullable: false),
                    acquisition_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_serialized_entries", x => x.id);
                    table.CheckConstraint("CK_serialized_entries_card_identifier", "card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}$'");
                    table.ForeignKey(
                        name: "FK_serialized_entries_treatments_treatment_key",
                        column: x => x.treatment_key,
                        principalTable: "treatments",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_serialized_entries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "slab_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_identifier = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    treatment_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    grading_agency_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    grade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    certificate_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    serial_number = table.Column<int>(type: "integer", nullable: true),
                    print_run_total = table.Column<int>(type: "integer", nullable: true),
                    condition = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    autographed = table.Column<bool>(type: "boolean", nullable: false),
                    acquisition_date = table.Column<DateOnly>(type: "date", nullable: false),
                    acquisition_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_slab_entries", x => x.id);
                    table.CheckConstraint("CK_slab_entries_card_identifier", "card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}$'");
                    table.CheckConstraint("CK_slab_entries_print_run_total", "serial_number IS NULL OR print_run_total IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_slab_entries_grading_agencies_grading_agency_code",
                        column: x => x.grading_agency_code,
                        principalTable: "grading_agencies",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_slab_entries_treatments_treatment_key",
                        column: x => x.treatment_key,
                        principalTable: "treatments",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_slab_entries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_page = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    set_completion_regular_only = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_preferences", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_user_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wishlist_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_identifier = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wishlist_entries", x => x.id);
                    table.CheckConstraint("CK_wishlist_entries_card_identifier", "card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}$'");
                    table.ForeignKey(
                        name: "FK_wishlist_entries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sealed_inventory_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_identifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    condition = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    acquisition_date = table.Column<DateOnly>(type: "date", nullable: false),
                    acquisition_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    category_slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sub_type_slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sealed_inventory_entries", x => x.id);
                    table.CheckConstraint("CK_sealed_inventory_entries_quantity", "quantity > 0");
                    table.CheckConstraint("CK_sealed_inventory_entries_sub_type_slug", "sub_type_slug IS NULL OR category_slug IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_sealed_inventory_entries_sealed_product_categories_category~",
                        column: x => x.category_slug,
                        principalTable: "sealed_product_categories",
                        principalColumn: "slug",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sealed_inventory_entries_sealed_product_sub_types_sub_type_~",
                        column: x => x.sub_type_slug,
                        principalTable: "sealed_product_sub_types",
                        principalColumn: "slug",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sealed_inventory_entries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sealed_products",
                columns: table => new
                {
                    identifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    set_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    category_slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sub_type_slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    current_market_value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    image_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sealed_products", x => x.identifier);
                    table.ForeignKey(
                        name: "FK_sealed_products_sealed_product_categories_category_slug",
                        column: x => x.category_slug,
                        principalTable: "sealed_product_categories",
                        principalColumn: "slug",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sealed_products_sealed_product_sub_types_sub_type_slug",
                        column: x => x.sub_type_slug,
                        principalTable: "sealed_product_sub_types",
                        principalColumn: "slug",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sealed_products_sets_set_code",
                        column: x => x.set_code,
                        principalTable: "sets",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "grading_agencies",
                columns: new[] { "code", "active", "full_name", "source", "supports_direct_lookup", "validation_url_template" },
                values: new object[,]
                {
                    { "bgs", true, "Beckett Grading Services", "Canonical", true, "https://www.beckett.com/grading" },
                    { "ccc", true, "Certificateur de Cartes de Collection", "Canonical", false, "https://cccgrading.com/en/ccc-card-verification" },
                    { "cgc", true, "Certified Guaranty Company", "Canonical", true, "https://www.cgccards.com/certlookup/{0}" },
                    { "isa", true, "International Sports Authentication", "Canonical", true, "https://www.isagrading.com/verify/{0}" },
                    { "psa", true, "Professional Sports Authenticator", "Canonical", true, "https://www.psacard.com/cert/{0}" },
                    { "sgc", true, "Sportscard Guaranty", "Canonical", true, "https://www.sgccard.com/cert/{0}" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_backup_destination_records_backup_record_id",
                table: "backup_destination_records",
                column: "backup_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_cards_set_code",
                table: "cards",
                column: "set_code");

            migrationBuilder.CreateIndex(
                name: "IX_collection_entries_card_identifier",
                table: "collection_entries",
                column: "card_identifier");

            migrationBuilder.CreateIndex(
                name: "IX_collection_entries_treatment_key",
                table: "collection_entries",
                column: "treatment_key");

            migrationBuilder.CreateIndex(
                name: "IX_collection_entries_user_id",
                table: "collection_entries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sealed_inventory_entries_category_slug",
                table: "sealed_inventory_entries",
                column: "category_slug");

            migrationBuilder.CreateIndex(
                name: "IX_sealed_inventory_entries_sub_type_slug",
                table: "sealed_inventory_entries",
                column: "sub_type_slug");

            migrationBuilder.CreateIndex(
                name: "IX_sealed_inventory_entries_user_id",
                table: "sealed_inventory_entries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sealed_product_sub_types_category_slug",
                table: "sealed_product_sub_types",
                column: "category_slug");

            migrationBuilder.CreateIndex(
                name: "IX_sealed_products_category_slug",
                table: "sealed_products",
                column: "category_slug");

            migrationBuilder.CreateIndex(
                name: "IX_sealed_products_set_code",
                table: "sealed_products",
                column: "set_code");

            migrationBuilder.CreateIndex(
                name: "IX_sealed_products_sub_type_slug",
                table: "sealed_products",
                column: "sub_type_slug");

            migrationBuilder.CreateIndex(
                name: "IX_serialized_entries_treatment_key",
                table: "serialized_entries",
                column: "treatment_key");

            migrationBuilder.CreateIndex(
                name: "IX_serialized_entries_user_id",
                table: "serialized_entries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_slab_entries_grading_agency_code",
                table: "slab_entries",
                column: "grading_agency_code");

            migrationBuilder.CreateIndex(
                name: "IX_slab_entries_treatment_key",
                table: "slab_entries",
                column: "treatment_key");

            migrationBuilder.CreateIndex(
                name: "IX_slab_entries_user_id",
                table: "slab_entries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_export_files_user_id",
                table: "user_export_files",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_oauth_provider_oauth_provider_user_id",
                table: "users",
                columns: new[] { "oauth_provider", "oauth_provider_user_id" },
                unique: true,
                filter: "oauth_provider IS NOT NULL AND oauth_provider_user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_entries_user_id",
                table: "wishlist_entries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_entries_user_id_card_identifier",
                table: "wishlist_entries",
                columns: new[] { "user_id", "card_identifier" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_notifications");

            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "backup_destination_configs");

            migrationBuilder.DropTable(
                name: "backup_destination_records");

            migrationBuilder.DropTable(
                name: "cards");

            migrationBuilder.DropTable(
                name: "collection_entries");

            migrationBuilder.DropTable(
                name: "pending_schema_updates");

            migrationBuilder.DropTable(
                name: "sealed_inventory_entries");

            migrationBuilder.DropTable(
                name: "sealed_products");

            migrationBuilder.DropTable(
                name: "serialized_entries");

            migrationBuilder.DropTable(
                name: "slab_entries");

            migrationBuilder.DropTable(
                name: "update_versions");

            migrationBuilder.DropTable(
                name: "user_export_files");

            migrationBuilder.DropTable(
                name: "user_preferences");

            migrationBuilder.DropTable(
                name: "wishlist_entries");

            migrationBuilder.DropTable(
                name: "backup_records");

            migrationBuilder.DropTable(
                name: "sealed_product_sub_types");

            migrationBuilder.DropTable(
                name: "sets");

            migrationBuilder.DropTable(
                name: "grading_agencies");

            migrationBuilder.DropTable(
                name: "treatments");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "sealed_product_categories");
        }
    }
}
