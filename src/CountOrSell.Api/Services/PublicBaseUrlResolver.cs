namespace CountOrSell.Api.Services;

// Resolves the canonical public base URL used in user-facing links (invite emails, etc).
//
// When PUBLIC_BASE_URL is configured, it is authoritative - this closes Host-header
// injection on outbound links because the value cannot be influenced by an incoming
// HTTP Host header. When unset, falls back to the request-derived URL for backwards
// compatibility on existing instances; admins should set PUBLIC_BASE_URL to opt into
// the protection.
public static class PublicBaseUrlResolver
{
    public const string ConfigKey = "PUBLIC_BASE_URL";

    public readonly record struct Result(string? BaseUrl, string? Error)
    {
        public bool Success => BaseUrl != null;
    }

    // Pure resolution from a configured value plus a fallback. Trims the trailing
    // slash so callers can confidently concatenate "/path/segment".
    public static Result Resolve(string? configuredValue, string fallbackBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
            return new Result(fallbackBaseUrl.TrimEnd('/'), null);

        if (!Uri.TryCreate(configuredValue, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new Result(
                null,
                $"{ConfigKey} is configured but is not a valid http or https URL.");
        }

        return new Result(configuredValue.TrimEnd('/'), null);
    }
}
