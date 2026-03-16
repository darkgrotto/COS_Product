import { api } from './client';
import { Treatment } from '../types/treatments';

export const treatmentsApi = {
  getAll: (): Promise<Treatment[]> => api.get<Treatment[]>('/api/treatments'),
};
