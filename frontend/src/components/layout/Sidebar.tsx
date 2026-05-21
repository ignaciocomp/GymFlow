import { useState } from 'react'
import { Link, useLocation, useSearchParams } from 'react-router-dom'
import {
  LayoutDashboard,
  Users,
  UserPlus,
  UserX,
  UserCog,
  CreditCard,
  Dumbbell,
  ChevronDown,
  ChevronRight,
  ClipboardList,
  Shield,
  Receipt,
  BookOpen,
  CalendarDays,
} from 'lucide-react'
import { usePermisos } from '@/hooks/usePermisos'
import type { Modulo } from '@/types/permisos'

interface NavGroup {
  label: string
  icon: React.ReactNode
  modulo: Modulo
  items: { label: string; path: string; icon: React.ReactNode }[]
}

const navigation: NavGroup[] = [
  {
    label: 'Socios',
    icon: <Users className="h-5 w-5" />,
    modulo: 'Socios',
    items: [
      { label: 'Nuevo socio', path: '/admin/socios/nuevo', icon: <UserPlus className="h-4 w-4" /> },
      { label: 'Socios activos', path: '/admin/socios', icon: <Users className="h-4 w-4" /> },
      { label: 'Socios inactivos', path: '/admin/socios?tab=inactivos', icon: <UserX className="h-4 w-4" /> },
    ],
  },
  {
    label: 'Planes',
    icon: <CreditCard className="h-5 w-5" />,
    modulo: 'Planes',
    items: [
      { label: 'Nuevo plan', path: '/admin/planes/nuevo', icon: <CreditCard className="h-4 w-4" /> },
      { label: 'Lista de planes', path: '/admin/planes', icon: <CreditCard className="h-4 w-4" /> },
    ],
  },
  {
    label: 'Clases',
    icon: <BookOpen className="h-5 w-5" />,
    modulo: 'Clases',
    items: [
      { label: 'Nueva clase', path: '/admin/clases/nueva', icon: <BookOpen className="h-4 w-4" /> },
      { label: 'Lista de clases', path: '/admin/clases', icon: <BookOpen className="h-4 w-4" /> },
      { label: 'Horarios', path: '/admin/horarios', icon: <CalendarDays className="h-4 w-4" /> },
    ],
  },
  {
    label: 'Cuotas',
    icon: <Receipt className="h-5 w-5" />,
    modulo: 'Cuotas',
    items: [
      { label: 'Gestión de cuotas', path: '/admin/cuotas', icon: <Receipt className="h-4 w-4" /> },
    ],
  },
  {
    label: 'Sistema',
    icon: <ClipboardList className="h-5 w-5" />,
    modulo: 'Empleados',
    items: [
      { label: 'Usuarios', path: '/admin/usuarios', icon: <UserCog className="h-4 w-4" /> },
      { label: 'Auditoría', path: '/admin/auditoria', icon: <ClipboardList className="h-4 w-4" /> },
      { label: 'Roles', path: '/admin/roles', icon: <Shield className="h-4 w-4" /> },
    ],
  },
]

interface SidebarProps {
  collapsed: boolean
  mobileOpen: boolean
  onMobileClose: () => void
}

