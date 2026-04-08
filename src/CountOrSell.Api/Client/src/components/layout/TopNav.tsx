import { NavLink } from 'react-router-dom'
import { useState } from 'react'
import {
  LayoutDashboard,
  Library,
  Hash,
  Award,
  Package,
  Heart,
  BarChart2,
  Users,
  HardDrive,
  Settings,
  RefreshCw,
  Info,
  LogOut,
  KeyRound,
  Moon,
  Sun,
  UserCircle,
  BookOpen,
  Star,
  PanelLeft,
} from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import { useBranding } from '@/contexts/BrandingContext'
import { usePreferences } from '@/contexts/PreferencesContext'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { ChangePasswordDialog } from '@/components/ChangePasswordDialog'
import { ProfileDialog } from '@/components/ProfileDialog'
import { cn } from '@/lib/utils'

interface NavItem {
  to: string
  label: string
  icon: React.ElementType
}

const generalUserNav: NavItem[] = [
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/browse', label: 'Browse', icon: BookOpen },
  { to: '/collection', label: 'Collection', icon: Library },
  { to: '/reserved', label: 'Reserved List', icon: Star },
  { to: '/wishlist', label: 'Wishlist', icon: Heart },
  { to: '/sealed', label: 'Sealed Product', icon: Package },
  { to: '/slabs', label: 'Slabs', icon: Award },
  { to: '/serialized', label: 'Serialized', icon: Hash },
  { to: '/metrics', label: 'Metrics', icon: BarChart2 },
  { to: '/about', label: 'About', icon: Info },
]

const adminNav: NavItem[] = [
  { to: '/browse', label: 'Browse', icon: BookOpen },
  { to: '/admin/users', label: 'Users', icon: Users },
  { to: '/admin/updates', label: 'Updates', icon: RefreshCw },
  { to: '/admin/backups', label: 'Backups', icon: HardDrive },
  { to: '/admin/settings', label: 'Settings', icon: Settings },
  { to: '/about', label: 'About', icon: Info },
]

function TopNavLink({ item }: { item: NavItem }) {
  const Icon = item.icon
  return (
    <NavLink
      to={item.to}
      className={({ isActive }) =>
        cn(
          'flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-sm font-medium transition-colors whitespace-nowrap',
          isActive
            ? 'bg-primary text-primary-foreground'
            : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
        )
      }
    >
      <Icon className="h-4 w-4 shrink-0" />
      {item.label}
    </NavLink>
  )
}

export function TopNav() {
  const { user, logout } = useAuth()
  const { instanceName } = useBranding()
  const { prefs, patchPrefs } = usePreferences()
  const [changePasswordOpen, setChangePasswordOpen] = useState(false)
  const [profileOpen, setProfileOpen] = useState(false)
  const isAdmin = user?.role === 'Admin'
  const isLocalAccount = user !== null
  const navItems = isAdmin ? adminNav : generalUserNav
  const initials = user?.username?.slice(0, 2).toUpperCase() ?? '??'
  const avatarSrc = user?.hasAvatar ? '/api/users/me/avatar' : undefined

  return (
    <>
      <header className="shrink-0 border-b bg-background">
        <div className="flex items-center gap-3 px-4 h-12">
          <span className="font-semibold text-sm tracking-tight shrink-0 mr-2">{instanceName}</span>

          <nav className="flex items-center gap-0.5 flex-1 overflow-x-auto">
            {navItems.map(item => (
              <TopNavLink key={item.to} item={item} />
            ))}
          </nav>

          <DropdownMenu>
            <DropdownMenuTrigger className="shrink-0 flex items-center gap-2 rounded-md px-2 py-1.5 hover:bg-accent transition-colors">
              <Avatar className="h-6 w-6">
                {avatarSrc && <AvatarImage src={avatarSrc} alt={user?.displayName ?? user?.username} />}
                <AvatarFallback className="text-xs">{initials}</AvatarFallback>
              </Avatar>
              <span className="text-sm font-medium hidden sm:block max-w-32 truncate">
                {user?.displayName || user?.username}
              </span>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-48">
              <DropdownMenuItem onClick={() => setProfileOpen(true)}>
                <UserCircle className="h-4 w-4 mr-2" />
                Edit Profile
              </DropdownMenuItem>
              {isLocalAccount && (
                <DropdownMenuItem onClick={() => setChangePasswordOpen(true)}>
                  <KeyRound className="h-4 w-4 mr-2" />
                  Change Password
                </DropdownMenuItem>
              )}
              <DropdownMenuItem onClick={() => patchPrefs({ darkMode: !prefs.darkMode })}>
                {prefs.darkMode
                  ? <Sun className="h-4 w-4 mr-2" />
                  : <Moon className="h-4 w-4 mr-2" />}
                {prefs.darkMode ? 'Light Mode' : 'Dark Mode'}
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => patchPrefs({ navLayout: 'sidebar' })}>
                <PanelLeft className="h-4 w-4 mr-2" />
                Switch to Sidebar
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={logout} className="text-destructive focus:text-destructive">
                <LogOut className="h-4 w-4 mr-2" />
                Sign out
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </header>

      <ChangePasswordDialog open={changePasswordOpen} onOpenChange={setChangePasswordOpen} />
      <ProfileDialog open={profileOpen} onOpenChange={setProfileOpen} />
    </>
  )
}
