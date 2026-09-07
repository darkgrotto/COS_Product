using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CountOrSell.Api;

// Loads admin-managed values from the app_settings table and exposes them as
// IConfiguration entries. Inserted as the lowest-priority configuration source
// so env vars and appsettings.json still override DB values (useful for dev
// and emergency recovery). Values are captured at app build and refreshed when
// DbAppSettingsReloader.Reload() is called (the settings endpoints trigger it
// after saving), so admin UI changes take effect without a restart.
public sealed class DbAppSettingsConfigurationSource : IConfigurationSource
{
    public required string ConnectionString { get; init; }
    public DbAppSettingsReloader? Reloader { get; init; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var provider = new DbAppSettingsConfigurationProvider(ConnectionString);
        Reloader?.Attach(provider);
        return provider;
    }
}

// DI-visible handle for re-reading app_settings into IConfiguration at
// runtime. Raising the configuration reload token also rebuilds any options
// wired to it via ConfigurationChangeTokenSource (the OAuth handler options).
public sealed class DbAppSettingsReloader
{
    private DbAppSettingsConfigurationProvider? _provider;

    internal void Attach(DbAppSettingsConfigurationProvider provider) => _provider = provider;

    public void Reload() => _provider?.Reload();
}

internal sealed class DbAppSettingsConfigurationProvider : ConfigurationProvider
{
    private readonly string _connectionString;

    public DbAppSettingsConfigurationProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void Reload()
    {
        Load();
        OnReload();
    }

    public override void Load()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT key, value FROM app_settings", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var key = reader.GetString(0);
                var value = reader.IsDBNull(1) ? null : reader.GetString(1);
                if (DbKeyToConfigKey(key) is { } configKey)
                    data[configKey] = value;
            }
        }
        catch
        {
            // First-run, migrations not yet applied, or DB unreachable: leave the
            // previous map in place on reload (empty on first load). Auth handlers
            // and other readers treat missing values like an unset env var.
            return;
        }
        Data = data;
    }

    private static string? DbKeyToConfigKey(string dbKey) => dbKey switch
    {
        "oauth_google_client_id"              => "OAuth:Google:ClientId",
        "oauth_google_client_secret"          => "OAuth:Google:ClientSecret",
        "oauth_microsoft_client_id"           => "OAuth:Microsoft:ClientId",
        "oauth_microsoft_client_secret"       => "OAuth:Microsoft:ClientSecret",
        "oauth_microsoft_entra_client_id"     => "OAuth:MicrosoftEntra:ClientId",
        "oauth_microsoft_entra_client_secret" => "OAuth:MicrosoftEntra:ClientSecret",
        "oauth_microsoft_entra_tenant_id"     => "OAuth:MicrosoftEntra:TenantId",
        "oauth_github_client_id"              => "OAuth:GitHub:ClientId",
        "oauth_github_client_secret"          => "OAuth:GitHub:ClientSecret",
        "tcgplayer_api_key"                   => "TCGPLAYER_API_KEY",
        _                                     => null,
    };
}
