import { useEffect, useState } from 'react';
import { SealedInventoryEntry } from '../types/collection';
import { CollectionFilter } from '../types/filters';
import { sealedInventoryApi, SealedInventoryRequest } from '../api/sealedInventory';
import { sealedTaxonomyApi, SealedCategory } from '../api/sealedTaxonomy';
import { UniversalFilter } from '../components/UniversalFilter';

interface Props {
  adminUserId?: string;
}

interface EditForm {
  acquisitionDate: string;
  acquisitionPrice: string;
  notes: string;
  categorySlug: string;
  subTypeSlug: string;
}

export function SealedInventoryList({ adminUserId }: Props) {
  const [entries, setEntries] = useState<SealedInventoryEntry[]>([]);
  const [filter, setFilter] = useState<CollectionFilter>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<EditForm>({ acquisitionDate: '', acquisitionPrice: '', notes: '', categorySlug: '', subTypeSlug: '' });
  const [saving, setSaving] = useState(false);
  const [categories, setCategories] = useState<SealedCategory[]>([]);

  useEffect(() => {
    sealedTaxonomyApi.getCategories()
      .then(setCategories)
      .catch(() => {});
  }, []);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    sealedInventoryApi.getAll(adminUserId, filter.sealedCategorySlug, filter.sealedSubTypeSlug)
      .then((data) => { if (!cancelled) { setEntries(data); setLoading(false); } })
      .catch(() => { if (!cancelled) { setError('Failed to load sealed inventory'); setLoading(false); } });
    return () => { cancelled = true; };
  }, [adminUserId, filter.sealedCategorySlug, filter.sealedSubTypeSlug]);

  const handleDelete = async (id: string) => {
    await sealedInventoryApi.delete(id);
    setEntries((prev) => prev.filter((e) => e.id !== id));
  };

  const startEdit = (e: SealedInventoryEntry) => {
    setEditingId(e.id);
    setEditForm({
      acquisitionDate: e.acquisitionDate,
      acquisitionPrice: String(e.acquisitionPrice),
      notes: e.notes ?? '',
      categorySlug: e.categorySlug ?? '',
      subTypeSlug: e.subTypeSlug ?? '',
    });
  };

  const cancelEdit = () => setEditingId(null);

  const saveEdit = async (e: SealedInventoryEntry) => {
    setSaving(true);
    try {
      const request: SealedInventoryRequest = {
        productIdentifier: e.productIdentifier,
        quantity: e.quantity,
        condition: e.condition,
        acquisitionDate: editForm.acquisitionDate,
        acquisitionPrice: parseFloat(editForm.acquisitionPrice),
        notes: editForm.notes.trim() || undefined,
        categorySlug: editForm.categorySlug || undefined,
        subTypeSlug: editForm.subTypeSlug || undefined,
      };
      const updated = await sealedInventoryApi.update(e.id, request);
      setEntries((prev) => prev.map((x) => x.id === e.id ? updated : x));
      setEditingId(null);
    } catch {
      // leave edit form open on error
    } finally {
      setSaving(false);
    }
  };

  const handleEditCategoryChange = (slug: string) => {
    setEditForm((f) => ({ ...f, categorySlug: slug, subTypeSlug: '' }));
  };

  const editSelectedCategory = categories.find((c) => c.slug === editForm.categorySlug);

  if (error) return <div role="alert">{error}</div>;

  return (
    <div>
      <h2>Sealed Product Inventory</h2>
      <UniversalFilter
        filter={filter}
        onChange={setFilter}
        hideFields={['setCode', 'color', 'cardType', 'treatment', 'autographed', 'serialized', 'slabbed', 'sealedProduct', 'gradingAgency']}
      />
      {loading ? (
        <p>Loading...</p>
      ) : entries.length === 0 ? (
        <p>No sealed inventory found.</p>
      ) : (
        <table aria-label="Sealed inventory entries">
          <thead>
            <tr>
              <th>Product</th>
              <th>Category</th>
              <th>Sub-type</th>
              <th>Qty</th>
              <th>Condition</th>
              <th>Acquired</th>
              <th>Acquisition price</th>
              <th>Market value</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((e) => {
              if (editingId === e.id) {
                return (
                  <tr key={e.id}>
                    <td>{e.productName ?? e.productIdentifier}</td>
                    <td>
                      {categories.length > 0 ? (
                        <select
                          value={editForm.categorySlug}
                          onChange={(ev) => handleEditCategoryChange(ev.target.value)}
                          disabled={saving}
                          aria-label="Category"
                        >
                          <option value="">-- None --</option>
                          {categories.map((c) => (
                            <option key={c.slug} value={c.slug}>{c.displayName}</option>
                          ))}
                        </select>
                      ) : (
                        e.categoryDisplayName ?? ''
                      )}
                    </td>
                    <td>
                      {categories.length > 0 && editSelectedCategory && editSelectedCategory.subTypes.length > 0 ? (
                        <select
                          value={editForm.subTypeSlug}
                          onChange={(ev) => setEditForm((f) => ({ ...f, subTypeSlug: ev.target.value }))}
                          disabled={saving}
                          aria-label="Sub-type"
                        >
                          <option value="">-- None --</option>
                          {editSelectedCategory.subTypes.map((s) => (
                            <option key={s.slug} value={s.slug}>{s.displayName}</option>
                          ))}
                        </select>
                      ) : (
                        e.subTypeDisplayName ?? ''
                      )}
                    </td>
                    <td>{e.quantity}</td>
                    <td>{e.condition}</td>
                    <td>
                      <input
                        type="date"
                        value={editForm.acquisitionDate}
                        onChange={(ev) => setEditForm((f) => ({ ...f, acquisitionDate: ev.target.value }))}
                        disabled={saving}
                        aria-label="Acquisition date"
                      />
                    </td>
                    <td>
                      <input
                        type="number"
                        min={0}
                        step="0.01"
                        value={editForm.acquisitionPrice}
                        onChange={(ev) => setEditForm((f) => ({ ...f, acquisitionPrice: ev.target.value }))}
                        disabled={saving}
                        aria-label="Acquisition price"
                      />
                    </td>
                    <td>
                      <input
                        type="text"
                        value={editForm.notes}
                        onChange={(ev) => setEditForm((f) => ({ ...f, notes: ev.target.value }))}
                        disabled={saving}
                        placeholder="Notes"
                        aria-label="Notes"
                      />
                    </td>
                    <td>
                      <button type="button" onClick={() => saveEdit(e)} disabled={saving}>
                        {saving ? 'Saving...' : 'Save'}
                      </button>
                      {' '}
                      <button type="button" onClick={cancelEdit} disabled={saving}>Cancel</button>
                    </td>
                  </tr>
                );
              }
              return (
                <tr key={e.id}>
                  <td>{e.productName ?? e.productIdentifier}</td>
                  <td>{e.categoryDisplayName ?? ''}</td>
                  <td>{e.subTypeDisplayName ?? ''}</td>
                  <td>{e.quantity}</td>
                  <td>{e.condition}</td>
                  <td>{e.acquisitionDate}</td>
                  <td>${e.acquisitionPrice.toFixed(2)}</td>
                  <td>{e.currentMarketValue !== null ? `$${e.currentMarketValue.toFixed(2)}` : '--'}</td>
                  <td>
                    {!adminUserId && (
                      <>
                        <button
                          type="button"
                          onClick={() => startEdit(e)}
                          aria-label={`Edit sealed product ${e.productName ?? e.productIdentifier}`}
                        >
                          Edit
                        </button>
                        {' '}
                        <button
                          type="button"
                          onClick={() => handleDelete(e.id)}
                          aria-label={`Delete sealed product ${e.productName ?? e.productIdentifier}`}
                        >
                          Delete
                        </button>
                      </>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
}
