using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data.Repositories;

public class CardRepository : ICardRepository
{
    private readonly AppDbContext _db;
    public CardRepository(AppDbContext db) => _db = db;

    public Task<Card?> GetByIdentifierAsync(string identifier, CancellationToken ct = default) =>
        _db.Cards.FirstOrDefaultAsync(c => c.Identifier == identifier, ct);

    public Task<List<Card>> SearchByNameAsync(string query, CancellationToken ct = default) =>
        _db.Cards
            .Where(c => EF.Functions.ILike(c.Name, $"%{query}%"))
            .OrderBy(c => c.Name)
            .Take(20)
            .ToListAsync(ct);

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
