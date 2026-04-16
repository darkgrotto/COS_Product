import { api } from './client';

export interface SetSummary {
  code: string;
  name: string;
  totalCards: number;
  setType: string | null;
  releaseDate: string | null;
}

export const setsApi = {
  getAll: (): Promise<SetSummary[]> =>
    api.get<SetSummary[]>('/api/sets'),
};
