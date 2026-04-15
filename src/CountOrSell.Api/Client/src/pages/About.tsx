import { useEffect, useState } from 'react'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'

interface ContentStats {
  totalCards: number
  totalSets: number
  totalCardImages: number
  totalSealedImages: number
}

interface AboutData {
  currentVersion: string
  latestReleasedVersion: string
  updatePending: boolean
  lastContentUpdate: string | null
  lastUpdateCheckedAt: string | null
  scheduledUpdateCheckTime: string | null
  instanceName: string
  isDemo: boolean
  demoSets: string[]
  contentStats: ContentStats
  license: {
    name: string
    fullName: string
    url: string
  }
}

function fmtDateTime(iso: string | null | undefined): string {
  if (!iso) return 'Never'
  const d = new Date(iso)
  return d.toLocaleString(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}

// Formats the stored content version for display. The version key is an ISO timestamp
// (e.g. "2026-04-10T15:30:00Z") since the switch from cards-only version tracking.
// Old records may still hold a semver string like "1.0.0" - show those as-is.
function fmtContentVersion(v: string | null | undefined): string {
  if (!v) return 'No updates applied'
  const d = new Date(v)
  if (isNaN(d.getTime())) return v
  return d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

export function AboutPage() {
  const [data, setData] = useState<AboutData | null>(null)
  const [error, setError] = useState(false)

  useEffect(() => {
    fetch('/api/about', { credentials: 'include' })
      .then(res => {
        if (!res.ok) throw new Error()
        return res.json()
      })
      .then((d: AboutData) => setData(d))
      .catch(() => setError(true))
  }, [])

  if (error) return <p className="text-destructive text-sm">Failed to load about information.</p>
  if (!data) return <p className="text-muted-foreground text-sm">Loading...</p>

  return (
    <div className="max-w-lg space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">{data.instanceName}</h1>
        {data.isDemo && (
          <Badge variant="secondary" className="mt-2">Demo Environment</Badge>
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Application</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3 text-sm">
          <Row label="Current version" value={data.currentVersion} />
          <Row
            label="Latest released version"
            value={
              <span className="flex items-center gap-2">
                {data.latestReleasedVersion}
                {data.updatePending && <Badge variant="outline">Update available</Badge>}
              </span>
            }
          />
          <Separator />
          <Row
            label="Last content update"
            value={fmtContentVersion(data.lastContentUpdate)}
          />
          <Row
            label="Last update check"
            value={fmtDateTime(data.lastUpdateCheckedAt)}
          />
          {data.scheduledUpdateCheckTime && (
            <Row
              label="Scheduled check time"
              value={`Daily at ${data.scheduledUpdateCheckTime} UTC`}
            />
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Content</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3 text-sm">
          <Row
            label="Cards"
            value={data.contentStats.totalCards.toLocaleString()}
          />
          <Row
            label="Sets"
            value={data.contentStats.totalSets.toLocaleString()}
          />
          <Separator />
          <Row
            label="Card images"
            value={data.contentStats.totalCardImages.toLocaleString()}
          />
          <Row
            label="Sealed product images"
            value={data.contentStats.totalSealedImages.toLocaleString()}
          />
        </CardContent>
      </Card>

      {data.isDemo && data.demoSets.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Demo Sets</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              {data.demoSets.map(s => s.toUpperCase()).join(', ')}
            </p>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">License</CardTitle>
        </CardHeader>
        <CardContent className="text-sm space-y-1">
          <p>{data.license.fullName}</p>
          <a
            href={data.license.url}
            target="_blank"
            rel="noreferrer"
            className="text-primary hover:underline text-xs"
          >
            {data.license.name}
          </a>
        </CardContent>
      </Card>
    </div>
  )
}

function Row({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex items-center justify-between gap-4">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium text-right">{value}</span>
    </div>
  )
}
