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
}
