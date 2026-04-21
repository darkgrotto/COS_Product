using CountOrSell.Domain.Models;

namespace CountOrSell.Domain.Services;

public interface IMetricsService
{
    Task<MetricsResult> GetMetricsAsync(Guid userId, CollectionFilter filter, CancellationToken ct = default);
    Task<MetricsResult> GetAggregateMetricsAsync(CollectionFilter filter, CancellationToken ct = default);
    Task<SetCompletionResult> GetSetCompletionAsync(Guid userId, string setCode, bool regularOnly, CancellationToken ct = default);
    Task<List<SetCompletionResult>> GetAllSetCompletionAsync(Guid userId, bool regularOnly, CollectionFilter? filter = null, CancellationToken ct = default);
    Task<(List<TopCardResult> Results, int TotalCount)> GetTopCardsAsync(Guid userId, string metric, int limit, int offset, CollectionFilter filter, CancellationToken ct = default);
}
