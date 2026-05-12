import { useState } from 'react'
import { Outlet, Navigate } from 'react-router-dom'
import { useAuth } from '@/context/AuthContext'
import { Users } from 'lucide-react'
import Sidebar from './Sidebar'
import Topbar from './Topbar'

export default function AdminLayout() {
  const { user, isAuthenticated, isLoading, logout } = useAuth()
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)
  const [sidebarMobileOpen, setSidebarMobileOpen] = useState(false)
  const tieneAccesoAdmin = (user?.permisos?.length ?? 0) > 0

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

  if (!tieneAccesoAdmin && user?.rolNombre === 'Socio') {
    return <Navigate to="/portal" replace />
  }

  if (!tieneAccesoAdmin) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <div className="w-full max-w-md rounded-xl border border-border bg-card p-8 text-center">
          <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-primary/10 text-primary">
            <Users className="h-7 w-7" />
          </div>
          <h2 className="text-xl font-bold text-foreground">Acceso no disponible</h2>
          <p className="mt-2 text-sm text-muted-foreground">
            Tu rol actual no tiene acceso al panel administrativo.
          </p>
          <p className="mt-1 text-xs text-muted-foreground">
            Sesión iniciada como <span className="text-primary">{user?.nombre} {user?.apellido}</span> ({user?.rolNombre})
          </p>
          <button
            onClick={logout}
            className="mt-6 rounded-lg bg-primary px-6 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 transition-colors cursor-pointer"
          >
            Cerrar sesión
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <Sidebar
        collapsed={sidebarCollapsed}
        mobileOpen={sidebarMobileOpen}
        onMobileClose={() => setSidebarMobileOpen(false)}
      />
      <div
        className={`transition-all duration-300 ${
          // En mobile sin margin (sidebar es overlay); en desktop según collapsed
          sidebarCollapsed ? 'md:ml-16' : 'md:ml-64'
        }`}
      >
        <Topbar
          onToggleSidebar={() => setSidebarCollapsed(!sidebarCollapsed)}
          onOpenMobileSidebar={() => setSidebarMobileOpen(true)}
        />
        <main className="p-4 sm:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
