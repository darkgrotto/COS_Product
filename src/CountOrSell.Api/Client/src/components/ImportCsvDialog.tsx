import { useRef, useState } from 'react'
import { CheckCircle, Download, FileText, Upload, XCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import {
  Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle,
} from '@/components/ui/dialog'

interface ImportResult {
  added: number
  skipped: number
  failed: number
  failures: string[]
}

interface ImportCsvDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  entityLabel: string
  templateUrl: string
  importUrl: string
  hint?: string
  onImportDone?: () => void
}

export function ImportCsvDialog({
  open,
  onOpenChange,
  entityLabel,
  templateUrl,
  importUrl,
  hint,
  onImportDone,
}: ImportCsvDialogProps) {
  const [busy, setBusy] = useState(false)
  const [result, setResult] = useState<ImportResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const fileRef = useRef<HTMLInputElement>(null)

  function reset() {
    setBusy(false)
    setResult(null)
    setError(null)
  }

  function handleOpenChange(v: boolean) {
    if (!v) reset()
    onOpenChange(v)
  }

  async function handleTemplate() {
    setError(null)
    try {
      const res = await fetch(templateUrl)
      if (!res.ok) throw new Error(`Template download failed (${res.status})`)
      const blob = await res.blob()
      const cd = res.headers.get('content-disposition') ?? ''
      const match = cd.match(/filename="?([^"]+)"?/)
      const name = match ? match[1] : `${entityLabel.toLowerCase()}-template.csv`
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = name
      a.click()
      URL.revokeObjectURL(url)
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Template download failed')
    }
  }

  async function handleImport() {
    const file = fileRef.current?.files?.[0]
    if (!file) { setError('Please choose a CSV file first.'); return }
    setBusy(true)
    setResult(null)
    setError(null)
    try {
      const fd = new FormData()
      fd.append('file', file)
      const res = await fetch(importUrl, { method: 'POST', body: fd })
      if (!res.ok) {
        const body = await res.json().catch(() => ({}))
        throw new Error(body.error ?? `Import failed (${res.status})`)
      }
      const r: ImportResult = await res.json()
      setResult(r)
      if (r.added > 0 && onImportDone) onImportDone()
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Import failed')
    } finally {
      setBusy(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Import {entityLabel} from CSV</DialogTitle>
        </DialogHeader>

        <div className="space-y-4">
          <div className="rounded-md border p-3 space-y-2">
            <p className="text-sm font-medium flex items-center gap-1.5">
              <FileText className="h-4 w-4" />
              Step 1: Download the template
            </p>
            <p className="text-xs text-muted-foreground">
              The template lists every required and optional column. Edit it in your spreadsheet tool, then upload.
            </p>
            <Button variant="outline" size="sm" type="button" onClick={handleTemplate}>
              <Download className="h-3.5 w-3.5 mr-1" />
              Download template
            </Button>
          </div>

          <div className="space-y-1.5">
            <Label>Step 2: Choose your CSV file</Label>
            <input
              ref={fileRef}
              type="file"
              accept=".csv,text/csv"
              className="block w-full text-sm file:mr-3 file:py-1 file:px-3 file:rounded-md file:border file:border-input file:text-sm file:bg-background file:cursor-pointer cursor-pointer"
            />
            {hint && <p className="text-xs text-muted-foreground">{hint}</p>}
            <p className="text-xs text-muted-foreground">
              Every row is checked for format and required fields. Invalid rows are reported back with a row number; valid rows are imported.
            </p>
          </div>

          {error && (
            <p className="flex items-center gap-1.5 text-sm text-destructive">
              <XCircle className="h-4 w-4 shrink-0" />
              {error}
            </p>
          )}

          {result && (
            <div className="rounded-md border p-3 space-y-1 text-sm">
              <p className="flex items-center gap-1.5 font-medium text-green-600 dark:text-green-400">
                <CheckCircle className="h-4 w-4 shrink-0" />
                Import complete
              </p>
              <p className="text-muted-foreground">
                Added: {result.added} &nbsp;&middot;&nbsp;
                Skipped: {result.skipped} &nbsp;&middot;&nbsp;
                Failed: {result.failed}
              </p>
              {result.failures.length > 0 && (
                <details className="mt-2">
                  <summary className="cursor-pointer text-xs text-muted-foreground hover:text-foreground">
                    Show {result.failures.length} failure{result.failures.length !== 1 ? 's' : ''}
                  </summary>
                  <ul className="mt-1 space-y-0.5 max-h-40 overflow-y-auto">
                    {result.failures.map((f, i) => (
                      <li key={i} className="text-xs text-muted-foreground">{f}</li>
                    ))}
                  </ul>
                </details>
              )}
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => handleOpenChange(false)}>
            {result ? 'Close' : 'Cancel'}
          </Button>
          {!result && (
            <Button onClick={handleImport} disabled={busy}>
              <Upload className="h-3.5 w-3.5 mr-1" />
              {busy ? 'Importing...' : 'Import'}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
