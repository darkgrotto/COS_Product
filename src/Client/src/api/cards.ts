import { api } from './client';

export interface CardSummary {
  identifier: string;
  name: string;
  setCode: string;
  currentMarketValue: number | null;
  isReserved: boolean;
}

export interface MarketValueResult {
  cardIdentifier: string;
  marketValue: number | null;
  updatedAt: string | null;
}

export const cardsApi = {
  getByIdentifier: (identifier: string): Promise<CardSummary> =>
    api.get<CardSummary>(`/api/cards/${identifier}`),

  search: (query: string): Promise<CardSummary[]> =>
    api.get<CardSummary[]>(`/api/cards/search?q=${encodeURIComponent(query)}`),

  getMarketValue: (identifier: string): Promise<MarketValueResult> =>
    api.get<MarketValueResult>(`/api/cards/${identifier}/market-value`),

  refreshPrice: (identifier: string): Promise<MarketValueResult> =>
    api.post<MarketValueResult>(`/api/cards/${identifier}/refresh-price`),

  getReservedIdentifiers: (): Promise<string[]> =>
    api.get<string[]>('/api/cards/reserved-identifiers'),
};
