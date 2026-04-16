import { useEffect, useState } from 'react'
import { Card, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'

interface DashboardStats {
  userCount: number
  setCount: number
  cardCount: number
  cardImageCount: number
  sealedProductCount: number
  sealedImageCount: number
  reservedListCount: number
}

export function AdminDashboard() {
  const [stats, setStats] = useState<DashboardStats | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetch('/api/admin/dashboard', { credentials: 'include' })
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        return res.json() as Promise<DashboardStats>
      })
      .then(data => {
        setStats(data)
        setLoading(false)
      })
      .catch(err => {
        setError(err instanceof Error ? err.message : 'Failed to load dashboard')
        setLoading(false)
      })
  }, [])

  const statCards = stats
    ? [
        { title: 'Users', value: stats.userCount, description: 'Registered accounts' },
        { title: 'Sets', value: stats.setCount, description: 'Content sets loaded' },
        { title: 'Cards', value: stats.cardCount, description: 'Card records' },
        { title: 'Card Images', value: stats.cardImageCount, description: 'Card images available' },
        { title: 'Sealed Products', value: stats.sealedProductCount, description: 'Sealed product records' },
        { title: 'Sealed Images', value: stats.sealedImageCount, description: 'Sealed product images' },
        { title: 'Reserved List', value: stats.reservedListCount, description: 'Reserved list entries' },
      ]
    : []

  return (
    <div>
      <h1 className="text-2xl font-semibold mb-6">Dashboard</h1>
      {loading && <p className="text-muted-foreground">Loading...</p>}
      {error && <p className="text-destructive">Error: {error}</p>}
      {stats && (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
          {statCards.map(card => (
            <Card key={card.title}>
              <CardHeader>
                <CardDescription>{card.title}</CardDescription>
                <CardTitle className="text-3xl">{card.value.toLocaleString()}</CardTitle>
                <CardDescription>{card.description}</CardDescription>
              </CardHeader>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
