using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CountOrSell.Api;

// Loads admin-managed values from the app_settings table and exposes them as
// IConfiguration entries during startup. Inserted as the lowest-priority
// configuration source so env vars and appsettings.json still override DB
// values (useful for dev and emergency recovery). Read-only; values are
// captured once at app build and require a restart to refresh.
public sealed class DbAppSettingsConfigurationSource : IConfigurationSource
{
    public required string ConnectionString { get; init; }
    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new DbAppSettingsConfigurationProvider(ConnectionString);
}

internal sealed class DbAppSettingsConfigurationProvider : ConfigurationProvider
{
    private readonly string _connectionString;

    public DbAppSettingsConfigurationProvider(string connectionString)
    {
        _connectionString = connectionString;
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
            // First-run, migrations not yet applied, or DB unreachable: leave map empty.
            // Auth handlers and other startup readers will treat the value as missing,
            // matching the behavior when no env var is set.
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
