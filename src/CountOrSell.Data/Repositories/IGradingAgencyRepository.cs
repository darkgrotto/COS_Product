using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface IGradingAgencyRepository
{
    Task<List<GradingAgency>> GetAllAsync(CancellationToken ct = default);
    Task<GradingAgency?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<GradingAgency> CreateAsync(GradingAgency agency, CancellationToken ct = default);
    Task<GradingAgency> UpdateAsync(GradingAgency agency, CancellationToken ct = default);
    Task DeleteAsync(string code, CancellationToken ct = default);
}
