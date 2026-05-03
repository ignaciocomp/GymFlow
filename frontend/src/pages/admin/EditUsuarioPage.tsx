import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { obtenerEmpleado, actualizarEmpleado } from '@/services/empleados'
import { listarRoles } from '@/services/roles'
import type { Rol } from '@/types/permisos'

export default function EditUsuarioPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [roles, setRoles] = useState<Rol[]>([])
  const [form, setForm] = useState({ nombre: '', apellido: '', correo: '', rolId: '' })
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!id) return
    Promise.all([obtenerEmpleado(id), listarRoles()])
      .then(([emp, rs]) => {
        setForm({ nombre: emp.nombre, apellido: emp.apellido, correo: emp.correo, rolId: emp.rolId })
        setRoles(rs.filter(r => r.nombre !== 'Socio'))
      })
      .catch(e => setError(e?.response?.data?.error ?? 'Error al cargar'))
      .finally(() => setLoading(false))
  }, [id])

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!id) return
    setError(null)
    setSaving(true)
    try {
      await actualizarEmpleado(id, form)
      navigate('/admin/usuarios')
    } catch (err: unknown) {
      const reqError = err as { response?: { data?: { error?: string } } }
      setError(reqError?.response?.data?.error ?? 'Error al guardar')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <div className="p-6">Cargando...</div>

  return (
    <div className="max-w-xl space-y-4">
      <h1 className="text-2xl font-bold">Editar usuario</h1>
      {error && <div className="rounded bg-destructive/10 text-destructive p-3 text-sm">{error}</div>}
      <form onSubmit={onSubmit} className="space-y-3">
        <div>
          <label className="block text-sm mb-1">Nombre</label>
          <input className="w-full rounded border px-3 py-2 bg-background" value={form.nombre} onChange={e => setForm({ ...form, nombre: e.target.value })} required />
        </div>
        <div>
          <label className="block text-sm mb-1">Apellido</label>
          <input className="w-full rounded border px-3 py-2 bg-background" value={form.apellido} onChange={e => setForm({ ...form, apellido: e.target.value })} required />
        </div>
        <div>
          <label className="block text-sm mb-1">Correo</label>
          <input type="email" className="w-full rounded border px-3 py-2 bg-background" value={form.correo} onChange={e => setForm({ ...form, correo: e.target.value })} required />
        </div>
        <div>
          <label className="block text-sm mb-1">Rol</label>
          <select className="w-full rounded border px-3 py-2 bg-background" value={form.rolId} onChange={e => setForm({ ...form, rolId: e.target.value })} required>
            <option value="">Seleccionar...</option>
            {roles.map(r => <option key={r.id} value={r.id}>{r.nombre}</option>)}
          </select>
        </div>
        <div className="flex gap-2 pt-2">
          <Button type="submit" disabled={saving}>{saving ? 'Guardando...' : 'Guardar'}</Button>
          <Button type="button" variant="outline" onClick={() => navigate('/admin/usuarios')}>Cancelar</Button>
        </div>
      </form>
    </div>
  )
}
