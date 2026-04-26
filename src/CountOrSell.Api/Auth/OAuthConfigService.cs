namespace CountOrSell.Api.Auth;

public class OAuthConfigService : IOAuthConfigService
{
    private readonly IConfiguration _config;

    public OAuthConfigService(IConfiguration config)
    {
        _config = config;
    }

    public bool IsConfigured(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "google" => !string.IsNullOrWhiteSpace(_config["OAuth:Google:ClientId"])
                     && !string.IsNullOrWhiteSpace(_config["OAuth:Google:ClientSecret"]),
            "microsoft" => !string.IsNullOrWhiteSpace(_config["OAuth:Microsoft:ClientId"])
                        && !string.IsNullOrWhiteSpace(_config["OAuth:Microsoft:ClientSecret"]),
            "microsoft-entra" => !string.IsNullOrWhiteSpace(_config["OAuth:MicrosoftEntra:ClientId"])
                              && !string.IsNullOrWhiteSpace(_config["OAuth:MicrosoftEntra:ClientSecret"])
                              && !string.IsNullOrWhiteSpace(_config["OAuth:MicrosoftEntra:TenantId"]),
            "github" => !string.IsNullOrWhiteSpace(_config["OAuth:GitHub:ClientId"])
                     && !string.IsNullOrWhiteSpace(_config["OAuth:GitHub:ClientSecret"]),
            _ => false
        };
    }
}
