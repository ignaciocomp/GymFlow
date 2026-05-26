import { Link, Outlet, Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '@/context/AuthContext'
import { Dumbbell, LogOut, User, CreditCard, CalendarDays } from 'lucide-react'

export default function SocioLayout() {
  const { user, isAuthenticated, isLoading, logout } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <p className="text-muted-foreground">Cargando...</p>
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (user?.rolNombre !== 'Socio') {
    return <Navigate to="/admin" replace />
  }

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <header className="sticky top-0 z-40 border-b border-border bg-card">
        <div className="mx-auto flex h-16 max-w-4xl items-center justify-between px-6">
          <div className="flex items-center gap-3">
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <Dumbbell className="h-5 w-5" />
            </div>
            <span className="text-lg font-bold tracking-tight text-foreground">GymFlow</span>
            <span className="ml-2 rounded-full bg-primary/10 px-2.5 py-0.5 text-xs font-medium text-primary">
              Portal del Socio
            </span>
          </div>

          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <div className="flex h-7 w-7 items-center justify-center rounded-full bg-primary/10 text-primary text-xs font-bold">
                {user.nombre.charAt(0)}{user.apellido.charAt(0)}
              </div>
              <span className="hidden sm:inline">
                {user.nombre} {user.apellido}
              </span>
            </div>
            <button
              onClick={logout}
              className="flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm text-muted-foreground hover:bg-accent hover:text-foreground transition-colors cursor-pointer"
            >
              <LogOut className="h-4 w-4" />
              <span className="hidden sm:inline">Cerrar sesión</span>
            </button>
          </div>
        </div>

        {/* Nav */}
        <nav className="mx-auto flex max-w-4xl gap-4 px-6 pb-0">
          <Link
            to="/portal/perfil"
            className={`flex items-center gap-1.5 border-b-2 px-1 pb-2 text-sm font-medium transition-colors ${
              location.pathname === '/portal' || location.pathname === '/portal/perfil'
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            <User className="h-4 w-4" />
            Mi Perfil
          </Link>
          <Link
            to="/portal/mis-cuotas"
            className={`flex items-center gap-1.5 border-b-2 px-1 pb-2 text-sm font-medium transition-colors ${
              location.pathname === '/portal/mis-cuotas'
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            <CreditCard className="h-4 w-4" />
            Mis Cuotas
          </Link>
          <Link
            to="/portal/horarios"
            className={`flex items-center gap-1.5 border-b-2 px-1 pb-2 text-sm font-medium transition-colors ${
              location.pathname === '/portal/horarios'
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            <CalendarDays className="h-4 w-4" />
            Horarios
          </Link>
        </nav>
      </header>

      <main className="mx-auto max-w-4xl px-6 py-8">
        <Outlet />
      </main>
    </div>
  )
}
