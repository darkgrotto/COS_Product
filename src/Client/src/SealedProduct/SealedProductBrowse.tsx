import { useEffect, useState } from 'react';
import { sealedTaxonomyApi, SealedCategory } from '../api/sealedTaxonomy';
import { api } from '../api/client';

interface SealedProductSummary {
  identifier: string;
  name: string;
  setCode: string | null;
  categorySlug: string | null;
  subTypeSlug: string | null;
  currentMarketValue: number | null;
  imageUrl: string;
}

interface BrowseResult {
  items: SealedProductSummary[];
  total: number;
  page: number;
  pageSize: number;
}

const PAGE_SIZE = 50;

export function SealedProductBrowse() {
  const [categories, setCategories] = useState<SealedCategory[]>([]);
  const [setCodeFilter, setSetCodeFilter] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [subTypeFilter, setSubTypeFilter] = useState('');
  const [page, setPage] = useState(1);
  const [result, setResult] = useState<BrowseResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    sealedTaxonomyApi.getCategories()
      .then(setCategories)
      .catch(() => {});
  }, []);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    const params = new URLSearchParams();
    if (setCodeFilter.trim()) params.set('setCode', setCodeFilter.trim());
    if (categoryFilter) params.set('categorySlug', categoryFilter);
    if (subTypeFilter) params.set('subTypeSlug', subTypeFilter);
    params.set('page', String(page));
    params.set('pageSize', String(PAGE_SIZE));

    api.get<BrowseResult>(`/api/sealed-products?${params}`)
      .then((data) => { if (!cancelled) { setResult(data); setLoading(false); } })
      .catch(() => { if (!cancelled) { setError('Failed to load sealed products'); setLoading(false); } });

    return () => { cancelled = true; };
  }, [setCodeFilter, categoryFilter, subTypeFilter, page]);

  const handleCategoryChange = (slug: string) => {
    setCategoryFilter(slug);
    setSubTypeFilter('');
    setPage(1);
  };

  const handleFilterChange = (setter: (v: string) => void) => (v: string) => {
    setter(v);
    setPage(1);
  };

  const selectedCategory = categories.find((c) => c.slug === categoryFilter);
  const totalPages = result ? Math.ceil(result.total / PAGE_SIZE) : 0;

  return (
    <div>
      <h2>Sealed Products</h2>

      <div role="search" aria-label="Sealed product filters">
        <label>
          Set code{' '}
          <input
            type="text"
            value={setCodeFilter}
            onChange={(e) => handleFilterChange(setSetCodeFilter)(e.target.value)}
            placeholder="e.g. EOE"
            aria-label="Filter by set code"
          />
        </label>
        {categories.length > 0 && (
          <>
            {' '}
            <label>
              Category{' '}
              <select
                value={categoryFilter}
                onChange={(e) => handleCategoryChange(e.target.value)}
                aria-label="Filter by category"
              >
                <option value="">All categories</option>
                {categories.map((c) => (
                  <option key={c.slug} value={c.slug}>{c.displayName}</option>
                ))}
              </select>
            </label>
            {selectedCategory && selectedCategory.subTypes.length > 0 && (
              <>
                {' '}
                <label>
                  Sub-type{' '}
                  <select
                    value={subTypeFilter}
                    onChange={(e) => handleFilterChange(setSubTypeFilter)(e.target.value)}
                    aria-label="Filter by sub-type"
                  >
                    <option value="">All sub-types</option>
                    {selectedCategory.subTypes.map((s) => (
                      <option key={s.slug} value={s.slug}>{s.displayName}</option>
                    ))}
                  </select>
                </label>
              </>
            )}
          </>
        )}
      </div>

      {loading ? (
        <p>Loading...</p>
      ) : error ? (
        <div role="alert">{error}</div>
      ) : result && result.items.length === 0 ? (
        <p>No sealed products found.</p>
      ) : result ? (
        <>
          <p>{result.total} product{result.total !== 1 ? 's' : ''}</p>
          <table aria-label="Sealed products catalog">
            <thead>
              <tr>
                <th aria-label="Product image"></th>
                <th>Name</th>
                <th>Set</th>
                <th>Category</th>
                <th>Sub-type</th>
                <th>Market value</th>
              </tr>
            </thead>
            <tbody>
              {result.items.map((p) => {
                const cat = categories.find((c) => c.slug === p.categorySlug);
                const sub = cat?.subTypes.find((s) => s.slug === p.subTypeSlug);
                return (
                  <tr key={p.identifier}>
                    <td style={{ width: '60px' }}>
                      <img
                        src={p.imageUrl}
                        alt={p.name}
                        style={{ width: '50px', height: '50px', objectFit: 'contain', display: 'block' }}
                        onError={(e) => { e.currentTarget.style.display = 'none'; }}
                      />
                    </td>
                    <td>{p.name}</td>
                    <td>{p.setCode ?? '--'}</td>
                    <td>{cat ? cat.displayName : (p.categorySlug ?? '--')}</td>
                    <td>{sub ? sub.displayName : (p.subTypeSlug ?? '--')}</td>
                    <td>{p.currentMarketValue !== null ? `$${p.currentMarketValue.toFixed(2)}` : '--'}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          {totalPages > 1 && (
            <div role="navigation" aria-label="Pagination">
              <button
                type="button"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
              >
                Previous
              </button>
              {' '}
              Page {page} of {totalPages}
              {' '}
              <button
                type="button"
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
              >
                Next
              </button>
            </div>
          )}
        </>
      ) : null}
    </div>
  );
}
