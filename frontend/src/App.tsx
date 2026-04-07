import { Routes, Route, Navigate } from 'react-router-dom'
import Login from '@/pages/Login'
import AdminLayout from '@/components/layout/AdminLayout'
import SociosPage from '@/pages/admin/SociosPage'
import NuevoSocioPage from '@/pages/admin/NuevoSocioPage'
import EditSocioPage from '@/pages/admin/EditSocioPage'
import AuditoriaPage from '@/pages/admin/AuditoriaPage'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/admin" element={<AdminLayout />}>
        <Route index element={<Navigate to="socios" replace />} />
        <Route path="dashboard" element={<Navigate to="/admin/socios" replace />} />
        <Route path="socios" element={<SociosPage />} />
        <Route path="socios/nuevo" element={<NuevoSocioPage />} />
        <Route path="socios/:id/editar" element={<EditSocioPage />} />
        <Route path="socios/inactivos" element={<Navigate to="/admin/socios?tab=inactivos" replace />} />
        <Route path="auditoria" element={<AuditoriaPage />} />
      </Route>
      <Route path="/" element={<Navigate to="/login" replace />} />
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  )
}
