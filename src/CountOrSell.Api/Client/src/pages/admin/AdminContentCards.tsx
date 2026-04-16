import { useEffect, useState } from 'react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { cn } from '@/lib/utils'

interface SetSummary {
  code: string
  name: string
  totalCards: number
}

interface CardRecord {
  identifier: string
  name: string
  rarity: string
  currentMarketValue: number | null
  validTreatments: string[]
}

export function AdminContentCards() {
  const [sets, setSets] = useState<SetSummary[]>([])
  const [selectedSet, setSelectedSet] = useState<string | null>(null)
  const [cards, setCards] = useState<CardRecord[]>([])
  const [setsLoading, setSetsLoading] = useState(true)
  const [cardsLoading, setCardsLoading] = useState(false)
  const [setsError, setSetsError] = useState<string | null>(null)
  const [cardsError, setCardsError] = useState<string | null>(null)

  useEffect(() => {
    fetch('/api/sets', { credentials: 'include' })
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        return res.json() as Promise<SetSummary[]>
      })
      .then(data => {
        setSets(data)
        setSetsLoading(false)
      })
      .catch(err => {
        setSetsError(err instanceof Error ? err.message : 'Failed to load sets')
        setSetsLoading(false)
      })
  }, [])

  useEffect(() => {
    if (!selectedSet) return
    setCardsLoading(true)
    setCardsError(null)
    setCards([])
    fetch(`/api/sets/${selectedSet}/cards`, { credentials: 'include' })
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        return res.json() as Promise<CardRecord[]>
      })
      .then(data => {
        setCards(data)
        setCardsLoading(false)
      })
      .catch(err => {
        setCardsError(err instanceof Error ? err.message : 'Failed to load cards')
        setCardsLoading(false)
      })
  }, [selectedSet])

  return (
    <div className="flex gap-6 h-full">
      {/* Set list */}
      <div className="w-56 shrink-0 border rounded-md overflow-auto">
        {setsLoading && <p className="p-3 text-sm text-muted-foreground">Loading sets...</p>}
        {setsError && <p className="p-3 text-sm text-destructive">Error: {setsError}</p>}
        {!setsLoading && !setsError && sets.length === 0 && (
          <p className="p-3 text-sm text-muted-foreground">No sets found.</p>
        )}
        {sets.map(set => (
          <button
            key={set.code}
            onClick={() => setSelectedSet(set.code)}
            className={cn(
              'w-full text-left px-3 py-2 text-sm border-b last:border-b-0 transition-colors',
              selectedSet === set.code
                ? 'bg-primary text-primary-foreground'
                : 'hover:bg-accent hover:text-accent-foreground'
            )}
          >
            <div className="font-medium">{set.code.toUpperCase()}</div>
            <div className="text-xs truncate opacity-80">{set.name}</div>
            <div className="text-xs opacity-60">{set.totalCards} cards</div>
          </button>
        ))}
      </div>

      {/* Card table */}
      <div className="flex-1 overflow-auto">
        {!selectedSet && (
          <p className="text-muted-foreground text-sm">Select a set to view cards.</p>
        )}
        {cardsLoading && <p className="text-sm text-muted-foreground">Loading cards...</p>}
        {cardsError && <p className="text-sm text-destructive">Error: {cardsError}</p>}
        {selectedSet && !cardsLoading && !cardsError && (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>ID</TableHead>
                <TableHead>Name</TableHead>
                <TableHead>Rarity</TableHead>
                <TableHead>Market Value</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {cards.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={4} className="text-center text-muted-foreground">
                    No cards found.
                  </TableCell>
                </TableRow>
              ) : (
                cards.map(card => (
                  <TableRow key={card.identifier}>
                    <TableCell className="font-mono text-xs">{card.identifier.toUpperCase()}</TableCell>
                    <TableCell>{card.name}</TableCell>
                    <TableCell>{card.rarity}</TableCell>
                    <TableCell>
                      {card.currentMarketValue != null
                        ? `$${card.currentMarketValue.toFixed(2)}`
                        : '-'}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        )}
      </div>
    </div>
  )
}
