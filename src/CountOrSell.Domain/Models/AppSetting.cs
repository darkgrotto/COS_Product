namespace CountOrSell.Domain.Models;

// Key-value store for application settings.
// Keys include: "current_schema_version", "latest_app_version"
public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
