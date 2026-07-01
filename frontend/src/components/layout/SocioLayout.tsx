import { Link, Outlet, Navigate, useLocation } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { useAuth } from '@/context/AuthContext'
import { portalApi } from '@/services/api'
import { Dumbbell, LogOut, User, CreditCard, CalendarDays, BookOpen, PartyPopper, Bell, Receipt } from 'lucide-react'

export default function SocioLayout() {
  const { user, isAuthenticated, isLoading, logout } = useAuth()
  const location = useLocation()

  const esSocio = user?.rolNombre === 'Socio'

  const { data: noLeidas } = useQuery({
    queryKey: ['notif-count'],
    queryFn: portalApi.contarNoLeidas,
    enabled: esSocio,
    refetchInterval: 45_000,
    refetchOnWindowFocus: true,
  })

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

          <div className="flex items-center gap-3 sm:gap-4">
            <Link
              to="/portal/notificaciones"
              aria-label={
                noLeidas && noLeidas > 0
                  ? `Notificaciones, ${noLeidas} sin leer`
                  : 'Notificaciones'
              }
              className={`relative flex h-9 w-9 items-center justify-center rounded-lg transition-colors cursor-pointer ${
                location.pathname === '/portal/notificaciones'
                  ? 'bg-primary/10 text-primary'
                  : 'text-muted-foreground hover:bg-accent hover:text-foreground'
              }`}
            >
              <Bell className="h-5 w-5" />
              {noLeidas != null && noLeidas > 0 && (
                <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-primary px-1 text-[10px] font-bold leading-none text-primary-foreground tabular-nums">
                  {noLeidas > 99 ? '99+' : noLeidas}
                </span>
              )}
            </Link>
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
            to="/portal/mis-pagos"
            className={`flex items-center gap-1.5 border-b-2 px-1 pb-2 text-sm font-medium transition-colors ${
              location.pathname === '/portal/mis-pagos'
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            <Receipt className="h-4 w-4" />
            Mis Pagos
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
          <Link
            to="/portal/eventos"
            className={`flex items-center gap-1.5 border-b-2 px-1 pb-2 text-sm font-medium transition-colors ${
              location.pathname === '/portal/eventos'
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            <PartyPopper className="h-4 w-4" />
            Eventos
          </Link>
          <Link
            to="/portal/mis-inscripciones"
            className={`flex items-center gap-1.5 border-b-2 px-1 pb-2 text-sm font-medium transition-colors ${
              location.pathname === '/portal/mis-inscripciones'
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            <BookOpen className="h-4 w-4" />
            Mis Inscripciones
          </Link>
          <Link
            to="/portal/notificaciones"
            className={`flex items-center gap-1.5 border-b-2 px-1 pb-2 text-sm font-medium transition-colors ${
              location.pathname === '/portal/notificaciones'
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            <Bell className="h-4 w-4" />
            Notificaciones
            {noLeidas != null && noLeidas > 0 && (
              <span className="ml-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-primary px-1 text-[10px] font-bold leading-none text-primary-foreground tabular-nums">
                {noLeidas > 99 ? '99+' : noLeidas}
              </span>
            )}
          </Link>
        </nav>
      </header>

      <main className="mx-auto max-w-4xl px-6 py-8">
        <Outlet />
      </main>
    </div>
  )
}
