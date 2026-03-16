import { api } from './client';
import { SlabEntry } from '../types/collection';

export interface SlabEntryRequest {
  cardIdentifier: string;
  treatment: string;
  gradingAgencyCode: string;
  grade: string;
  certificateNumber: string;
  serialNumber?: number;
  printRunTotal?: number;
  acquisitionDate: string;
  acquisitionPrice: number;
  notes?: string;
}

export const slabsApi = {
  getAll: (userId?: string): Promise<SlabEntry[]> => {
    const qs = userId ? `?userId=${userId}` : '';
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
