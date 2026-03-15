using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data.Repositories;

public class GradingAgencyRepository : IGradingAgencyRepository
{
    private readonly AppDbContext _db;
    public GradingAgencyRepository(AppDbContext db) => _db = db;

    public Task<List<GradingAgency>> GetAllAsync(CancellationToken ct = default) =>
        _db.GradingAgencies.ToListAsync(ct);

    public Task<GradingAgency?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _db.GradingAgencies.FirstOrDefaultAsync(a => a.Code == code, ct);

    public async Task<GradingAgency> CreateAsync(GradingAgency agency, CancellationToken ct = default)
    {
        _db.GradingAgencies.Add(agency);
        await _db.SaveChangesAsync(ct);
        return agency;
    }

    public async Task<GradingAgency> UpdateAsync(GradingAgency agency, CancellationToken ct = default)
    {
        _db.GradingAgencies.Update(agency);
        await _db.SaveChangesAsync(ct);
        return agency;
    }

    public async Task DeleteAsync(string code, CancellationToken ct = default)
    {
        var agency = await _db.GradingAgencies.FindAsync(new object[] { code }, ct);
        if (agency != null)
        {
            _db.GradingAgencies.Remove(agency);
            await _db.SaveChangesAsync(ct);
        }
    }
}
