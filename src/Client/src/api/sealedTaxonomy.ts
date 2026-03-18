import { api } from './client';

export interface SealedCategory {
  slug: string;
  displayName: string;
}

export interface SealedSubType {
  slug: string;
  categorySlug: string;
  displayName: string;
}

export const sealedTaxonomyApi = {
  getCategories: () =>
    api.get<SealedCategory[]>('/api/sealed-taxonomy/categories'),

  getSubTypes: (categorySlug?: string) =>
    api.get<SealedSubType[]>(
      categorySlug
        ? `/api/sealed-taxonomy/sub-types?categorySlug=${encodeURIComponent(categorySlug)}`
        : '/api/sealed-taxonomy/sub-types',
    ),
};
