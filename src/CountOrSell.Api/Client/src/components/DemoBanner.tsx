import { useDemo } from '@/contexts/DemoContext'

function formatTime(seconds: number): string {
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  const s = seconds % 60
  if (h > 0) return `${h}h ${m}m ${s}s`
  if (m > 0) return `${m}m ${s}s`
  return `${s}s`
}

export function DemoBanner() {
  const { isDemo, secondsRemaining } = useDemo()
  if (!isDemo) return null

  return (
    <div className="bg-amber-500 text-amber-950 px-4 py-2 text-center text-sm font-medium flex items-center justify-center gap-3">
      <span>This is a demonstration environment. Data you add or modify is shared with all visitors.</span>
      {secondsRemaining !== null && secondsRemaining > 0 && (
        <span className="font-mono bg-amber-600/30 px-2 py-0.5 rounded text-xs">
          Expires in {formatTime(secondsRemaining)}
        </span>
      )}
    </div>
  )
}
