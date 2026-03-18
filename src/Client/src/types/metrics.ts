export interface ContentTypeBreakdown {
  contentType: string;
  totalValue: number;
  totalProfitLoss: number;
  count: number;
}

export interface MetricsResult {
  totalValue: number;
  totalProfitLoss: number;
  totalCardCount: number;
  serializedCount: number;
  slabCount: number;
  sealedProductCount: number;
  sealedProductValue: number;
  byContentType: ContentTypeBreakdown[];
}

export interface SetCompletionResult {
  setCode: string;
  setName: string;
  ownedCount: number;
  totalCards: number;
  percentage: number;
  totalValue: number | null;
  totalProfitLoss: number | null;
}
