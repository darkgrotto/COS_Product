import { api } from './client';
import { CollectionEntry } from '../types/collection';
import { CollectionFilter, CardCondition } from '../types/filters';
import { MetricsResult, SetCompletionResult } from '../types/metrics';

export interface ReservedCollectionEntry {
  entryId: string;
  cardIdentifier: string;
  cardName: string;
  setCode: string;
  cardType: string | null;
  treatment: string;
  quantity: number;
  condition: string;
  autographed: boolean;
  acquisitionPrice: number;
  marketValue: number | null;
}

export interface CollectionEntryRequest {
  cardIdentifier: string;
  treatment: string;
  quantity: number;
  condition: CardCondition;
  autographed: boolean;
  acquisitionDate: string;
  acquisitionPrice: number;
  notes?: string;
}

function buildFilterQuery(filter?: CollectionFilter): string {
  if (!filter) return '';
  const params = new URLSearchParams();
  if (filter.setCode) params.set('setCode', filter.setCode);
  if (filter.color) params.set('color', filter.color);
  if (filter.condition) params.set('condition', filter.condition);
  if (filter.cardType) params.set('cardType', filter.cardType);
  if (filter.treatment) params.set('treatment', filter.treatment);
  if (filter.autographed !== undefined) params.set('autographed', String(filter.autographed));
  if (filter.serialized !== undefined) params.set('serialized', String(filter.serialized));
  if (filter.slabbed !== undefined) params.set('slabbed', String(filter.slabbed));
  if (filter.sealedProduct !== undefined) params.set('sealedProduct', String(filter.sealedProduct));
  if (filter.gradingAgency) params.set('gradingAgency', filter.gradingAgency);
  const qs = params.toString();
  return qs ? `?${qs}` : '';
}

export const collectionApi = {
  getAll: (filter?: CollectionFilter, userId?: string): Promise<CollectionEntry[]> => {
    const params = new URLSearchParams();
    if (filter?.setCode) params.set('setCode', filter.setCode);
    if (filter?.color) params.set('color', filter.color);
    if (filter?.condition) params.set('condition', filter.condition);
    if (filter?.cardType) params.set('cardType', filter.cardType);
    if (filter?.treatment) params.set('treatment', filter.treatment);
    if (filter?.autographed !== undefined) params.set('autographed', String(filter.autographed));
    if (userId) params.set('userId', userId);
    const qs = params.toString();
    return api.get<CollectionEntry[]>(`/api/collection${qs ? `?${qs}` : ''}`);
  },

  getById: (id: string): Promise<CollectionEntry> =>
    api.get<CollectionEntry>(`/api/collection/${id}`),

  create: (request: CollectionEntryRequest): Promise<CollectionEntry> =>
    api.post<CollectionEntry>('/api/collection', request),

  update: (id: string, request: CollectionEntryRequest): Promise<CollectionEntry> =>
    api.put<CollectionEntry>(`/api/collection/${id}`, request),

  delete: (id: string): Promise<void> =>
    api.delete<void>(`/api/collection/${id}`),

  adjustQuantity: (id: string, quantity: number): Promise<CollectionEntry> =>
    api.patch<CollectionEntry>(`/api/collection/${id}/quantity`, { quantity }),

  getMetrics: (filter?: CollectionFilter, userId?: string): Promise<MetricsResult> => {
    const params = new URLSearchParams();
    if (userId) params.set('userId', userId);
    const qs = params.toString();
    return api.get<MetricsResult>(`/api/collection/metrics${qs ? `?${qs}` : ''}`);
  },

  getSetCompletion: (regularOnly?: boolean): Promise<SetCompletionResult[]> => {
    const qs = regularOnly !== undefined ? `?regularOnly=${regularOnly}` : '';
    return api.get<SetCompletionResult[]>(`/api/collection/completion${qs}`);
  },

  getSetCompletionBySet: (setCode: string, regularOnly?: boolean): Promise<SetCompletionResult> => {
    const qs = regularOnly !== undefined ? `?regularOnly=${regularOnly}` : '';
    return api.get<SetCompletionResult>(`/api/collection/completion/${setCode}${qs}`);
  },

  refreshPrice: (cardIdentifier: string): Promise<void> =>
    api.post<void>(`/api/collection/refresh-price/${cardIdentifier}`),

  getReserved: (userId?: string): Promise<ReservedCollectionEntry[]> => {
    const params = userId ? `?userId=${userId}` : '';
    return api.get<ReservedCollectionEntry[]>(`/api/collection/reserved${params}`);
  },
};

export { buildFilterQuery };
