import { useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { listarPermisos } from '@/services/permisos'
import type { Permiso, Modulo, Operacion } from '@/types/permisos'

interface Props {
  initialNombre?: string
  initialPermisoIds?: string[]
  onSubmit: (data: { nombre: string; permisoIds: string[] }) => Promise<void>
  submitLabel: string
}

const MODULOS: Modulo[] = ['Socios', 'Planes', 'Unidades', 'Auditoria', 'Empleados', 'Cuotas', 'Clases']
const OPERACIONES: Operacion[] = ['Lectura', 'Escritura', 'Modificacion', 'Eliminacion']

export default function RolForm({ initialNombre = '', initialPermisoIds = [], onSubmit, submitLabel }: Props) {
  const [nombre, setNombre] = useState(initialNombre)
  const [permisos, setPermisos] = useState<Permiso[]>([])
  const [seleccionados, setSeleccionados] = useState<Set<string>>(new Set(initialPermisoIds))
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => { listarPermisos().then(setPermisos) }, [])
  useEffect(() => { setSeleccionados(new Set(initialPermisoIds)) }, [initialPermisoIds.join(',')])

  const idDe = (m: Modulo, o: Operacion) => permisos.find(p => p.modulo === m && p.operacion === o)?.id

  const toggle = (id: string | undefined) => {
    if (!id) return
    setSeleccionados(prev => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await onSubmit({ nombre, permisoIds: Array.from(seleccionados) })
    } catch (err: unknown) {
      const e = err as { response?: { data?: { error?: string } } }
      setError(e?.response?.data?.error ?? 'Error al guardar')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div>
        <label className="block text-sm font-medium mb-1">Nombre del rol</label>
        <Input value={nombre} onChange={e => setNombre(e.target.value)} required />
      </div>
      <div>
        <h2 className="text-lg font-semibold mb-2">Permisos</h2>
        <div className="rounded-lg border border-border bg-card overflow-hidden">
          <table className="w-full">
            <thead>
              <tr className="border-b border-border bg-muted/40">
                <th className="text-left py-2 px-4 text-sm font-medium">Módulo</th>
                {OPERACIONES.map(o => (
                  <th key={o} className="text-center py-2 px-2 text-sm font-medium">{o}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {MODULOS.map(m => (
                <tr key={m} className="border-b border-border last:border-0">
                  <td className="py-2 px-4">{m}</td>
                  {OPERACIONES.map(o => {
                    const id = idDe(m, o)
                    return (
                      <td key={o} className="text-center py-2 px-2">
                        <input
                          type="checkbox"
                          checked={!!id && seleccionados.has(id)}
                          onChange={() => toggle(id)}
                          disabled={!id}
                          className="h-4 w-4 cursor-pointer"
                        />
                      </td>
                    )
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
      {error && <p className="text-destructive text-sm">{error}</p>}
      <Button type="submit" disabled={submitting}>{submitLabel}</Button>
    </form>
  )
}
