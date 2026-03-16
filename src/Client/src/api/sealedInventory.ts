import { api } from './client';
import { SealedInventoryEntry } from '../types/collection';
import { CardCondition } from '../types/filters';

export interface SealedInventoryRequest {
  productIdentifier: string;
  quantity: number;
  condition: CardCondition;
  acquisitionDate: string;
  acquisitionPrice: number;
  notes?: string;
}

export const sealedInventoryApi = {
  getAll: (userId?: string): Promise<SealedInventoryEntry[]> => {
    const qs = userId ? `?userId=${userId}` : '';
    return api.get<SealedInventoryEntry[]>(`/api/sealed-inventory${qs}`);
  },

  getById: (id: string): Promise<SealedInventoryEntry> =>
    api.get<SealedInventoryEntry>(`/api/sealed-inventory/${id}`),

  create: (request: SealedInventoryRequest): Promise<SealedInventoryEntry> =>
    api.post<SealedInventoryEntry>('/api/sealed-inventory', request),

  update: (id: string, request: SealedInventoryRequest): Promise<SealedInventoryEntry> =>
    api.put<SealedInventoryEntry>(`/api/sealed-inventory/${id}`, request),

  delete: (id: string): Promise<void> =>
    api.delete<void>(`/api/sealed-inventory/${id}`),
};
