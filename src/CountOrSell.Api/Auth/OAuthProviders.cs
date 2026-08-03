using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;

namespace CountOrSell.Api.Auth;

// Central map between the lowercase provider ids used in routes, app_settings
// keys, and users.oauth_provider, and the ASP.NET authentication scheme names
// the handlers register under. Route ids and scheme names differ for three of
// the four providers (e.g. "google" vs "Google"), so challenges must always go
// through this map - never pass the route value to Challenge() directly.
public static class OAuthProviders
{
    // Intermediate cookie scheme the OAuth/OIDC handlers sign the external
    // principal into. Never grants access to [Authorize] endpoints - the
    // callback exchanges it for the main cookie only after the identity maps
    // to an active application user.
    public const string ExternalScheme = "External";

    private sealed record ProviderInfo(string Scheme, string DisplayName);

    private static readonly Dictionary<string, ProviderInfo> Map = new(StringComparer.Ordinal)
    {
        ["google"] = new(GoogleDefaults.AuthenticationScheme, "Google"),
        ["microsoft"] = new(MicrosoftAccountDefaults.AuthenticationScheme, "Microsoft"),
        ["microsoft-entra"] = new("microsoft-entra", "Microsoft (Entra ID)"),
        ["github"] = new(GitHubAuthenticationDefaults.AuthenticationScheme, "GitHub"),
    };

    public static IReadOnlyCollection<string> All => Map.Keys;

    public static bool TryGetScheme(string providerId, out string scheme)
    {
        if (Map.TryGetValue(providerId, out var info))
        {
            scheme = info.Scheme;
            return true;
        }
        scheme = string.Empty;
        return false;
    }

    public static string DisplayName(string providerId) =>
        Map.TryGetValue(providerId, out var info) ? info.DisplayName : providerId;
}
