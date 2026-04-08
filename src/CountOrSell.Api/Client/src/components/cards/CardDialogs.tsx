import { useEffect, useState } from 'react'
import {
  ChevronUp, ChevronDown, ChevronsUpDown, Plus, ExternalLink, Star,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle,
} from '@/components/ui/dialog'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'

// ---- Shared types -----------------------------------------------------------

export interface Treatment {
  key: string
  displayName: string
  sortOrder: number
}

// Minimal card shape needed by QuickAddDialog.
export interface AddableCard {
  identifier: string
  name: string
  currentMarketValue: number | null
  // Per-treatment prices from pricing.json; optional - falls back to currentMarketValue.
  prices?: Record<string, number | null>
}

// ---- Shared constants -------------------------------------------------------

export const CONDITIONS = ['NM', 'LP', 'MP', 'HP', 'DMG'] as const
export const CONDITION_LABELS: Record<string, string> = {
  NM: 'Near Mint', LP: 'Lightly Played', MP: 'Moderately Played',
  HP: 'Heavily Played', DMG: 'Damaged',
}

export const COLORS = [
  { key: 'W', label: 'W', title: 'White' },
  { key: 'U', label: 'U', title: 'Blue' },
  { key: 'B', label: 'B', title: 'Black' },
  { key: 'R', label: 'R', title: 'Red' },
  { key: 'G', label: 'G', title: 'Green' },
  { key: 'C', label: 'C', title: 'Colorless' },
]

export const CARD_TYPES = [
  'Creature', 'Instant', 'Sorcery', 'Enchantment',
  'Artifact', 'Land', 'Planeswalker', 'Battle',
]

export function today() {
  return new Date().toISOString().slice(0, 10)
}

export function fmt(v: number | null | undefined) {
  if (v == null) return '-'
  return `$${v.toFixed(2)}`
}

// Regular first, Foil second, then alphabetical by displayName.
export function sortTreatments<T extends { key: string; displayName: string }>(ts: T[]): T[] {
  return [...ts].sort((a, b) => {
    if (a.key === 'regular') return -1
    if (b.key === 'regular') return 1
    if (a.key === 'foil') return -1
    if (b.key === 'foil') return 1
    return a.displayName.localeCompare(b.displayName)
  })
}

// ---- Filter chip ------------------------------------------------------------

export function ToggleChip({
  active, onClick, children,
}: {
  active: boolean
  onClick: () => void
  children: React.ReactNode
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`px-2.5 py-1 rounded-md text-xs font-medium border transition-colors ${
        active
          ? 'bg-primary text-primary-foreground border-primary'
          : 'bg-background text-muted-foreground border-border hover:bg-accent'
      }`}
    >
      {children}
    </button>
  )
}

// ---- Sortable table header --------------------------------------------------

export type SortDir = 'asc' | 'desc'

export function SortTh({
  label, sortKey, current, dir, onSort, className,
}: {
  label: string
  sortKey: string
  current: string
  dir: SortDir
  onSort: (key: string) => void
  className?: string
}) {
  const active = current === sortKey
  return (
    <th
      className={`px-3 py-2 cursor-pointer select-none hover:bg-muted/70 transition-colors ${className ?? ''}`}
      onClick={() => onSort(sortKey)}
    >
      <span className="inline-flex items-center gap-1">
        {label}
        {active
          ? dir === 'asc' ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />
          : <ChevronsUpDown className="h-3 w-3 opacity-30" />}
      </span>
    </th>
  )
}

// ---- Rulings panel ----------------------------------------------------------

interface ScryfallRuling {
  source: string
  published_at: string
  comment: string
}

