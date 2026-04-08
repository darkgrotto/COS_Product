using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data.Repositories;

public class SealedProductRepository : ISealedProductRepository
{
    private readonly AppDbContext _db;
    public SealedProductRepository(AppDbContext db) => _db = db;

    public Task<SealedProduct?> GetByIdentifierAsync(string identifier, CancellationToken ct = default) =>
        _db.SealedProducts.FirstOrDefaultAsync(p => p.Identifier == identifier, ct);

    public Task<Dictionary<string, SealedProduct>> GetByIdentifiersAsync(
        IEnumerable<string> identifiers, CancellationToken ct = default) =>
        _db.SealedProducts
            .Where(p => identifiers.Contains(p.Identifier))
            .ToDictionaryAsync(p => p.Identifier, ct);

    public Task<List<SealedProduct>> SearchAsync(string query, CancellationToken ct = default)
    {
        var q = query.Trim();
        return _db.SealedProducts
            .Where(p => EF.Functions.ILike(p.Name, $"%{q}%") ||
                        EF.Functions.ILike(p.Identifier, $"%{q}%"))
            .OrderBy(p => p.Name)
            .Take(20)
            .ToListAsync(ct);
    }
}
