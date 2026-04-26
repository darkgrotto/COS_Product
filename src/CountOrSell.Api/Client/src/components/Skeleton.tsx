interface SkeletonProps {
  className?: string
}

export function Skeleton({ className }: SkeletonProps) {
  return (
    <div
      aria-hidden="true"
      className={`animate-pulse rounded-md bg-muted ${className ?? ''}`}
    />
  )
}

interface TableSkeletonProps {
  rows?: number
  columns?: number
}

// Renders a tabular placeholder while data loads. Use inside the same border
// wrapper as the real table so layout doesn't jump on load.
export function TableSkeleton({ rows = 8, columns = 6 }: TableSkeletonProps) {
  return (
    <div role="status" aria-label="Loading" className="rounded-md border">
      <div className="border-b bg-muted/40 px-3 py-2 flex gap-3">
        {Array.from({ length: columns }).map((_, i) => (
          <Skeleton key={i} className="h-3.5 flex-1" />
        ))}
      </div>
      <div className="divide-y">
        {Array.from({ length: rows }).map((_, r) => (
          <div key={r} className="px-3 py-3 flex gap-3 items-center">
            {Array.from({ length: columns }).map((__, c) => (
              <Skeleton key={c} className="h-4 flex-1" />
            ))}
          </div>
        ))}
      </div>
    </div>
  )
}
