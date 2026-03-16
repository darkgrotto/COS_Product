import { api } from './client';
import { MetricsResult, SetCompletionResult } from '../types/metrics';
import { CollectionFilter } from '../types/filters';

function buildMetricsQuery(filter?: CollectionFilter, userId?: string): string {
  const params = new URLSearchParams();
  if (userId) params.set('userId', userId);
  if (filter?.setCode) params.set('setCode', filter.setCode);
  if (filter?.color) params.set('color', filter.color);
  if (filter?.condition) params.set('condition', filter.condition);
  if (filter?.cardType) params.set('cardType', filter.cardType);
  if (filter?.treatment) params.set('treatment', filter.treatment);
  if (filter?.autographed !== undefined) params.set('autographed', String(filter.autographed));
  const qs = params.toString();
  return qs ? `?${qs}` : '';
}

export const metricsApi = {
  getMetrics: (filter?: CollectionFilter, userId?: string): Promise<MetricsResult> =>
    api.get<MetricsResult>(`/api/collection/metrics${buildMetricsQuery(filter, userId)}`),

  getAggregateMetrics: (filter?: CollectionFilter): Promise<MetricsResult> =>
    api.get<MetricsResult>(`/api/collection/metrics/aggregate${buildMetricsQuery(filter)}`),

  getSetCompletion: (regularOnly?: boolean): Promise<SetCompletionResult[]> => {
    const qs = regularOnly !== undefined ? `?regularOnly=${regularOnly}` : '';
    return api.get<SetCompletionResult[]>(`/api/collection/completion${qs}`);
  },

  getSetCompletionBySet: (setCode: string, regularOnly?: boolean): Promise<SetCompletionResult> => {
    const qs = regularOnly !== undefined ? `?regularOnly=${regularOnly}` : '';
    return api.get<SetCompletionResult>(`/api/collection/completion/${setCode}${qs}`);
  },
};
