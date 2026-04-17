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

    public Task<List<SealedProduct>> GetAllAsync(CancellationToken ct = default) =>
        _db.SealedProducts.OrderBy(p => p.Name).ToListAsync(ct);

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

    public async Task<(List<SealedProduct> Items, int TotalCount)> BrowseAsync(
        string? setCode, string? categorySlug, string? subTypeSlug,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.SealedProducts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(setCode))
            query = query.Where(p => p.SetCode == setCode.ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(categorySlug))
            query = query.Where(p => p.CategorySlug == categorySlug);

        if (!string.IsNullOrWhiteSpace(subTypeSlug))
            query = query.Where(p => p.SubTypeSlug == subTypeSlug);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
