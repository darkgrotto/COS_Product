using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data.Repositories;

public class SealedProductRepository : ISealedProductRepository
{
    private readonly AppDbContext _db;
    public SealedProductRepository(AppDbContext db) => _db = db;

    public Task<Dictionary<string, SealedProduct>> GetByIdentifiersAsync(
        IEnumerable<string> identifiers, CancellationToken ct = default) =>
        _db.SealedProducts
            .Where(p => identifiers.Contains(p.Identifier))
            .ToDictionaryAsync(p => p.Identifier, ct);
}