export default function Sidebar({ collapsed, mobileOpen, onMobileClose }: SidebarProps) {
  const location = useLocation()
  const [searchParams] = useSearchParams()
  const [openGroups, setOpenGroups] = useState<string[]>(['Socios'])
  const { puedeLeer } = usePermisos()
  const visibleGroups = navigation.filter(g => puedeLeer(g.modulo))

  const toggleGroup = (label: string) => {
    setOpenGroups((prev) =>
      prev.includes(label) ? prev.filter((g) => g !== label) : [...prev, label]
    )
  }

  const handleLinkClick = () => {
    // En mobile, cerrar el drawer al hacer click en un link
    if (window.innerWidth < 768) onMobileClose()
  }

  // En mobile el sidebar siempre se muestra expandido (con texto), aunque el
  // estado `collapsed` esté activo (que es relevante solo para desktop).
  const showLabels = mobileOpen || !collapsed

  const isActive = (path: string) => {
    if (path === '/admin/socios') {
      return location.pathname.startsWith('/admin/socios') &&
        location.pathname !== '/admin/socios/nuevo' &&
        searchParams.get('tab') !== 'inactivos'
    }
    if (path === '/admin/socios?tab=inactivos') {
      return location.pathname === '/admin/socios' && searchParams.get('tab') === 'inactivos'
    }
    if (path === '/admin/planes') {
      return location.pathname.startsWith('/admin/planes') &&
        location.pathname !== '/admin/planes/nuevo'
    }
    if (path === '/admin/usuarios') {
      return location.pathname.startsWith('/admin/usuarios') &&
        location.pathname !== '/admin/usuarios/nuevo'
    }
    if (path === '/admin/clases') {
      return location.pathname.startsWith('/admin/clases') &&
        location.pathname !== '/admin/clases/nueva' &&
        location.pathname !== '/admin/horarios'
    }
    if (path === '/admin/horarios') {
      return location.pathname === '/admin/horarios'
    }
    return location.pathname === path
  }

  return (
    <>
      {/* Backdrop solo en mobile cuando está abierto */}
      {mobileOpen && (
        <div
          onClick={onMobileClose}
          className="fixed inset-0 z-30 bg-black/60 backdrop-blur-sm md:hidden"
          aria-hidden="true"
        />
      )}

      <aside
        className={`fixed left-0 top-0 z-40 h-screen bg-sidebar border-r border-sidebar-border transition-all duration-300 ${
          // Desktop (md+): siempre visible, ancho según collapsed
          collapsed ? 'md:w-16' : 'md:w-64'
        } ${
          // Mobile: drawer que entra/sale desde la izquierda
          mobileOpen ? 'w-64 translate-x-0' : '-translate-x-full md:translate-x-0'
        }`}
      >
      <div className="flex h-16 items-center gap-3 border-b border-sidebar-border px-4">
        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
          <Dumbbell className="h-5 w-5" />
        </div>
        {showLabels && (
          <span className="text-lg font-bold text-foreground tracking-tight">GymFlow</span>
        )}
      </div>

      <nav className="mt-4 flex flex-col gap-1 px-3">
        <Link
          to="/admin/dashboard"
          onClick={handleLinkClick}
          className={`flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors cursor-pointer ${
            isActive('/admin/dashboard')
              ? 'bg-sidebar-accent text-primary border-l-3 border-primary'
              : 'text-sidebar-foreground hover:bg-sidebar-accent/50 hover:text-foreground'
          }`}
        >
          <LayoutDashboard className="h-5 w-5 shrink-0" />
          {showLabels && <span>Inicio</span>}
        </Link>

        {visibleGroups.map((group) => {
          const isOpen = openGroups.includes(group.label)
          return (
            <div key={group.label}>
              <button
                onClick={() => toggleGroup(group.label)}
                className="flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-sidebar-foreground hover:bg-sidebar-accent/50 hover:text-foreground transition-colors cursor-pointer"
              >
                <span className="shrink-0">{group.icon}</span>
                {showLabels && (
                  <>
                    <span className="flex-1 text-left">{group.label}</span>
                    {isOpen ? (
                      <ChevronDown className="h-4 w-4 text-muted-foreground" />
                    ) : (
                      <ChevronRight className="h-4 w-4 text-muted-foreground" />
                    )}
                  </>
                )}
              </button>
              {isOpen && showLabels && (
                <div className="ml-4 mt-1 flex flex-col gap-0.5 border-l border-sidebar-border pl-3">
                  {group.items.map((item) => (
                    <Link
                      key={item.path}
                      to={item.path}
                      onClick={handleLinkClick}
                      className={`flex items-center gap-2.5 rounded-md px-3 py-2 text-sm transition-colors cursor-pointer ${
                        isActive(item.path)
                          ? 'text-primary bg-sidebar-accent font-medium'
                          : 'text-muted-foreground hover:text-foreground hover:bg-sidebar-accent/40'
                      }`}
                    >
                      {item.icon}
                      <span>{item.label}</span>
                    </Link>
                  ))}
                </div>
              )}
            </div>
          )
        })}
      </nav>
    </aside>
    </>
  )
}


