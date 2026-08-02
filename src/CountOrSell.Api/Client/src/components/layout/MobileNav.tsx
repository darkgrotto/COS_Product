import { useState } from 'react'
import { Menu } from 'lucide-react'
import { useBranding } from '@/contexts/BrandingContext'
import {
  Sheet,
  SheetContent,
  SheetTitle,
  SheetDescription,
} from '@/components/ui/sheet'
import { SidebarContent } from './Sidebar'

// Mobile-only top bar with a hamburger that opens the navigation drawer.
// Hidden at md and up, where the static Sidebar rail is shown instead.
export function MobileNav() {
  const { instanceName } = useBranding()
  const [open, setOpen] = useState(false)

  return (
    <header className="md:hidden flex items-center gap-3 border-b bg-background px-4 h-14 shrink-0">
      <button
        type="button"
        onClick={() => setOpen(true)}
        aria-label="Open navigation menu"
        className="-ml-1 rounded-md p-2 text-muted-foreground hover:bg-accent hover:text-accent-foreground transition-colors"
      >
        <Menu className="h-5 w-5" />
      </button>
      <span className="font-semibold text-sm tracking-tight truncate">{instanceName}</span>

      <Sheet open={open} onOpenChange={setOpen}>
        <SheetContent side="left" className="p-0">
          <SheetTitle className="sr-only">Navigation</SheetTitle>
          <SheetDescription className="sr-only">
            Application navigation and account menu
          </SheetDescription>
          <SidebarContent onNavigate={() => setOpen(false)} />
        </SheetContent>
      </Sheet>
    </header>
  )
}
