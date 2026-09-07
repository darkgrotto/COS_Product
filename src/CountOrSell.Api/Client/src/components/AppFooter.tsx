const rawCommit = import.meta.env.VITE_GIT_COMMIT as string | undefined
const rawTag = import.meta.env.VITE_BUILD_TAG as string | undefined
const hash = rawCommit ? rawCommit.slice(0, 7) : null
const buildLabel = hash
  ? rawTag ? `${hash} (${rawTag})` : hash
  : rawTag ?? 'dev'

export function AppFooter() {
  return (
    // Sits at the end of the scrolling content rather than pinned across the viewport:
    // as a fixed bar it overlapped the nav and pushed the user menu off the top bar.
    <footer className="border-t mt-8 pt-3 flex items-center justify-between flex-wrap gap-2 text-xs text-muted-foreground">
      <span>
        CountOrSell is not affiliated with, endorsed by, or sponsored by Wizards of the Coast LLC or
        Hasbro, Inc. Magic: The Gathering is a trademark of Wizards of the Coast LLC.
      </span>
      <span className="flex items-center gap-2 shrink-0">
        <a
          href="https://www.gnu.org/licenses/agpl-3.0.html"
          target="_blank"
          rel="noopener noreferrer"
          className="hover:underline"
        >
          Licensed under AGPL-3.0
        </a>
        <span>build {buildLabel}</span>
      </span>
    </footer>
  )
}
