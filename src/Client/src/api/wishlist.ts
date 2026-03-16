import { api } from './client';
import { WishlistEntry } from '../types/collection';

export interface WishlistListResult {
  entries: WishlistEntry[];
  totalValue: number | null;
}

export const wishlistApi = {
  getAll: (): Promise<WishlistListResult> =>
    api.get<WishlistListResult>('/api/wishlist'),

  add: (cardIdentifier: string): Promise<WishlistEntry> =>
    api.post<WishlistEntry>('/api/wishlist', { cardIdentifier }),

  remove: (id: string): Promise<void> =>
    api.delete<void>(`/api/wishlist/${id}`),
};
