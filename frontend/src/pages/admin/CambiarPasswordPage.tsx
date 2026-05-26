import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { cambiarPasswordEmpleado } from '@/services/empleados'

export default function CambiarPasswordPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [pw, setPw] = useState('')
  const [confirmPw, setConfirmPw] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!id) return
    if (pw !== confirmPw) {
      setError('Las contraseñas no coinciden.')
      return
    }

    setError(null)
    setSaving(true)
    try {
      await cambiarPasswordEmpleado(id, { nuevaPassword: pw })
      navigate('/admin/usuarios')
    } catch (err: unknown) {
      const reqError = err as { response?: { data?: { error?: string } } }
      setError(reqError?.response?.data?.error ?? 'Error al cambiar contraseña')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="max-w-md space-y-4">
      <h1 className="text-2xl font-bold">Cambiar contraseña</h1>
      {error && <div className="rounded bg-destructive/10 text-destructive p-3 text-sm">{error}</div>}
      <form onSubmit={onSubmit} className="space-y-3">
        <div>
          <label className="block text-sm mb-1">Nueva contraseña (mínimo 8 caracteres)</label>
          <input type="password" className="w-full rounded border px-3 py-2 bg-background" value={pw} onChange={e => setPw(e.target.value)} minLength={8} required />
        </div>
        <div>
          <label className="block text-sm mb-1">Confirmar</label>
          <input type="password" className="w-full rounded border px-3 py-2 bg-background" value={confirmPw} onChange={e => setConfirmPw(e.target.value)} minLength={8} required />
        </div>
        <div className="flex gap-2 pt-2">
          <Button type="submit" disabled={saving}>{saving ? 'Guardando...' : 'Cambiar'}</Button>
          <Button type="button" variant="outline" onClick={() => navigate('/admin/usuarios')}>Cancelar</Button>
        </div>
      </form>
    </div>
  )
}
