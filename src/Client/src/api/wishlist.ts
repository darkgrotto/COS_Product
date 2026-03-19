import { api } from './client';
import { WishlistEntry } from '../types/collection';
import { CollectionFilter } from '../types/filters';

export interface WishlistListResult {
  entries: WishlistEntry[];
  totalValue: number | null;
}

export const wishlistApi = {
  getAll: (filter?: CollectionFilter): Promise<WishlistListResult> => {
    const params = new URLSearchParams();
    if (filter?.setCode) params.set('setCode', filter.setCode);
    if (filter?.color) params.set('color', filter.color);
    if (filter?.cardType) params.set('cardType', filter.cardType);
    const qs = params.toString() ? `?${params.toString()}` : '';
    return api.get<WishlistListResult>(`/api/wishlist${qs}`);
  },

  add: (cardIdentifier: string): Promise<WishlistEntry> =>
    api.post<WishlistEntry>('/api/wishlist', { cardIdentifier }),

  remove: (id: string): Promise<void> =>
    api.delete<void>(`/api/wishlist/${id}`),

  exportTcgPlayer: (): Promise<string> =>
    api.getText('/api/wishlist/export/tcgplayer'),
};
