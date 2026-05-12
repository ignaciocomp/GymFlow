import { useAuth } from '@/context/AuthContext'
import { Menu, LogOut, User } from 'lucide-react'

interface TopbarProps {
  onToggleSidebar: () => void
  onOpenMobileSidebar?: () => void
}

export default function Topbar({ onToggleSidebar, onOpenMobileSidebar }: TopbarProps) {
  const { user, logout } = useAuth()

  const handleHamburger = () => {
    // En mobile (< md) abre el drawer; en desktop colapsa/expande
    if (window.innerWidth < 768) {
      onOpenMobileSidebar?.()
    } else {
      onToggleSidebar()
    }
  }

  return (
    <header className="sticky top-0 z-20 flex h-16 items-center gap-4 border-b border-border bg-card/80 backdrop-blur-sm px-4 sm:px-6">
      {/* Hamburger */}
      <button
        onClick={handleHamburger}
        className="flex h-9 w-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-accent hover:text-foreground transition-colors cursor-pointer"
        aria-label="Abrir menú"
      >
        <Menu className="h-5 w-5" />
      </button>

      {/* Spacer */}
      <div className="flex-1" />

      {/* User menu */}
      <div className="flex items-center gap-2 sm:gap-3">
        <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-primary">
          <User className="h-4 w-4" />
        </div>
        <div className="hidden md:block">
          <p className="text-sm font-medium text-foreground">
            {user?.nombre} {user?.apellido}
          </p>
          <p className="text-xs text-muted-foreground">{user?.rolNombre}</p>
        </div>
        <button
          onClick={logout}
          className="flex h-9 w-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-destructive/10 hover:text-destructive transition-colors cursor-pointer"
          title="Cerrar sesión"
          aria-label="Cerrar sesión"
        >
          <LogOut className="h-4 w-4" />
        </button>
      </div>
    </header>
  )
}
