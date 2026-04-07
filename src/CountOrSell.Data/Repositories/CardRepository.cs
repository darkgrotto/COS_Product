using System.Text.RegularExpressions;
using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data.Repositories;

public class CardRepository : ICardRepository
{
    private readonly AppDbContext _db;
    public CardRepository(AppDbContext db) => _db = db;

    public Task<Card?> GetByIdentifierAsync(string identifier, CancellationToken ct = default) =>
        _db.Cards.FirstOrDefaultAsync(c => c.Identifier == identifier, ct);

    // Matches queries that look like a card identifier prefix: 3-4 alphanumeric set code
    // followed by one or more digits and an optional trailing letter (e.g. "EOE123", "EOE12", "eoe123a").
    private static readonly Regex IdentifierQueryRegex =
        new(@"^[a-z0-9]{3,4}\d+[a-z]?$", RegexOptions.Compiled);

    public Task<List<Card>> SearchByNameAsync(string query, CancellationToken ct = default) =>
        _db.Cards
            .Where(c => EF.Functions.ILike(c.Name, $"%{query}%"))
            .OrderBy(c => c.Name)
            .Take(20)
            .ToListAsync(ct);

    public Task<List<Card>> SearchAsync(string query, string? setCode = null, CancellationToken ct = default)
    {
        var q = query.Trim().ToLowerInvariant();
        var setFilter = setCode?.ToLowerInvariant();

        IQueryable<Card> baseQuery = _db.Cards;
        if (!string.IsNullOrEmpty(setFilter))
            baseQuery = baseQuery.Where(c => c.SetCode == setFilter);

        // Identifier-pattern query: prefix-match against identifier column
        // Returns all variants (e.g. eoe123, eoe123a, eoe123b) ordered by identifier
        if (IdentifierQueryRegex.IsMatch(q))
        {
            return baseQuery
                .Where(c => c.Identifier.StartsWith(q))
                .OrderBy(c => c.Identifier)
                .Take(20)
                .ToListAsync(ct);
        }

        // Name search: case-insensitive substring match, ordered by name
        return baseQuery
            .Where(c => EF.Functions.ILike(c.Name, $"%{q}%"))
            .OrderBy(c => c.Name)
            .ThenBy(c => c.SetCode)
            .Take(20)
            .ToListAsync(ct);
    }

    public async Task<Card> UpdateAsync(Card card, CancellationToken ct = default)
    {
        _db.Cards.Update(card);
        await _db.SaveChangesAsync(ct);
        return card;
    }

    public Task<List<Card>> GetBySetCodeAsync(string setCode, CancellationToken ct = default) =>
        _db.Cards.Where(c => c.SetCode == setCode).ToListAsync(ct);

    public Task<List<string>> GetReservedIdentifiersAsync(CancellationToken ct = default) =>
        _db.Cards
            .Where(c => c.IsReserved)
            .Select(c => c.Identifier)
            .ToListAsync(ct);

    public Task<Dictionary<string, string?>> GetOracleRulingUrlsByIdentifiersAsync(
        IEnumerable<string> identifiers, CancellationToken ct = default) =>
        _db.Cards
            .Where(c => identifiers.Contains(c.Identifier))
            .ToDictionaryAsync(c => c.Identifier, c => c.OracleRulingUrl, ct);

    public Task<Dictionary<string, decimal?>> GetMarketValuesByIdentifiersAsync(
        IEnumerable<string> identifiers, CancellationToken ct = default) =>
        _db.Cards
            .Where(c => identifiers.Contains(c.Identifier))
            .ToDictionaryAsync(c => c.Identifier, c => c.CurrentMarketValue, ct);

    public async Task<Dictionary<string, (string Name, decimal? MarketValue, string SetCode)>> GetSummaryByIdentifiersAsync(
        IEnumerable<string> identifiers, CancellationToken ct = default)
    {
        var list = await _db.Cards
            .Where(c => identifiers.Contains(c.Identifier))
            .Select(c => new { c.Identifier, c.Name, c.CurrentMarketValue, c.SetCode })
            .ToListAsync(ct);
        return list.ToDictionary(
            c => c.Identifier,
            c => (c.Name, c.CurrentMarketValue, c.SetCode));
    }

    public Task<Card?> GetRandomWithFlavorTextAsync(CancellationToken ct = default) =>
        _db.Cards
            .Where(c => c.FlavorText != null)
            .OrderBy(_ => EF.Functions.Random())
            .FirstOrDefaultAsync(ct);
}