function RulingsPanel({ rulingUrl }: { rulingUrl: string }) {
  const [rulings, setRulings] = useState<ScryfallRuling[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setRulings(null)
    setError(null)
    fetch(rulingUrl, { headers: { Accept: 'application/json' } })
      .then(r => {
        if (!r.ok) throw new Error(`${r.status} ${r.statusText}`)
        return r.json() as Promise<{ data: ScryfallRuling[] }>
      })
      .then(body => setRulings(body.data))
      .catch((e: Error) => setError(e.message))
  }, [rulingUrl])

  return (
    <div className="border-t pt-3 space-y-2">
      <div className="flex items-center justify-between">
        <p className="text-xs text-muted-foreground font-medium uppercase tracking-wide">Rulings</p>
        <a
          href={rulingUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-primary"
        >
          Scryfall <ExternalLink className="h-2.5 w-2.5" />
        </a>
      </div>

      {error && (
        <p className="text-xs text-destructive">Failed to load rulings: {error}</p>
      )}

      {!error && !rulings && (
        <p className="text-xs text-muted-foreground">Loading rulings...</p>
      )}

      {rulings && rulings.length === 0 && (
        <p className="text-xs text-muted-foreground">No rulings on record.</p>
      )}

      {rulings && rulings.length > 0 && (
        <div className="space-y-2">
          {rulings.map((r, i) => (
            <div
              key={i}
              className={`rounded-md px-3 py-2 text-sm border-l-2 bg-muted/40 ${
                r.source === 'wotc' ? 'border-primary' : 'border-muted-foreground/30'
              }`}
            >
              <p className="text-xs text-muted-foreground mb-1">
                {r.published_at} - {r.source === 'wotc' ? 'Wizards of the Coast' : 'Scryfall'}
              </p>
              <p className="leading-relaxed">{r.comment}</p>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

// ---- Card detail dialog -----------------------------------------------------

interface CardDetail {
  identifier: string
  setCode: string
  name: string
  manaCost: string | null
  cmc: number | null
  color: string | null
  colorIdentity: string | null
  keywords: string | null
  cardType: string | null
  oracleText: string | null
  layout: string | null
  rarity: string | null
  oracleRulingUrl: string | null
  flavorText: string | null
  currentMarketValue: number | null
  updatedAt: string
  isReserved: boolean
}

export function CardDetailDialog({
  identifier,
  onClose,
  onAdd,
}: {
  identifier: string
  onClose: () => void
  onAdd: () => void
}) {
  const [card, setCard] = useState<CardDetail | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetch(`/api/cards/${identifier.toLowerCase()}`)
      .then(r => r.ok ? r.json() : null)
      .then(d => { setCard(d); setLoading(false) })
      .catch(() => setLoading(false))
  }, [identifier])

  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            {loading ? 'Loading...' : card?.name ?? identifier}
            {card?.isReserved && (
              <span title="Reserved List" className="inline-flex">
                <Star className="h-4 w-4 text-amber-500 fill-amber-500" />
              </span>
            )}
          </DialogTitle>
        </DialogHeader>

        {loading ? (
          <p className="text-sm text-muted-foreground py-4">Loading card details...</p>
        ) : !card ? (
          <p className="text-sm text-muted-foreground py-4">Card details not available.</p>
        ) : (
          <div className="space-y-4">
            <div className="flex gap-4">
              <img
                src={`/api/images/cards/${card.identifier.toLowerCase()}.jpg`}
                alt={card.name}
                className="h-32 w-24 rounded object-cover bg-muted shrink-0"
                onError={e => { (e.target as HTMLImageElement).style.display = 'none' }}
              />
              <div className="space-y-1.5 text-sm min-w-0">
                <div className="flex gap-4">
                  <div>
                    <p className="text-xs text-muted-foreground">Identifier</p>
                    <p className="font-mono font-medium">{card.identifier}</p>
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground">Set</p>
                    <p className="font-mono font-medium">{card.setCode.toUpperCase()}</p>
                  </div>
                </div>
                {card.manaCost && (
                  <div>
                    <p className="text-xs text-muted-foreground">Mana Cost</p>
                    <p className="font-mono text-xs">{card.manaCost}</p>
                  </div>
                )}
                <div>
                  <p className="text-xs text-muted-foreground">CMC</p>
                  <p className="text-xs">{card.cmc != null ? card.cmc : '-'}</p>
                </div>
                {card.cardType && (
                  <div>
                    <p className="text-xs text-muted-foreground">Type</p>
                    <p className="text-xs leading-snug">{card.cardType}</p>
                  </div>
                )}
                <div>
                  <p className="text-xs text-muted-foreground">Rarity</p>
                  <p className="text-xs capitalize">{card.rarity ?? '-'}</p>
                </div>
                {card.color && (
                  <div>
                    <p className="text-xs text-muted-foreground">Color</p>
                    <p className="font-mono text-xs">{card.color}</p>
                  </div>
                )}
                <div>
                  <p className="text-xs text-muted-foreground">Color Identity</p>
                  <p className="font-mono text-xs">{card.colorIdentity ?? '-'}</p>
                </div>
                {card.keywords && (
                  <div>
                    <p className="text-xs text-muted-foreground">Keywords</p>
                    <p className="text-xs">{card.keywords.split(',').join(', ')}</p>
                  </div>
                )}
                <div>
                  <p className="text-xs text-muted-foreground">Market Value</p>
                  <p className="font-medium">{fmt(card.currentMarketValue)}</p>
                </div>
              </div>
            </div>

            <div className="border-t pt-3">
              <p className="text-xs text-muted-foreground mb-1">Card Text</p>
              {card.oracleText ? (
                <div className="max-h-28 overflow-y-auto pr-1">
                  <p className="text-xs text-foreground leading-relaxed whitespace-pre-line">{card.oracleText}</p>
                </div>
              ) : (
                <p className="text-xs text-muted-foreground">-</p>
              )}
            </div>

            {card.flavorText && (
              <div className={card.oracleText ? 'pt-2' : 'border-t pt-3'}>
                <p className="text-xs text-muted-foreground mb-1">Flavor Text</p>
                <p className="text-sm italic text-muted-foreground leading-relaxed">{card.flavorText}</p>
              </div>
            )}

            {card.oracleRulingUrl && (
              <RulingsPanel rulingUrl={card.oracleRulingUrl} />
            )}
          </div>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Close</Button>
          <Button onClick={() => { onClose(); onAdd() }}>
            <Plus className="h-4 w-4 mr-1" /> Add
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

// ---- Quick-add dialog -------------------------------------------------------

interface AddForm {
  treatment: string
  quantity: number
  condition: string
  autographed: boolean
  acquisitionDate: string
  acquisitionPrice: string
  notes: string
}

export function QuickAddDialog({
  card,
  treatments,
  onClose,
  onAdded,
}: {
  card: AddableCard
  treatments: Treatment[]
  onClose: () => void
  onAdded: (mode: 'collection' | 'wishlist') => void
}) {
  const sorted = sortTreatments(treatments)
  const defaultTreatment = sorted[0]?.key ?? 'regular'
  const defaultPrice = (card.prices?.[defaultTreatment] ?? card.currentMarketValue)
  const [mode, setMode] = useState<'collection' | 'wishlist'>('collection')
  const [form, setForm] = useState<AddForm>({
    treatment: defaultTreatment,
    quantity: 1,
    condition: 'NM',
    autographed: false,
    acquisitionDate: today(),
    acquisitionPrice: defaultPrice != null ? defaultPrice.toFixed(2) : '',
    notes: '',
  })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  // Track whether the user has manually edited the price so we don't overwrite it on treatment change.
  const [priceManuallyEdited, setPriceManuallyEdited] = useState(false)

  async function handleSave() {
    setError('')
    setSaving(true)
    try {
      if (mode === 'wishlist') {
        const res = await fetch('/api/wishlist', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ cardIdentifier: card.identifier.toLowerCase() }),
        })
        if (!res.ok) {
          const data = await res.json().catch(() => ({}))
          throw new Error((data as { error?: string }).error ?? 'Failed to add to wishlist.')
        }
        onAdded('wishlist')
        onClose()
        return
      }

      // Collection mode
      const rawPrice = form.acquisitionPrice.trim()
      const price = rawPrice === '' ? 0 : parseFloat(rawPrice)
      if (isNaN(price) || price < 0) {
        setError('Enter a valid acquisition price.')
        setSaving(false)
        return
      }
      const res = await fetch('/api/collection', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          cardIdentifier: card.identifier.toLowerCase(),
          treatment: form.treatment,
          quantity: form.quantity,
          condition: form.condition,
          autographed: form.autographed,
          acquisitionDate: form.acquisitionDate,
          acquisitionPrice: price,
          notes: form.notes || null,
        }),
      })
      if (!res.ok) {
        const data = await res.json().catch(() => ({}))
        throw new Error((data as { error?: string }).error ?? 'Failed to add.')
      }
      onAdded('collection')
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Add Card</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="font-medium">{card.name}</p>
              <p className="text-xs text-muted-foreground font-mono">{card.identifier}</p>
            </div>
            <div className="flex gap-1 shrink-0">
              <ToggleChip active={mode === 'collection'} onClick={() => { setMode('collection'); setError('') }}>
                Collection
              </ToggleChip>
              <ToggleChip active={mode === 'wishlist'} onClick={() => { setMode('wishlist'); setError('') }}>
                Wishlist
              </ToggleChip>
            </div>
          </div>

          {mode === 'collection' && (
            <>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <Label>Treatment</Label>
                  <Select
                    value={form.treatment}
                    onValueChange={v => {
                      setForm(f => {
                        if (priceManuallyEdited) return { ...f, treatment: v }
                        const p = card.prices?.[v] ?? card.currentMarketValue
                        return { ...f, treatment: v, acquisitionPrice: p != null ? p.toFixed(2) : '' }
                      })
                    }}
                  >
                    <SelectTrigger><SelectValue /></SelectTrigger>
                    <SelectContent>
                      {sorted.map(t => (
                        <SelectItem key={t.key} value={t.key}>{t.displayName}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div>
                  <Label>Condition</Label>
                  <Select value={form.condition} onValueChange={v => setForm(f => ({ ...f, condition: v }))}>
                    <SelectTrigger><SelectValue /></SelectTrigger>
                    <SelectContent>
                      {CONDITIONS.map(c => (
                        <SelectItem key={c} value={c}>{c} - {CONDITION_LABELS[c]}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <Label>Quantity</Label>
                  <Input
                    type="number" min={1}
                    value={form.quantity}
                    onChange={e => setForm(f => ({ ...f, quantity: parseInt(e.target.value) || 1 }))}
                  />
                </div>
                <div className="flex items-end pb-1">
                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      id="qa-autographed"
                      checked={form.autographed}
                      onChange={e => setForm(f => ({ ...f, autographed: e.target.checked }))}
                      className="h-4 w-4 rounded border-input"
                    />
                    <label htmlFor="qa-autographed" className="text-sm">Autographed</label>
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <Label>Acquisition Date</Label>
                  <Input
                    type="date" value={form.acquisitionDate}
                    onChange={e => setForm(f => ({ ...f, acquisitionDate: e.target.value }))}
                  />
                </div>
                <div>
                  <Label>Acquisition Price <span className="text-xs text-muted-foreground">(blank = $0)</span></Label>
                  <Input
                    type="number" min={0} step="0.01" placeholder="0.00"
                    value={form.acquisitionPrice}
                    onChange={e => {
                      setPriceManuallyEdited(true)
                      setForm(f => ({ ...f, acquisitionPrice: e.target.value }))
                    }}
                  />
                </div>
              </div>

              <div>
                <Label>Notes <span className="text-xs text-muted-foreground">(optional)</span></Label>
                <Input
                  value={form.notes}
                  onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
                  placeholder="Any notes..."
                />
              </div>
            </>
          )}

          {mode === 'wishlist' && (
            <p className="text-sm text-muted-foreground">
              This card will be added to your wishlist. You can add it to your collection from the Wishlist page.
            </p>
          )}

          {error && <p className="text-sm text-destructive">{error}</p>}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={saving}>Cancel</Button>
          <Button onClick={handleSave} disabled={saving}>
            {saving
              ? 'Adding...'
              : mode === 'collection' ? 'Add to Collection' : 'Add to Wishlist'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
