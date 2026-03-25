import { useAuth } from '@/context/AuthContext'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useQuery } from '@tanstack/react-query'
import { unidadesApi } from '@/services/api'
import { Menu, LogOut, User } from 'lucide-react'

interface TopbarProps {
  onToggleSidebar: () => void
  selectedUnidad: string
  onUnidadChange: (value: string) => void
}

export default function Topbar({ onToggleSidebar, selectedUnidad, onUnidadChange }: TopbarProps) {
  const { user, logout } = useAuth()

  const { data: unidades } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  return (
    <header className="sticky top-0 z-30 flex h-16 items-center gap-4 border-b border-border bg-card/80 backdrop-blur-sm px-6">
      {/* Hamburger */}
      <button
        onClick={onToggleSidebar}
        className="flex h-9 w-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-accent hover:text-foreground transition-colors cursor-pointer"
      >
        <Menu className="h-5 w-5" />
      </button>

      {/* Spacer */}
      <div className="flex-1" />

      {/* Unit Toggle */}
      <Select value={selectedUnidad} onValueChange={(val) => onUnidadChange(val ?? 'all')}>
        <SelectTrigger className="w-[220px] bg-muted/50 border-border text-foreground">
          <SelectValue placeholder="Seleccionar unidad" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">Ambas Unidades</SelectItem>
          {unidades?.map((u) => (
            <SelectItem key={u.id} value={u.id}>
              {u.nombre}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      {/* User menu */}
      <div className="flex items-center gap-3">
        <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-primary">
          <User className="h-4 w-4" />
        </div>
        <div className="hidden md:block">
          <p className="text-sm font-medium text-foreground">
            {user?.nombre} {user?.apellido}
          </p>
          <p className="text-xs text-muted-foreground">{user?.rol}</p>
        </div>
        <button
          onClick={logout}
          className="flex h-9 w-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-destructive/10 hover:text-destructive transition-colors cursor-pointer"
          title="Cerrar sesión"
        >
          <LogOut className="h-4 w-4" />
        </button>
      </div>
    </header>
  )
}
