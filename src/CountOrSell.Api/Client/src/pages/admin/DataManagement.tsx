import { useCallback, useEffect, useState } from 'react'
import { Trash2, RefreshCw, AlertTriangle, Image, Database } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { ConfirmDialog } from '@/components/ConfirmDialog'

interface SetImageCount {
  setCode: string
  count: number
}

interface DataSummary {
  metadata: {
    setCount: number
    cardCount: number
    sealedProductCount: number
    treatmentCount: number
  }
  images: {
    totalCount: number
    sealedCount: number
    bySet: SetImageCount[]
  }
}

type PurgeScope = 'all' | 'sealed' | { setCode: string }

interface ConfirmState {
  open: boolean
  title: string
  description: string
  onConfirm: () => Promise<void>
}

export function DataManagementPage() {
  const [summary, setSummary] = useState<DataSummary | null>(null)
  const [loading, setLoading] = useState(true)
  const [actionMessage, setActionMessage] = useState<{ text: string; error: boolean } | null>(null)
  const [confirm, setConfirm] = useState<ConfirmState>({
    open: false, title: '', description: '', onConfirm: async () => {},
  })

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const res = await fetch('/api/admin/data/summary', { credentials: 'include' })
      if (res.ok) setSummary(await res.json())
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  function openConfirm(title: string, description: string, onConfirm: () => Promise<void>) {
    setConfirm({ open: true, title, description, onConfirm })
    setActionMessage(null)
  }

  async function runAction(fn: () => Promise<Response>) {
    try {
      const res = await fn()
      if (!res.ok) {
        const data = await res.json().catch(() => ({}))
        setActionMessage({ text: (data as { error?: string }).error ?? 'Action failed.', error: true })
      } else {
        const data = await res.json().catch(() => ({}))
        setActionMessage({ text: (data as { message?: string }).message ?? 'Done.', error: false })
        await load()
      }
    } catch {
      setActionMessage({ text: 'Request failed. Check the application logs.', error: true })
    }
  }

  // ---- Image purge actions ----

  function purgeImages(scope: PurgeScope) {
    const scopeLabel = scope === 'all' ? 'all image data'
      : scope === 'sealed' ? 'sealed product images'
      : `images for set ${scope.setCode.toUpperCase()}`
    const url = scope === 'all' ? '/api/admin/data/images/all'
      : scope === 'sealed' ? '/api/admin/data/images/sealed'
      : `/api/admin/data/images/sets/${scope.setCode}`
    openConfirm(
      'Purge Images',
      `This will permanently delete ${scopeLabel}. Images can be restored by running a content update or force redownload. This cannot be undone. Proceed?`,
      () => runAction(() => fetch(url, { method: 'DELETE', credentials: 'include' })),
    )
  }

  // ---- Metadata purge actions ----

  function purgeMetadata(scope: PurgeScope) {
    const scopeLabel = scope === 'all' ? 'all metadata (sets, cards, sealed products, treatments, and update versions)'
      : scope === 'sealed' ? 'all sealed product metadata'
      : `metadata for set ${(scope as { setCode: string }).setCode.toUpperCase()}`
    const url = scope === 'all' ? '/api/admin/data/metadata/all'
      : scope === 'sealed' ? '/api/admin/data/metadata/sealed'
      : `/api/admin/data/metadata/sets/${(scope as { setCode: string }).setCode}`
    openConfirm(
      'Purge Metadata',
      `This will permanently delete ${scopeLabel}. All associated collection entries and wishlist entries for affected cards will also be deleted. Run a content update to restore. This cannot be undone. Proceed?`,
      () => runAction(() => fetch(url, { method: 'DELETE', credentials: 'include' })),
    )
  }

  const cardImageCount = summary
    ? summary.images.totalCount - summary.images.sealedCount
    : 0

  return (
    <div className="space-y-8 max-w-3xl">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Data Management</h1>
        <Button variant="outline" size="sm" onClick={load} disabled={loading}>
          <RefreshCw className={`h-4 w-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
          Refresh
        </Button>
      </div>

      {actionMessage && (
        <Alert variant={actionMessage.error ? 'destructive' : 'default'}>
          {actionMessage.error
            ? <AlertTriangle className="h-4 w-4" />
            : null}
          <AlertTitle>{actionMessage.error ? 'Error' : 'Done'}</AlertTitle>
          <AlertDescription>{actionMessage.text}</AlertDescription>
        </Alert>
      )}

      {/* ---- Images section ---- */}
      <section className="space-y-4">
        <div className="flex items-center gap-2">
          <Image className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-medium">Images</h2>
        </div>

        {summary && (
          <div className="text-sm text-muted-foreground">
            {summary.images.totalCount} total &mdash; {cardImageCount} card/set,{' '}
            {summary.images.sealedCount} sealed
          </div>
        )}

        <div className="flex flex-wrap gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => purgeImages('all')}
            disabled={loading || summary?.images.totalCount === 0}
          >
            <Trash2 className="h-4 w-4 mr-2" />
            Purge All Images
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => purgeImages('sealed')}
            disabled={loading || summary?.images.sealedCount === 0}
          >
            <Trash2 className="h-4 w-4 mr-2" />
            Purge Sealed Images
          </Button>
        </div>

        {summary && summary.images.bySet.length > 0 && (
          <div className="space-y-1">
            <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Per Set</p>
            <div className="rounded-md border divide-y">
              {summary.images.bySet.map(s => (
                <div key={s.setCode} className="flex items-center justify-between px-4 py-2 text-sm">
                  <span className="font-mono font-medium">{s.setCode.toUpperCase()}</span>
                  <div className="flex items-center gap-3">
                    <span className="text-muted-foreground">{s.count} image{s.count !== 1 ? 's' : ''}</span>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-7 px-2 text-destructive hover:text-destructive"
                      onClick={() => purgeImages({ setCode: s.setCode })}
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {summary && summary.images.bySet.length === 0 && summary.images.sealedCount === 0 && (
          <p className="text-sm text-muted-foreground">No images stored.</p>
        )}
      </section>

      {/* ---- Metadata section ---- */}
      <section className="space-y-4">
        <div className="flex items-center gap-2">
          <Database className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-medium">Metadata</h2>
        </div>

        {summary && (
          <div className="text-sm text-muted-foreground">
            {summary.metadata.setCount} sets, {summary.metadata.cardCount} cards,{' '}
            {summary.metadata.sealedProductCount} sealed products,{' '}
            {summary.metadata.treatmentCount} treatments
          </div>
        )}

        <Alert variant="warning">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription className="text-xs">
            Purging metadata deletes associated collection and wishlist entries permanently.
            Run a content update after purging to restore canonical data.
          </AlertDescription>
        </Alert>

        <div className="flex flex-wrap gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => purgeMetadata('all')}
            disabled={loading || (summary?.metadata.setCount === 0 && summary?.metadata.sealedProductCount === 0)}
          >
            <Trash2 className="h-4 w-4 mr-2" />
            Purge All Metadata
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => purgeMetadata('sealed')}
            disabled={loading || summary?.metadata.sealedProductCount === 0}
          >
            <Trash2 className="h-4 w-4 mr-2" />
            Purge Sealed Metadata
          </Button>
        </div>

        {summary && summary.images.bySet.length > 0 && (
          <div className="space-y-1">
            <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Per Set</p>
            <div className="rounded-md border divide-y">
              {summary.images.bySet.map(s => (
                <div key={s.setCode} className="flex items-center justify-between px-4 py-2 text-sm">
                  <span className="font-mono font-medium">{s.setCode.toUpperCase()}</span>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-7 px-2 text-destructive hover:text-destructive"
                    onClick={() => purgeMetadata({ setCode: s.setCode })}
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                </div>
              ))}
            </div>
          </div>
        )}
      </section>

      <ConfirmDialog
        open={confirm.open}
        onOpenChange={(v) => setConfirm(prev => ({ ...prev, open: v }))}
        title={confirm.title}
        description={confirm.description}
        confirmLabel="Confirm"
        destructive
        onConfirm={confirm.onConfirm}
      />
    </div>
  )
}
