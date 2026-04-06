import { useDemo } from '@/contexts/DemoContext'

export function FilterScopeIndicator() {
  const { isDemo, demoSets } = useDemo()
  if (!isDemo || demoSets.length === 0) return null

  return (
    <p className="text-xs text-muted-foreground mt-1">
      Results limited to demo sets: {demoSets.map(s => s.toUpperCase()).join(', ')}
    </p>
  )
}
