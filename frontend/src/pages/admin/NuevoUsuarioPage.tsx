import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { crearEmpleado } from '@/services/empleados'
import { listarRoles } from '@/services/roles'
import type { Rol } from '@/types/permisos'

export default function NuevoUsuarioPage() {
  const navigate = useNavigate()
  const [roles, setRoles] = useState<Rol[]>([])
  const [form, setForm] = useState({ nombre: '', apellido: '', correo: '', password: '', rolId: '' })
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    listarRoles().then(rs => setRoles(rs.filter(r => r.nombre !== 'Socio')))
  }, [])

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSaving(true)
    try {
      await crearEmpleado(form)
      navigate('/admin/usuarios')
    } catch (err: unknown) {
      const reqError = err as { response?: { data?: { error?: string } } }
      setError(reqError?.response?.data?.error ?? 'Error al crear usuario')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="max-w-xl space-y-4">
      <h1 className="text-2xl font-bold">Nuevo usuario</h1>
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
          <label className="block text-sm mb-1">Contraseña inicial (mínimo 8 caracteres)</label>
          <input type="password" className="w-full rounded border px-3 py-2 bg-background" value={form.password} onChange={e => setForm({ ...form, password: e.target.value })} minLength={8} required />
        </div>
        <div>
          <label className="block text-sm mb-1">Rol</label>
          <select className="w-full rounded border px-3 py-2 bg-background" value={form.rolId} onChange={e => setForm({ ...form, rolId: e.target.value })} required>
            <option value="">Seleccionar...</option>
            {roles.map(r => <option key={r.id} value={r.id}>{r.nombre}</option>)}
          </select>
        </div>
        <div className="flex gap-2 pt-2">
          <Button type="submit" disabled={saving}>{saving ? 'Guardando...' : 'Crear'}</Button>
          <Button type="button" variant="outline" onClick={() => navigate('/admin/usuarios')}>Cancelar</Button>
        </div>
      </form>
    </div>
  )
}
