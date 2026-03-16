namespace CountOrSell.Domain;

public static class BackupScope
{
    // Tables included in every backup
    public static readonly IReadOnlyList<string> Tables = new[]
    {
        "users",
        "user_preferences",
        "collection_entries",
        "serialized_entries",
        "slab_entries",
        "sealed_inventory_entries",
        "wishlist_entries",
        "app_settings",
        "user_export_files",
        "admin_notifications",
        "grading_agencies",
        "backup_records",
        "backup_destination_records",
        "backup_destination_configs",
    };

    // Tables explicitly excluded (canonical reference data)
    public static readonly IReadOnlyList<string> ExcludedTables = new[]
    {
        "cards",
        "sets",
        "treatments",
        "sealed_products",
        "update_versions",
        "pending_schema_updates",
    };
}
