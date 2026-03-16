using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<Treatment> Treatments => Set<Treatment>();
    public DbSet<GradingAgency> GradingAgencies => Set<GradingAgency>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Set> Sets => Set<Set>();
    public DbSet<SealedProduct> SealedProducts => Set<SealedProduct>();
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureUserPreferences(modelBuilder);
        ConfigureTreatments(modelBuilder);
        ConfigureGradingAgencies(modelBuilder);
        ConfigureSets(modelBuilder);
        ConfigureCards(modelBuilder);
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
            e.Property(s => s.ReleaseDate).HasColumnName("release_date");
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });
    }

    private static void ConfigureSealedProducts(ModelBuilder b)
    {
        b.Entity<SealedProduct>(e =>
        {
            e.ToTable("sealed_products");
            e.HasKey(s => s.Identifier);
            e.Property(s => s.Identifier).HasColumnName("identifier").HasMaxLength(100);
            e.Property(s => s.SetCode).HasColumnName("set_code").HasMaxLength(4).IsRequired();
            e.Property(s => s.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
            e.Property(s => s.ImagePath).HasColumnName("image_path").HasMaxLength(500);
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
            e.HasOne<Set>().WithMany().HasForeignKey(s => s.SetCode);
            e.HasIndex(s => s.SetCode);
        });
    }

    private static void ConfigureCards(ModelBuilder b)
    {
        b.Entity<Card>(e =>
        {
            e.ToTable("cards", t =>
            {
                // Card identifier validation:
                // - Basic format: 3-4 alphanumeric chars (set code) + 3-4 digits (numeric suffix)
                // - Four-digit suffix must be >= 1000 (no zero-padded four-digit suffixes like "0123")
                t.HasCheckConstraint("CK_cards_identifier",
                    @"identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}$' AND identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}$'");
            });
            e.HasKey(c => c.Identifier);
            e.Property(c => c.Identifier).HasColumnName("identifier").HasMaxLength(8);
            e.Property(c => c.SetCode).HasColumnName("set_code").HasMaxLength(4).IsRequired();
            e.Property(c => c.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
            e.Property(c => c.Color).HasColumnName("color").HasMaxLength(20);
            e.Property(c => c.CardType).HasColumnName("card_type").HasMaxLength(100);
            e.Property(c => c.OracleRulingUrl).HasColumnName("oracle_ruling_url").HasMaxLength(500);
            e.Property(c => c.CurrentMarketValue).HasColumnName("current_market_value")
                .HasPrecision(10, 2);
            e.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired();
            e.HasOne<Set>().WithMany().HasForeignKey(c => c.SetCode);
            e.HasIndex(c => c.SetCode);
        });
    }

    // Card identifier constraint for foreign-keyed columns:
    // Basic format: 3-4 alphanumeric (set code) + 3-4 digits (suffix)
    // Four-digit suffix must be >= 1000 (no zero-padded four-digit suffixes like "0123")
    private static string CardIdentifierConstraint =>
        @"card_identifier ~ '^[a-z0-9]{3,4}[0-9]{3,4}$' AND card_identifier !~ '^[a-z0-9]{3,4}0[0-9]{3}$'";

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
            e.Property(c => c.CardIdentifier).HasColumnName("card_identifier").HasMaxLength(8).IsRequired();
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
            e.Property(s => s.CardIdentifier).HasColumnName("card_identifier").HasMaxLength(8).IsRequired();
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
            e.Property(s => s.CardIdentifier).HasColumnName("card_identifier").HasMaxLength(8).IsRequired();
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
            e.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId);
            e.HasIndex(s => s.UserId);
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
            e.Property(w => w.CardIdentifier).HasColumnName("card_identifier").HasMaxLength(8).IsRequired();
            e.Property(w => w.CreatedAt).HasColumnName("created_at").IsRequired();
            e.HasOne(w => w.User).WithMany().HasForeignKey(w => w.UserId);
            e.HasIndex(w => w.UserId);
            e.HasIndex(w => new { w.UserId, w.CardIdentifier }).IsUnique();
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
