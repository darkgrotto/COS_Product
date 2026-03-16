using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services;

public class TcgPlayerService : ITcgPlayerService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ICardRepository _cards;

    public TcgPlayerService(IConfiguration configuration, HttpClient httpClient, ICardRepository cards)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _cards = cards;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["TCGPLAYER_API_KEY"] ??
        Environment.GetEnvironmentVariable("TCGPLAYER_API_KEY"));

    public async Task<decimal?> GetMarketValueAsync(string cardIdentifier, CancellationToken ct = default)
    {
        if (!IsConfigured) return null;

        var apiKey = _configuration["TCGPLAYER_API_KEY"] ?? Environment.GetEnvironmentVariable("TCGPLAYER_API_KEY");

        try
        {
            // TCGPlayer API direct query - CountOrSell does NOT proxy this
            // Client calls this endpoint; this service calls TCGPlayer directly
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            // TCGPlayer API integration - implementation detail deferred
            // Once TCGPlayer API endpoint confirmed, replace with actual call
            await Task.CompletedTask;
            return null;
        }
        catch
        {
            return null;
        }
    }
}
