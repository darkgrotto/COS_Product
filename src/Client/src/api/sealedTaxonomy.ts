import { api } from './client';

export interface SealedSubType {
  slug: string;
  categorySlug: string;
  displayName: string;
  sortOrder: number;
}

export interface SealedCategory {
  slug: string;
  displayName: string;
  sortOrder: number;
  subTypes: SealedSubType[];
}

export const sealedTaxonomyApi = {
  getCategories: () =>
    api.get<SealedCategory[]>('/api/sealed-product-taxonomy/categories'),
};
