import { api } from './client';

export interface AdminDashboardStats {
  userCount: number;
  setCount: number;
  cardCount: number;
  cardImageCount: number;
  sealedProductCount: number;
  sealedImageCount: number;
  reservedListCount: number;
}

export const adminApi = {
  getDashboard: (): Promise<AdminDashboardStats> =>
    api.get<AdminDashboardStats>('/api/admin/dashboard'),
};
