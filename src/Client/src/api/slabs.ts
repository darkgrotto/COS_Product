import { api } from './client';
import { SlabEntry } from '../types/collection';
import { CardCondition } from '../types/filters';

export interface SlabEntryRequest {
  cardIdentifier: string;
  treatment: string;
  gradingAgencyCode: string;
  grade: string;
  certificateNumber: string;
  serialNumber?: number;
  printRunTotal?: number;
  condition: CardCondition;
  autographed: boolean;
  acquisitionDate: string;
  acquisitionPrice: number;
  notes?: string;
}

export const slabsApi = {
  getAll: (userId?: string, filter?: { setCode?: string; treatment?: string; condition?: string; autographed?: boolean; gradingAgency?: string }): Promise<SlabEntry[]> => {
    const params = new URLSearchParams();
    if (userId) params.set('userId', userId);
    if (filter?.setCode) params.set('setCode', filter.setCode);
    if (filter?.treatment) params.set('treatment', filter.treatment);
    if (filter?.condition) params.set('condition', filter.condition);
    if (filter?.autographed !== undefined) params.set('autographed', String(filter.autographed));
    if (filter?.gradingAgency) params.set('gradingAgency', filter.gradingAgency);
    const qs = params.toString() ? `?${params.toString()}` : '';
    return api.get<SlabEntry[]>(`/api/slabs${qs}`);
  },

  getById: (id: string): Promise<SlabEntry> =>
    api.get<SlabEntry>(`/api/slabs/${id}`),

  create: (request: SlabEntryRequest): Promise<SlabEntry> =>
    api.post<SlabEntry>('/api/slabs', request),

  update: (id: string, request: SlabEntryRequest): Promise<SlabEntry> =>
    api.put<SlabEntry>(`/api/slabs/${id}`, request),

  delete: (id: string): Promise<void> =>
    api.delete<void>(`/api/slabs/${id}`),
};
