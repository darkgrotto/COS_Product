// Renders a Keyrune set symbol using CSS icon font classes.
// Class format: ss ss-{setCode} where setCode is lowercase (e.g. ss-lea, ss-3ed).
// If the set code has no Keyrune glyph the element renders but is invisible.
export function SetSymbol({
  setCode,
  className,
}: {
  setCode: string
  className?: string
}) {
  return (
    <i
      className={`ss ss-${setCode.toLowerCase()} ${className ?? ''}`.trim()}
      aria-hidden="true"
    />
  )
}
