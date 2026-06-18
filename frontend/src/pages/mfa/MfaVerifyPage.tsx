import { useState } from 'react'
import { isAxiosError } from 'axios'
import { useAuth } from '@/context/AuthContext'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ShieldCheck } from 'lucide-react'

interface MfaVerifyPageProps {
  onVerificado: () => void
  onCancelar: () => void
}

/**
 * Verificación del segundo factor en el login. Pide el código de 6 dígitos de la app
 * autenticadora y permite, como alternativa, ingresar un código de recuperación.
 */
export default function MfaVerifyPage({ onVerificado, onCancelar }: MfaVerifyPageProps) {
  const { mfaVerify, mfaRecovery } = useAuth()
  const [modoRecuperacion, setModoRecuperacion] = useState(false)
  const [codigo, setCodigo] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [verificando, setVerificando] = useState(false)

  const cambiarModo = (recuperacion: boolean) => {
    setModoRecuperacion(recuperacion)
    setCodigo('')
    setError(null)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setVerificando(true)
    try {
      if (modoRecuperacion) {
        await mfaRecovery(codigo.trim())
      } else {
        await mfaVerify(codigo)
      }
      onVerificado()
    } catch (e) {
      const mensaje = isAxiosError(e) && typeof e.response?.data?.error === 'string'
        ? e.response.data.error
        : 'Código incorrecto o expirado.'
      setError(mensaje)
    } finally {
      setVerificando(false)
    }
  }

  const codigoValido = modoRecuperacion ? codigo.trim().length > 0 : codigo.length === 6

  return (
    <div className="space-y-5">
      <div className="space-y-1.5">
        <div className="flex items-center gap-2">
          <ShieldCheck className="h-5 w-5 text-primary" />
          <h2 className="text-lg font-semibold text-card-foreground">Verificá tu identidad</h2>
        </div>
        <p className="text-sm text-muted-foreground">
          {modoRecuperacion
            ? 'Ingresá uno de los códigos de recuperación que guardaste al configurar el segundo factor.'
            : 'Ingresá el código de 6 dígitos de tu app autenticadora.'}
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="mfa-verify-codigo" className="text-sm text-muted-foreground">
            {modoRecuperacion ? 'Código de recuperación' : 'Código de verificación'}
          </Label>
          {modoRecuperacion ? (
            <Input
              id="mfa-verify-codigo"
              value={codigo}
              onChange={(e) => setCodigo(e.target.value)}
              placeholder="XXXX-XXXX"
              autoComplete="one-time-code"
              className={`bg-muted/50 border-border font-mono tracking-wider text-foreground placeholder:text-muted-foreground/40 ${error ? 'border-destructive' : ''}`}
              autoFocus
              required
            />
          ) : (
            <Input
              id="mfa-verify-codigo"
              value={codigo}
              onChange={(e) => setCodigo(e.target.value.replace(/\D/g, '').slice(0, 6))}
              placeholder="000000"
              inputMode="numeric"
              autoComplete="one-time-code"
              maxLength={6}
              className={`bg-muted/50 border-border text-center font-mono text-lg tracking-[0.5em] text-foreground placeholder:tracking-[0.5em] placeholder:text-muted-foreground/40 ${error ? 'border-destructive' : ''}`}
              autoFocus
              required
            />
          )}
        </div>

        {error && <p className="text-sm text-destructive" role="alert">{error}</p>}

        <Button
          type="submit"
          className="w-full cursor-pointer"
          disabled={verificando || !codigoValido}
        >
          {verificando ? 'Verificando…' : 'Verificar'}
        </Button>
      </form>

      <div className="flex flex-col items-center gap-2">
        <button
          type="button"
          onClick={() => cambiarModo(!modoRecuperacion)}
          className="text-sm text-primary hover:underline cursor-pointer"
        >
          {modoRecuperacion ? 'Usar código de la app' : 'Usar un código de recuperación'}
        </button>
        <button
          type="button"
          onClick={onCancelar}
          className="text-sm text-muted-foreground hover:text-foreground cursor-pointer"
        >
          Volver
        </button>
      </div>
    </div>
  )
}
