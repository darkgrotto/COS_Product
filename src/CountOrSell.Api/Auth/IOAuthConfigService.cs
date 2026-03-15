namespace CountOrSell.Api.Auth;

public interface IOAuthConfigService
{
    bool IsConfigured(string provider);
}
