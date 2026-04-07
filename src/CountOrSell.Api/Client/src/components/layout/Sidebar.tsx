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
  UserCircle,
} from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Separator } from '@/components/ui/separator'
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
  { to: '/collection', label: 'Collection', icon: Library },
  { to: '/serialized', label: 'Serialized', icon: Hash },
  { to: '/slabs', label: 'Slabs', icon: Award },
  { to: '/sealed', label: 'Sealed Product', icon: Package },
  { to: '/wishlist', label: 'Wishlist', icon: Heart },
  { to: '/metrics', label: 'Metrics', icon: BarChart2 },
]

const adminNav: NavItem[] = [
  { to: '/admin/users', label: 'Users', icon: Users },
  { to: '/admin/updates', label: 'Updates', icon: RefreshCw },
  { to: '/admin/backups', label: 'Backups', icon: HardDrive },
  { to: '/admin/settings', label: 'Settings', icon: Settings },
]

function NavItem({ item }: { item: NavItem }) {
  const Icon = item.icon
  return (
    <NavLink
      to={item.to}
      className={({ isActive }) =>
        cn(
          'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
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

export function Sidebar() {
  const { user, logout } = useAuth()
  const [changePasswordOpen, setChangePasswordOpen] = useState(false)
  const [profileOpen, setProfileOpen] = useState(false)
  const isAdmin = user?.role === 'Admin'
  const isLocalAccount = user !== null // OAuth users won't have password change available
  const navItems = isAdmin ? adminNav : generalUserNav
  const initials = user?.username?.slice(0, 2).toUpperCase() ?? '??'
  const avatarSrc = user?.hasAvatar ? '/api/users/me/avatar' : undefined

  return (
    <>
      <aside className="w-56 shrink-0 border-r bg-background flex flex-col">
        <div className="p-4 border-b">
          <span className="font-semibold text-sm tracking-tight">CountOrSell</span>
        </div>

        <nav className="flex-1 overflow-y-auto p-3 space-y-1">
          {navItems.map(item => (
            <NavItem key={item.to} item={item} />
          ))}

          <Separator className="my-2" />

          <NavItem item={{ to: '/about', label: 'About', icon: Info }} />
        </nav>

        <div className="p-3 border-t">
          <DropdownMenu>
            <DropdownMenuTrigger className="w-full flex items-center gap-2 rounded-md px-2 py-1.5 hover:bg-accent transition-colors text-left">
              <Avatar className="h-7 w-7">
                {avatarSrc && <AvatarImage src={avatarSrc} alt={user?.displayName ?? user?.username} />}
                <AvatarFallback className="text-xs">{initials}</AvatarFallback>
              </Avatar>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium truncate">{user?.displayName || user?.username}</p>
                <p className="text-xs text-muted-foreground">{isAdmin ? 'Admin' : 'General User'}</p>
              </div>
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
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={logout} className="text-destructive focus:text-destructive">
                <LogOut className="h-4 w-4 mr-2" />
                Sign out
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </aside>

      <ChangePasswordDialog open={changePasswordOpen} onOpenChange={setChangePasswordOpen} />
      <ProfileDialog open={profileOpen} onOpenChange={setProfileOpen} />
    </>
  )
}
