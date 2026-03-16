import { api } from './client';
import { SerializedEntry } from '../types/collection';
import { CardCondition } from '../types/filters';

export interface SerializedEntryRequest {
  cardIdentifier: string;
  treatment: string;
  serialNumber: number;
  printRunTotal: number;
  condition: CardCondition;
  autographed: boolean;
  acquisitionDate: string;
  acquisitionPrice: number;
  notes?: string;
}

export const serializedApi = {
  getAll: (userId?: string): Promise<SerializedEntry[]> => {
    const qs = userId ? `?userId=${userId}` : '';
    return api.get<SerializedEntry[]>(`/api/serialized${qs}`);
  },

  getById: (id: string): Promise<SerializedEntry> =>
    api.get<SerializedEntry>(`/api/serialized/${id}`),

  create: (request: SerializedEntryRequest): Promise<SerializedEntry> =>
    api.post<SerializedEntry>('/api/serialized', request),

  update: (id: string, request: SerializedEntryRequest): Promise<SerializedEntry> =>
    api.put<SerializedEntry>(`/api/serialized/${id}`, request),

  delete: (id: string): Promise<void> =>
    api.delete<void>(`/api/serialized/${id}`),
};
