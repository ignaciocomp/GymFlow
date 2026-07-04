import { Routes, Route, Navigate } from 'react-router-dom'
import Login from '@/pages/Login'
import AdminLayout from '@/components/layout/AdminLayout'
import SocioLayout from '@/components/layout/SocioLayout'
import SociosPage from '@/pages/admin/SociosPage'
import NuevoSocioPage from '@/pages/admin/NuevoSocioPage'
import EditSocioPage from '@/pages/admin/EditSocioPage'
import PlanesPage from '@/pages/admin/PlanesPage'
import NuevoPlanPage from '@/pages/admin/NuevoPlanPage'
import EditPlanPage from '@/pages/admin/EditPlanPage'
import AuditoriaPage from '@/pages/admin/AuditoriaPage'
import RolesPage from '@/pages/admin/RolesPage'
import NuevoRolPage from '@/pages/admin/NuevoRolPage'
import EditRolPage from '@/pages/admin/EditRolPage'
import UsuariosPage from '@/pages/admin/UsuariosPage'
import NuevoUsuarioPage from '@/pages/admin/NuevoUsuarioPage'
import EditUsuarioPage from '@/pages/admin/EditUsuarioPage'
import CambiarPasswordPage from '@/pages/admin/CambiarPasswordPage'
import PerfilSocioPage from '@/pages/portal/PerfilSocioPage'
import MisCuotasPage from '@/pages/portal/MisCuotasPage'
import CuotasPage from '@/pages/admin/CuotasPage'
import SociosCuotasPage from '@/pages/admin/SociosCuotasPage'
import ClasesPage from '@/pages/admin/ClasesPage'
import NuevaClasePage from '@/pages/admin/NuevaClasePage'
import EditClasePage from '@/pages/admin/EditClasePage'
import HorariosPage from '@/pages/admin/HorariosPage'
import EventosPage from '@/pages/admin/EventosPage'
import HorariosPortalPage from '@/pages/portal/HorariosPortalPage'
import EventosPortalPage from '@/pages/portal/EventosPortalPage'
import MisInscripcionesPage from '@/pages/portal/MisInscripcionesPage'
import NotificacionesPortalPage from '@/pages/portal/NotificacionesPortalPage'
import MisPagosPage from '@/pages/portal/MisPagosPage'
import PagoResultadoPage from '@/pages/portal/PagoResultadoPage'
import PublicLayout from '@/components/public/PublicLayout'
import HomePage from '@/pages/public/HomePage'
import SedesPage from '@/pages/public/SedesPage'
import PlanesPublicPage from '@/pages/public/PlanesPublicPage'
import ClasesPublicPage from '@/pages/public/ClasesPublicPage'
import ContactoPage from '@/pages/public/ContactoPage'
import NotFoundPage from '@/pages/public/NotFoundPage'

export default function App() {
  return (
    <Routes>
      <Route element={<PublicLayout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/sedes" element={<SedesPage />} />
        <Route path="/planes" element={<PlanesPublicPage />} />
        <Route path="/clases" element={<ClasesPublicPage />} />
        <Route path="/contacto" element={<ContactoPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
      <Route path="/login" element={<Login />} />
      <Route path="/admin" element={<AdminLayout />}>
        <Route index element={<Navigate to="socios" replace />} />
        <Route path="dashboard" element={<Navigate to="/admin/socios" replace />} />
        <Route path="socios" element={<SociosPage />} />
        <Route path="socios/nuevo" element={<NuevoSocioPage />} />
        <Route path="socios/:id/editar" element={<EditSocioPage />} />
        <Route path="socios/inactivos" element={<Navigate to="/admin/socios?tab=inactivos" replace />} />
        <Route path="planes" element={<PlanesPage />} />
        <Route path="planes/nuevo" element={<NuevoPlanPage />} />
        <Route path="planes/:id/editar" element={<EditPlanPage />} />
        <Route path="auditoria" element={<AuditoriaPage />} />
        <Route path="roles" element={<RolesPage />} />
        <Route path="roles/nuevo" element={<NuevoRolPage />} />
        <Route path="roles/:id/editar" element={<EditRolPage />} />
        <Route path="usuarios" element={<UsuariosPage />} />
        <Route path="usuarios/nuevo" element={<NuevoUsuarioPage />} />
        <Route path="usuarios/:id/editar" element={<EditUsuarioPage />} />
        <Route path="usuarios/:id/password" element={<CambiarPasswordPage />} />
        <Route path="cuotas" element={<SociosCuotasPage />} />
        <Route path="cuotas/:socioId" element={<CuotasPage />} />
        <Route path="clases" element={<ClasesPage />} />
        <Route path="clases/nueva" element={<NuevaClasePage />} />
        <Route path="clases/:id/editar" element={<EditClasePage />} />
        <Route path="horarios" element={<HorariosPage />} />
        <Route path="eventos" element={<EventosPage />} />
      </Route>
      <Route path="/portal" element={<SocioLayout />}>
        <Route index element={<PerfilSocioPage />} />
        <Route path="perfil" element={<PerfilSocioPage />} />
        <Route path="mis-cuotas" element={<MisCuotasPage />} />
        <Route path="mis-pagos" element={<MisPagosPage />} />
        <Route path="pago/resultado" element={<PagoResultadoPage />} />
        <Route path="horarios" element={<HorariosPortalPage />} />
        <Route path="eventos" element={<EventosPortalPage />} />
        <Route path="mis-inscripciones" element={<MisInscripcionesPage />} />
        <Route path="notificaciones" element={<NotificacionesPortalPage />} />
      </Route>
    </Routes>
  )
}

