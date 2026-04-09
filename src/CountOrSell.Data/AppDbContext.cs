using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;

// Backup model configuration is in ConfigureBackupRecords,
// ConfigureBackupDestinationRecords, ConfigureBackupDestinationConfigs.

namespace CountOrSell.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<Treatment> Treatments => Set<Treatment>();
    public DbSet<GradingAgency> GradingAgencies => Set<GradingAgency>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardPrice> CardPrices => Set<CardPrice>();
    public DbSet<Set> Sets => Set<Set>();
    public DbSet<SealedProduct> SealedProducts => Set<SealedProduct>();
    public DbSet<SealedProductCategory> SealedProductCategories => Set<SealedProductCategory>();
    public DbSet<SealedProductSubType> SealedProductSubTypes => Set<SealedProductSubType>();
    public DbSet<CollectionEntry> CollectionEntries => Set<CollectionEntry>();
    public DbSet<SerializedEntry> SerializedEntries => Set<SerializedEntry>();
    public DbSet<SlabEntry> SlabEntries => Set<SlabEntry>();
    public DbSet<SealedInventoryEntry> SealedInventoryEntries => Set<SealedInventoryEntry>();
    public DbSet<WishlistEntry> WishlistEntries => Set<WishlistEntry>();
    public DbSet<UpdateVersion> UpdateVersions => Set<UpdateVersion>();
    public DbSet<PendingSchemaUpdate> PendingSchemaUpdates => Set<PendingSchemaUpdate>();
    public DbSet<AdminNotification> AdminNotifications => Set<AdminNotification>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<UserExportFile> UserExportFiles => Set<UserExportFile>();
    public DbSet<UserInvitation> UserInvitations => Set<UserInvitation>();
    public DbSet<BackupRecord> BackupRecords => Set<BackupRecord>();
    public DbSet<BackupDestinationRecord> BackupDestinationRecords => Set<BackupDestinationRecord>();
    public DbSet<BackupDestinationConfig> BackupDestinationConfigs => Set<BackupDestinationConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureUserPreferences(modelBuilder);
        ConfigureTreatments(modelBuilder);
        ConfigureGradingAgencies(modelBuilder);
        ConfigureSets(modelBuilder);
        ConfigureCards(modelBuilder);
        ConfigureCardPrices(modelBuilder);
        ConfigureSealedProductTaxonomy(modelBuilder);
        ConfigureSealedProducts(modelBuilder);
        ConfigureCollectionEntries(modelBuilder);
        ConfigureSerializedEntries(modelBuilder);
        ConfigureSlabEntries(modelBuilder);
        ConfigureSealedInventoryEntries(modelBuilder);
        ConfigureWishlistEntries(modelBuilder);
        ConfigureUpdateVersions(modelBuilder);
        ConfigurePendingSchemaUpdates(modelBuilder);
        ConfigureAdminNotifications(modelBuilder);
        ConfigureAppSettings(modelBuilder);
        ConfigureUserExportFiles(modelBuilder);
        ConfigureUserInvitations(modelBuilder);
        ConfigureBackupRecords(modelBuilder);
        ConfigureBackupDestinationRecords(modelBuilder);
        ConfigureBackupDestinationConfigs(modelBuilder);
        SeedGradingAgencies(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id");
            e.Property(u => u.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
            e.Property(u => u.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
            e.Property(u => u.AuthType).HasColumnName("auth_type")
                .HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(u => u.Role).HasColumnName("role")
                .HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(u => u.IsBuiltinAdmin).HasColumnName("is_builtin_admin").IsRequired();
            e.Property(u => u.State).HasColumnName("state")
                .HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(u => u.UpdatedAt).HasColumnName("updated_at").IsRequired();
            e.Property(u => u.LastLoginAt).HasColumnName("last_login_at");
            e.Property(u => u.OAuthProvider).HasColumnName("oauth_provider").HasMaxLength(50);
            e.Property(u => u.OAuthProviderUserId).HasColumnName("oauth_provider_user_id").HasMaxLength(200);
            e.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(200);
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => new { u.OAuthProvider, u.OAuthProviderUserId }).IsUnique()
                .HasFilter("oauth_provider IS NOT NULL AND oauth_provider_user_id IS NOT NULL");
            e.HasOne(u => u.Preferences)
                .WithOne(p => p.User)
                .HasForeignKey<UserPreferences>(p => p.UserId);
        });
    }

    private static void ConfigureUserPreferences(ModelBuilder b)
    {
        b.Entity<UserPreferences>(e =>
        {
            e.ToTable("user_preferences");
            e.HasKey(p => p.UserId);
            e.Property(p => p.UserId).HasColumnName("user_id");
            e.Property(p => p.DefaultPage).HasColumnName("default_page").HasMaxLength(100);
            e.Property(p => p.SetCompletionRegularOnly).HasColumnName("set_completion_regular_only")
                .HasDefaultValue(false).IsRequired();
            e.Property(p => p.DarkMode).HasColumnName("dark_mode")
                .HasDefaultValue(false).IsRequired();
            e.Property(p => p.NavLayout).HasColumnName("nav_layout")
                .HasMaxLength(20).HasDefaultValue("sidebar").IsRequired();
            e.Property(p => p.CardSortDefault).HasColumnName("card_sort_default")
                .HasMaxLength(20).HasDefaultValue("name").IsRequired();
        });
    }

    private static void ConfigureTreatments(ModelBuilder b)
    {
        b.Entity<Treatment>(e =>
        {
            e.ToTable("treatments");
            e.HasKey(t => t.Key);
            e.Property(t => t.Key).HasColumnName("key").HasMaxLength(50);
            e.Property(t => t.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
            e.Property(t => t.SortOrder).HasColumnName("sort_order").IsRequired();
        });
    }

    private static void ConfigureGradingAgencies(ModelBuilder b)
    {
        b.Entity<GradingAgency>(e =>
        {
            e.ToTable("grading_agencies");
            e.HasKey(a => a.Code);
            e.Property(a => a.Code).HasColumnName("code").HasMaxLength(20);
            e.Property(a => a.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
            e.Property(a => a.ValidationUrlTemplate).HasColumnName("validation_url_template")
                .HasMaxLength(500).IsRequired();
            e.Property(a => a.SupportsDirectLookup).HasColumnName("supports_direct_lookup").IsRequired();
            e.Property(a => a.Source).HasColumnName("source")
                .HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(a => a.Active).HasColumnName("active").IsRequired();
        });
    }

    private static void ConfigureSets(ModelBuilder b)
    {
        b.Entity<Set>(e =>
        {
            e.ToTable("sets", t =>
            {
                t.HasCheckConstraint("CK_sets_code", @"code ~ '^[a-z0-9]{3,4}$'");
            });
            e.HasKey(s => s.Code);
            e.Property(s => s.Code).HasColumnName("code").HasMaxLength(4);
            e.Property(s => s.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
            e.Property(s => s.TotalCards).HasColumnName("total_cards").IsRequired();
            e.Property(s => s.SetType).HasColumnName("set_type").HasMaxLength(50);
            e.Property(s => s.ReleaseDate).HasColumnName("release_date");
            e.Property(s => s.Digital).HasColumnName("digital").HasDefaultValue(false).IsRequired();
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });
    }

    private static void ConfigureSealedProductTaxonomy(ModelBuilder b)
    {
        b.Entity<SealedProductCategory>(e =>
        {
            e.ToTable("sealed_product_categories");
            e.HasKey(c => c.Slug);
            e.Property(c => c.Slug).HasColumnName("slug").HasMaxLength(100);
            e.Property(c => c.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
            e.Property(c => c.SortOrder).HasColumnName("sort_order").IsRequired();
        });

        b.Entity<SealedProductSubType>(e =>
        {
            e.ToTable("sealed_product_sub_types");
            e.HasKey(s => s.Slug);
            e.Property(s => s.Slug).HasColumnName("slug").HasMaxLength(100);
            e.Property(s => s.CategorySlug).HasColumnName("category_slug").HasMaxLength(100).IsRequired();
            e.Property(s => s.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
            e.Property(s => s.SortOrder).HasColumnName("sort_order").IsRequired();
            e.HasOne<SealedProductCategory>().WithMany().HasForeignKey(s => s.CategorySlug);
            e.HasIndex(s => s.CategorySlug);
        });
    }

    private static void ConfigureSealedProducts(ModelBuilder b)
    {
        b.Entity<SealedProduct>(e =>
        {
            e.ToTable("sealed_products", t =>
            {
                // UPC-A (12 digits) or EAN-13 (13 digits)
                t.HasCheckConstraint("CK_sealed_products_upc", "upc IS NULL OR upc ~ '^\\d{12,13}$'");
            });
            e.HasKey(s => s.Identifier);
            e.Property(s => s.Identifier).HasColumnName("identifier").HasMaxLength(100);
            e.Property(s => s.SetCode).HasColumnName("set_code").HasMaxLength(4).IsRequired();
            e.Property(s => s.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
            e.Property(s => s.CategorySlug).HasColumnName("category_slug").HasMaxLength(100);
            e.Property(s => s.SubTypeSlug).HasColumnName("sub_type_slug").HasMaxLength(100);
            e.Property(s => s.Upc).HasColumnName("upc").HasMaxLength(13);
            e.Property(s => s.CurrentMarketValue).HasColumnName("current_market_value").HasPrecision(10, 2);
            e.Property(s => s.ImagePath).HasColumnName("image_path").HasMaxLength(500);
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
            e.HasOne<Set>().WithMany().HasForeignKey(s => s.SetCode);
            // Nullable FK to taxonomy - SET NULL on taxonomy replacement
            e.HasOne<SealedProductCategory>().WithMany().HasForeignKey(s => s.CategorySlug)
                .IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<SealedProductSubType>().WithMany().HasForeignKey(s => s.SubTypeSlug)
                .IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(s => s.SetCode);
            e.HasIndex(s => s.CategorySlug);
        });
    }

    private static void ConfigureCards(ModelBuilder b)
    {
        b.Entity<Card>(e =>
        {
            e.ToTable("cards", t =>
            {
                // Card identifier validation:
                // - Format: 3-4 alphanumeric chars (set code) + exactly 3 digits OR exactly 4 digits starting with 1-9 + optional trailing letter
                // - Single unambiguous alternation avoids false positives on 4-char set codes ending in a digit (e.g. oc20, pm20)
                t.HasCheckConstraint("CK_cards_identifier",
                    @"identifier ~ '^[a-z0-9]{3,4}([0-9]{3}|[1-9][0-9]{3})[a-z]?$'");
            });
            e.HasKey(c => c.Identifier);
            e.Property(c => c.Identifier).HasColumnName("identifier").HasMaxLength(9);
            e.Property(c => c.SetCode).HasColumnName("set_code").HasMaxLength(4).IsRequired();
            e.Property(c => c.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
            e.Property(c => c.ManaCost).HasColumnName("mana_cost").HasMaxLength(100);
            e.Property(c => c.Cmc).HasColumnName("cmc").HasPrecision(6, 2);
            e.Property(c => c.Color).HasColumnName("color").HasMaxLength(20);
            e.Property(c => c.ColorIdentity).HasColumnName("color_identity").HasMaxLength(20);
            e.Property(c => c.Keywords).HasColumnName("keywords").HasMaxLength(500);
            e.Property(c => c.CardType).HasColumnName("card_type").HasMaxLength(200);
            e.Property(c => c.OracleText).HasColumnName("oracle_text");
            e.Property(c => c.Layout).HasColumnName("layout").HasMaxLength(50);
            e.Property(c => c.OracleRulingUrl).HasColumnName("oracle_ruling_url").HasMaxLength(500);
            e.Property(c => c.CurrentMarketValue).HasColumnName("current_market_value")
                .HasPrecision(10, 2);
            e.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired();
            e.Property(c => c.IsReserved).HasColumnName("is_reserved").HasDefaultValue(false);
            e.Property(c => c.Rarity).HasColumnName("rarity").HasMaxLength(20);
            e.Property(c => c.FlavorText).HasColumnName("flavor_text").HasMaxLength(1000);
            e.Property(c => c.ValidTreatments).HasColumnName("valid_treatments").HasMaxLength(500);
            e.HasOne<Set>().WithMany().HasForeignKey(c => c.SetCode);
            e.HasIndex(c => c.SetCode);
        });
    }

    private static void ConfigureCardPrices(ModelBuilder b)
    {
        b.Entity<CardPrice>(e =>
        {
            e.ToTable("card_prices");
            e.HasKey(p => new { p.CardIdentifier, p.TreatmentKey });
            e.Property(p => p.CardIdentifier).HasColumnName("card_identifier").HasMaxLength(9);
            e.Property(p => p.TreatmentKey).HasColumnName("treatment_key").HasMaxLength(50);
            e.Property(p => p.PriceUsd).HasColumnName("price_usd").HasPrecision(10, 2);
            e.Property(p => p.CapturedAt).HasColumnName("captured_at").IsRequired();
            e.HasOne<Card>().WithMany().HasForeignKey(p => p.CardIdentifier);
            e.HasOne<Treatment>().WithMany().HasForeignKey(p => p.TreatmentKey);
            e.HasIndex(p => p.CardIdentifier);
        });
    }

    // Card identifier constraint for foreign-keyed columns:
    // Format: 3-4 alphanumeric (set code) + exactly 3 digits OR exactly 4 digits starting with 1-9 + optional trailing letter
    // Single unambiguous alternation avoids false positives on 4-char set codes ending in a digit (e.g. oc20, pm20)
    private static string CardIdentifierConstraint =>
        @"card_identifier ~ '^[a-z0-9]{3,4}([0-9]{3}|[1-9][0-9]{3})[a-z]?$'";

    private static void ConfigureCollectionEntries(ModelBuilder b)
    {
        b.Entity<CollectionEntry>(e =>
        {
            e.ToTable("collection_entries", t =>
            {
                t.HasCheckConstraint("CK_collection_entries_card_identifier", CardIdentifierConstraint);
                t.HasCheckConstraint("CK_collection_entries_quantity", "quantity > 0");
            });
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.UserId).HasColumnName("user_id").IsRequired();
            e.Property(c => c.CardIdentifier).HasColumnName("card_identifier").HasMaxLength(9).IsRequired();
            e.Property(c => c.TreatmentKey).HasColumnName("treatment_key").HasMaxLength(50).IsRequired();
            e.Property(c => c.Quantity).HasColumnName("quantity").IsRequired();
            e.Property(c => c.Condition).HasColumnName("condition")
                .HasConversion<string>().HasMaxLength(10).IsRequired();
            e.Property(c => c.Autographed).HasColumnName("autographed").IsRequired();
            e.Property(c => c.AcquisitionDate).HasColumnName("acquisition_date").IsRequired();
            e.Property(c => c.AcquisitionPrice).HasColumnName("acquisition_price")
                .HasPrecision(10, 2).IsRequired();
            e.Property(c => c.Notes).HasColumnName("notes").HasMaxLength(1000);
            e.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired();
            e.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId);
            e.HasOne(c => c.Treatment).WithMany().HasForeignKey(c => c.TreatmentKey);
            e.HasIndex(c => c.UserId);
            e.HasIndex(c => c.CardIdentifier);
        });
    }

    private static void ConfigureSerializedEntries(ModelBuilder b)
    {
        b.Entity<SerializedEntry>(e =>
        {
            e.ToTable("serialized_entries", t =>
            {
                t.HasCheckConstraint("CK_serialized_entries_card_identifier", CardIdentifierConstraint);
            });
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
            e.Property(s => s.CardIdentifier).HasColumnName("card_identifier").HasMaxLength(9).IsRequired();
            e.Property(s => s.TreatmentKey).HasColumnName("treatment_key").HasMaxLength(50).IsRequired();
            e.Property(s => s.SerialNumber).HasColumnName("serial_number").IsRequired();
            e.Property(s => s.PrintRunTotal).HasColumnName("print_run_total").IsRequired();
            e.Property(s => s.Condition).HasColumnName("condition")
                .HasConversion<string>().HasMaxLength(10).IsRequired();
            e.Property(s => s.Autographed).HasColumnName("autographed").IsRequired();
            e.Property(s => s.AcquisitionDate).HasColumnName("acquisition_date").IsRequired();
            e.Property(s => s.AcquisitionPrice).HasColumnName("acquisition_price")
                .HasPrecision(10, 2).IsRequired();
            e.Property(s => s.Notes).HasColumnName("notes").HasMaxLength(1000);
            e.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId);
            e.HasOne(s => s.Treatment).WithMany().HasForeignKey(s => s.TreatmentKey);
            e.HasIndex(s => s.UserId);
        });
    }

    private static void ConfigureSlabEntries(ModelBuilder b)
    {
        b.Entity<SlabEntry>(e =>
        {
            e.ToTable("slab_entries", t =>
            {
                t.HasCheckConstraint("CK_slab_entries_card_identifier", CardIdentifierConstraint);
                // print_run_total must be present when serial_number is present
                t.HasCheckConstraint("CK_slab_entries_print_run_total",
                    "serial_number IS NULL OR print_run_total IS NOT NULL");
            });
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
            e.Property(s => s.CardIdentifier).HasColumnName("card_identifier").HasMaxLength(9).IsRequired();
            e.Property(s => s.TreatmentKey).HasColumnName("treatment_key").HasMaxLength(50).IsRequired();
            e.Property(s => s.GradingAgencyCode).HasColumnName("grading_agency_code").HasMaxLength(20).IsRequired();
            e.Property(s => s.Grade).HasColumnName("grade").HasMaxLength(50).IsRequired();
            e.Property(s => s.CertificateNumber).HasColumnName("certificate_number").HasMaxLength(100).IsRequired();
            e.Property(s => s.SerialNumber).HasColumnName("serial_number");
            e.Property(s => s.PrintRunTotal).HasColumnName("print_run_total");
            e.Property(s => s.Condition).HasColumnName("condition")
                .HasConversion<string>().HasMaxLength(10).IsRequired();
            e.Property(s => s.Autographed).HasColumnName("autographed").IsRequired();
            e.Property(s => s.AcquisitionDate).HasColumnName("acquisition_date").IsRequired();
            e.Property(s => s.AcquisitionPrice).HasColumnName("acquisition_price")
                .HasPrecision(10, 2).IsRequired();
            e.Property(s => s.Notes).HasColumnName("notes").HasMaxLength(1000);
            e.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId);
            e.HasOne(s => s.Treatment).WithMany().HasForeignKey(s => s.TreatmentKey);
            e.HasOne(s => s.GradingAgency).WithMany().HasForeignKey(s => s.GradingAgencyCode);
            e.HasIndex(s => s.UserId);
        });
    }

    private static void ConfigureSealedInventoryEntries(ModelBuilder b)
    {
        b.Entity<SealedInventoryEntry>(e =>
        {
            e.ToTable("sealed_inventory_entries", t =>
            {
                t.HasCheckConstraint("CK_sealed_inventory_entries_quantity", "quantity > 0");
                // sub_type_slug requires category_slug to be non-null
                t.HasCheckConstraint("CK_sealed_inventory_entries_sub_type_slug",
                    "sub_type_slug IS NULL OR category_slug IS NOT NULL");
            });
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
            e.Property(s => s.ProductIdentifier).HasColumnName("product_identifier").HasMaxLength(100).IsRequired();
            e.Property(s => s.Quantity).HasColumnName("quantity").IsRequired();
            e.Property(s => s.Condition).HasColumnName("condition")
                .HasConversion<string>().HasMaxLength(10).IsRequired();
            e.Property(s => s.AcquisitionDate).HasColumnName("acquisition_date").IsRequired();
            e.Property(s => s.AcquisitionPrice).HasColumnName("acquisition_price")
                .HasPrecision(10, 2).IsRequired();
            e.Property(s => s.Notes).HasColumnName("notes").HasMaxLength(1000);
            e.Property(s => s.CategorySlug).HasColumnName("category_slug").HasMaxLength(100);
            e.Property(s => s.SubTypeSlug).HasColumnName("sub_type_slug").HasMaxLength(100);
            e.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId);
            e.HasOne<SealedProductCategory>().WithMany().HasForeignKey(s => s.CategorySlug)
                .IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<SealedProductSubType>().WithMany().HasForeignKey(s => s.SubTypeSlug)
                .IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(s => s.UserId);
            e.HasIndex(s => s.CategorySlug);
        });
    }

    private static void ConfigureWishlistEntries(ModelBuilder b)
    {
        b.Entity<WishlistEntry>(e =>
        {
            e.ToTable("wishlist_entries", t =>
            {
                t.HasCheckConstraint("CK_wishlist_entries_card_identifier", CardIdentifierConstraint);
            });
            e.HasKey(w => w.Id);
            e.Property(w => w.Id).HasColumnName("id");
            e.Property(w => w.UserId).HasColumnName("user_id").IsRequired();
            e.Property(w => w.CardIdentifier).HasColumnName("card_identifier").HasMaxLength(9).IsRequired();
            e.Property(w => w.TreatmentKey).HasColumnName("treatment_key").HasMaxLength(50).IsRequired().HasDefaultValue("regular");
            e.Property(w => w.CreatedAt).HasColumnName("created_at").IsRequired();
            e.HasOne(w => w.User).WithMany().HasForeignKey(w => w.UserId);
            e.HasOne<Treatment>().WithMany().HasForeignKey(w => w.TreatmentKey);
            e.HasIndex(w => w.UserId);
            e.HasIndex(w => new { w.UserId, w.CardIdentifier, w.TreatmentKey }).IsUnique();
        });
    }

    private static void ConfigureUpdateVersions(ModelBuilder b)
    {
        b.Entity<UpdateVersion>(e =>
        {
            e.ToTable("update_versions");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id");
            e.Property(u => u.ContentVersion).HasColumnName("content_version").HasMaxLength(100).IsRequired();
            e.Property(u => u.SchemaVersion).HasColumnName("schema_version").HasMaxLength(100);
            e.Property(u => u.ApplicationVersion).HasColumnName("application_version").HasMaxLength(100);
            e.Property(u => u.AppliedAt).HasColumnName("applied_at").IsRequired();
        });
    }

    private static void ConfigurePendingSchemaUpdates(ModelBuilder b)
    {
        b.Entity<PendingSchemaUpdate>(e =>
        {
            e.ToTable("pending_schema_updates");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.SchemaVersion).HasColumnName("schema_version").HasMaxLength(100).IsRequired();
            e.Property(p => p.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();
            e.Property(p => p.DownloadUrl).HasColumnName("download_url").HasMaxLength(500).IsRequired();
            e.Property(p => p.ZipSha256).HasColumnName("zip_sha256").HasMaxLength(64).IsRequired();
            e.Property(p => p.DetectedAt).HasColumnName("detected_at").IsRequired();
            e.Property(p => p.IsApproved).HasColumnName("is_approved").IsRequired();
            e.Property(p => p.ApprovedAt).HasColumnName("approved_at");
            e.Property(p => p.ApprovedByUserId).HasColumnName("approved_by_user_id");
        });
    }

    private static void ConfigureAdminNotifications(ModelBuilder b)
    {
        b.Entity<AdminNotification>(e =>
        {
            e.ToTable("admin_notifications");
            e.HasKey(n => n.Id);
            e.Property(n => n.Id).HasColumnName("id");
            e.Property(n => n.Message).HasColumnName("message").HasMaxLength(2000).IsRequired();
            e.Property(n => n.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
            e.Property(n => n.IsRead).HasColumnName("is_read").IsRequired();
            e.Property(n => n.CreatedAt).HasColumnName("created_at").IsRequired();
        });
    }

    private static void ConfigureAppSettings(ModelBuilder b)
    {
        b.Entity<AppSetting>(e =>
        {
            e.ToTable("app_settings");
            e.HasKey(s => s.Key);
            e.Property(s => s.Key).HasColumnName("key").HasMaxLength(100);
            e.Property(s => s.Value).HasColumnName("value").HasMaxLength(500).IsRequired();
        });
    }

    private static void ConfigureUserExportFiles(ModelBuilder b)
    {
        b.Entity<UserExportFile>(e =>
        {
            e.ToTable("user_export_files");
            e.HasKey(f => f.Id);
            e.Property(f => f.Id).HasColumnName("id");
            e.Property(f => f.UserId).HasColumnName("user_id").IsRequired();
            e.Property(f => f.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
            e.Property(f => f.RemovedAt).HasColumnName("removed_at").IsRequired();
            e.Property(f => f.FilePath).HasColumnName("file_path").HasMaxLength(1000).IsRequired();
            e.Property(f => f.FileSizeBytes).HasColumnName("file_size_bytes").IsRequired();
            e.Property(f => f.CreatedAt).HasColumnName("created_at").IsRequired();
            e.HasIndex(f => f.UserId);
        });
    }

    private static void ConfigureUserInvitations(ModelBuilder b)
    {
        b.Entity<UserInvitation>(e =>
        {
            e.ToTable("user_invitations");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).HasColumnName("id");
            e.Property(i => i.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
            e.Property(i => i.Token).HasColumnName("token").HasMaxLength(64).IsRequired();
            e.Property(i => i.Role).HasColumnName("role")
                .HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(i => i.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
            e.Property(i => i.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(i => i.ExpiresAt).HasColumnName("expires_at").IsRequired();
            e.Property(i => i.UsedAt).HasColumnName("used_at");
            e.Property(i => i.UsedByUserId).HasColumnName("used_by_user_id");
            e.HasIndex(i => i.Token).IsUnique();
        });
    }

    private static void ConfigureBackupRecords(ModelBuilder b)
    {
        b.Entity<BackupRecord>(e =>
        {
            e.ToTable("backup_records");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id");
            e.Property(r => r.Label).HasColumnName("label").HasMaxLength(300).IsRequired();
            e.Property(r => r.BackupType).HasColumnName("backup_type")
                .HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(r => r.SchemaVersion).HasColumnName("schema_version").IsRequired();
            e.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(r => r.FileSizeBytes).HasColumnName("file_size_bytes").IsRequired();
            e.Property(r => r.IsAvailable).HasColumnName("is_available").IsRequired();
            e.HasMany(r => r.Destinations)
                .WithOne()
                .HasForeignKey(d => d.BackupRecordId);
        });
    }

    private static void ConfigureBackupDestinationRecords(ModelBuilder b)
    {
        b.Entity<BackupDestinationRecord>(e =>
        {
            e.ToTable("backup_destination_records");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id");
            e.Property(r => r.BackupRecordId).HasColumnName("backup_record_id").IsRequired();
            e.Property(r => r.DestinationType).HasColumnName("destination_type").HasMaxLength(50).IsRequired();
            e.Property(r => r.DestinationLabel).HasColumnName("destination_label").HasMaxLength(200).IsRequired();
            e.Property(r => r.Success).HasColumnName("success").IsRequired();
            e.Property(r => r.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
            e.Property(r => r.AttemptedAt).HasColumnName("attempted_at").IsRequired();
        });
    }

    private static void ConfigureBackupDestinationConfigs(ModelBuilder b)
    {
        b.Entity<BackupDestinationConfig>(e =>
        {
            e.ToTable("backup_destination_configs");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.DestinationType).HasColumnName("destination_type").HasMaxLength(50).IsRequired();
            e.Property(c => c.Label).HasColumnName("label").HasMaxLength(200).IsRequired();
            e.Property(c => c.ConfigurationJson).HasColumnName("configuration_json").HasMaxLength(4000).IsRequired();
            e.Property(c => c.IsActive).HasColumnName("is_active").IsRequired();
        });
    }

    private static void SeedGradingAgencies(ModelBuilder b)
    {
        b.Entity<GradingAgency>().HasData(
            new GradingAgency
            {
                Code = "bgs",
                FullName = "Beckett Grading Services",
                ValidationUrlTemplate = "https://www.beckett.com/grading",
                SupportsDirectLookup = true,
                Source = AgencySource.Canonical,
                Active = true
            },
            new GradingAgency
            {
                Code = "psa",
                FullName = "Professional Sports Authenticator",
                ValidationUrlTemplate = "https://www.psacard.com/cert/{0}",
                SupportsDirectLookup = true,
                Source = AgencySource.Canonical,
                Active = true
            },
            new GradingAgency
            {
                Code = "sgc",
                FullName = "Sportscard Guaranty",
                ValidationUrlTemplate = "https://www.sgccard.com/cert/{0}",
                SupportsDirectLookup = true,
                Source = AgencySource.Canonical,
                Active = true
            },
            new GradingAgency
            {
                Code = "cgc",
                FullName = "Certified Guaranty Company",
                ValidationUrlTemplate = "https://www.cgccards.com/certlookup/{0}",
                SupportsDirectLookup = true,
                Source = AgencySource.Canonical,
                Active = true
            },
            new GradingAgency
            {
                Code = "ccc",
                FullName = "Certificateur de Cartes de Collection",
                ValidationUrlTemplate = "https://cccgrading.com/en/ccc-card-verification",
                SupportsDirectLookup = false,
                Source = AgencySource.Canonical,
                Active = true
            },
            new GradingAgency
            {
                Code = "isa",
                FullName = "International Sports Authentication",
                ValidationUrlTemplate = "https://www.isagrading.com/verify/{0}",
                SupportsDirectLookup = true,
                Source = AgencySource.Canonical,
                Active = true
            }
        );
    }
}
