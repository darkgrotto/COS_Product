using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services;

public class ScryfallCardImageFetcher : ICardImageFetcher
{
    private readonly HttpClient _http;
    private readonly ILogger<ScryfallCardImageFetcher> _logger;

    // Card identifier pattern: {setCode}{number} e.g. "eoe019", "3ed019"
    // Set code is 3-4 alphanumeric chars; number is 3-4 digits.
    private static readonly System.Text.RegularExpressions.Regex IdentifierRegex =
        new(@"^([a-z0-9]{3,4})(\d{3,4})$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public ScryfallCardImageFetcher(HttpClient http, ILogger<ScryfallCardImageFetcher> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<byte[]?> FetchAsync(string cardIdentifier, CancellationToken ct)
    {
        var match = IdentifierRegex.Match(cardIdentifier.ToLowerInvariant());
        if (!match.Success)
        {
            _logger.LogWarning("Cannot parse card identifier for Scryfall fetch: {Identifier}", cardIdentifier);
            return null;
        }

        var setCode = match.Groups[1].Value;
        var number = match.Groups[2].Value.TrimStart('0');
        if (string.IsNullOrEmpty(number)) number = "0";

        var url = $"https://api.scryfall.com/cards/{setCode}/{number}?format=image&version=normal";

        try
        {
            var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Scryfall returned {Status} for {Identifier}",
                    response.StatusCode,
                    cardIdentifier);
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch image from Scryfall for {Identifier}", cardIdentifier);
            return null;
        }
    }
}
