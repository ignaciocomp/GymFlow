import { useState, useEffect, useCallback } from 'react'
import { isAxiosError } from 'axios'
import { useAuth } from '@/context/AuthContext'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { MfaSetupResponse } from '@/services/api'
import { ShieldCheck, Copy, Check, Download, KeyRound } from 'lucide-react'

interface MfaSetupPageProps {
  onListo: () => void
  onCancelar: () => void
}

/**
 * Alta del segundo factor (enrolment). Muestra el QR + clave manual para escanear con la app
 * autenticadora, confirma el primer código y, al activar, presenta los códigos de recuperación
 * una sola vez antes de entrar.
 */
export default function MfaSetupPage({ onListo, onCancelar }: MfaSetupPageProps) {
  const { mfaSetup, mfaActivate } = useAuth()
  const [datos, setDatos] = useState<MfaSetupResponse | null>(null)
  const [codigo, setCodigo] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [cargando, setCargando] = useState(true)
  const [activando, setActivando] = useState(false)
  const [claveCopiada, setClaveCopiada] = useState(false)
  // Tras activar: los códigos de recuperación se muestran una única vez.
  const [codigosRecuperacion, setCodigosRecuperacion] = useState<string[] | null>(null)
  const [codigosCopiados, setCodigosCopiados] = useState(false)
  const [confirmadoGuardado, setConfirmadoGuardado] = useState(false)

  const iniciar = useCallback(() => {
    setCargando(true)
    setError(null)
    mfaSetup()
      .then(setDatos)
      .catch((e) => {
        const mensaje = isAxiosError(e) && typeof e.response?.data?.error === 'string'
          ? e.response.data.error
          : 'No se pudo iniciar la configuración del segundo factor.'
        setError(mensaje)
      })
      .finally(() => setCargando(false))
  }, [mfaSetup])

  useEffect(() => { iniciar() }, [iniciar])

  const copiarClave = async () => {
    if (!datos) return
    try {
      await navigator.clipboard.writeText(datos.claveManual)
      setClaveCopiada(true)
      setTimeout(() => setClaveCopiada(false), 2000)
    } catch { /* el portapapeles puede no estar disponible; la clave sigue visible */ }
  }

  const copiarCodigos = async () => {
    if (!codigosRecuperacion) return
    try {
      await navigator.clipboard.writeText(codigosRecuperacion.join('\n'))
      setCodigosCopiados(true)
      setTimeout(() => setCodigosCopiados(false), 2000)
    } catch { /* sin portapapeles: los códigos siguen visibles/descargables */ }
  }

  const descargarCodigos = () => {
    if (!codigosRecuperacion) return
    const contenido = `Códigos de recuperación de GymFlow\nGuardalos en un lugar seguro. Cada código sirve una sola vez.\n\n${codigosRecuperacion.join('\n')}\n`
    const blob = new Blob([contenido], { type: 'text/plain;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'gymflow-codigos-recuperacion.txt'
    a.click()
    URL.revokeObjectURL(url)
  }

  const handleActivar = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setActivando(true)
    try {
      const codigos = await mfaActivate(codigo)
      setCodigosRecuperacion(codigos)
    } catch (e) {
      const mensaje = isAxiosError(e) && typeof e.response?.data?.error === 'string'
        ? e.response.data.error
        : 'Código incorrecto o expirado.'
      setError(mensaje)
    } finally {
      setActivando(false)
    }
  }

  // Pantalla final: códigos de recuperación (una sola vez).
  if (codigosRecuperacion) {
    return (
      <div className="space-y-5">
        <div className="space-y-1.5">
          <div className="flex items-center gap-2">
            <KeyRound className="h-5 w-5 text-primary" />
            <h2 className="text-lg font-semibold text-card-foreground">Guardá tus códigos de recuperación</h2>
          </div>
          <p className="text-sm text-muted-foreground">
            Si perdés acceso a la app autenticadora, vas a poder ingresar con uno de estos códigos.
            Cada uno sirve <strong className="text-foreground">una sola vez</strong>. Guardalos en un lugar seguro:
            no los vas a volver a ver.
          </p>
        </div>

        <ul className="grid grid-cols-2 gap-2 rounded-lg border border-border bg-muted/40 p-4 font-mono text-sm tabular-nums">
          {codigosRecuperacion.map((c) => (
            <li key={c} className="text-center tracking-wider text-foreground">{c}</li>
          ))}
        </ul>

        <div className="flex gap-2">
          <Button type="button" variant="outline" className="flex-1 cursor-pointer" onClick={copiarCodigos}>
            {codigosCopiados ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
            {codigosCopiados ? 'Copiados' : 'Copiar'}
          </Button>
          <Button type="button" variant="outline" className="flex-1 cursor-pointer" onClick={descargarCodigos}>
            <Download className="h-4 w-4" />
            Descargar
          </Button>
        </div>

        <label className="flex items-start gap-2 text-sm text-muted-foreground cursor-pointer">
          <input
            type="checkbox"
            className="mt-0.5 h-4 w-4 cursor-pointer accent-[var(--primary)]"
            checked={confirmadoGuardado}
            onChange={(e) => setConfirmadoGuardado(e.target.checked)}
          />
          <span>Guardé los códigos en un lugar seguro.</span>
        </label>

        <Button
          type="button"
          className="w-full cursor-pointer"
          disabled={!confirmadoGuardado}
          onClick={onListo}
        >
          Entrar
        </Button>
      </div>
    )
  }

  // Pantalla de enrolment: QR + clave + confirmación del primer código.
  return (
    <div className="space-y-5">
      <div className="space-y-1.5">
        <div className="flex items-center gap-2">
          <ShieldCheck className="h-5 w-5 text-primary" />
          <h2 className="text-lg font-semibold text-card-foreground">Configurá el segundo factor</h2>
        </div>
        <p className="text-sm text-muted-foreground">
          Escaneá el código QR con tu app autenticadora (Google Authenticator, Microsoft Authenticator)
          y después ingresá el código de 6 dígitos para activarla.
        </p>
      </div>

      {cargando && (
        <div className="flex h-48 items-center justify-center text-sm text-muted-foreground">
          Generando el código…
        </div>
      )}

      {!cargando && !datos && (
        <div className="space-y-3">
          {error && <p className="text-sm text-destructive" role="alert">{error}</p>}
          <Button type="button" variant="outline" className="w-full cursor-pointer" onClick={iniciar}>
            Reintentar
          </Button>
        </div>
      )}

      {datos && (
        <>
          <div className="flex flex-col items-center gap-4">
            <div className="rounded-xl border border-border bg-white p-3">
              <img
                src={datos.qrDataUri}
                alt="Código QR para configurar el segundo factor"
                className="h-44 w-44"
              />
            </div>

            <div className="w-full space-y-1.5">
              <p className="text-xs text-muted-foreground">
                ¿No podés escanear? Ingresá esta clave manualmente:
              </p>
              <div className="flex items-center gap-2">
                <code className="flex-1 truncate rounded-lg border border-border bg-muted/50 px-3 py-2 font-mono text-sm tracking-wider text-foreground">
                  {datos.claveManual}
                </code>
                <Button
                  type="button"
                  variant="outline"
                  size="icon"
                  className="shrink-0 cursor-pointer"
                  onClick={copiarClave}
                  aria-label="Copiar clave"
                >
                  {claveCopiada ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                </Button>
              </div>
            </div>
          </div>

          <form onSubmit={handleActivar} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="mfa-codigo" className="text-sm text-muted-foreground">
                Código de verificación
              </Label>
              <Input
                id="mfa-codigo"
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
            </div>

            {error && <p className="text-sm text-destructive" role="alert">{error}</p>}

            <Button
              type="submit"
              className="w-full cursor-pointer"
              disabled={activando || codigo.length !== 6}
            >
              {activando ? 'Activando…' : 'Activar segundo factor'}
            </Button>
          </form>
        </>
      )}

      <button
        type="button"
        onClick={onCancelar}
        className="w-full text-center text-sm text-muted-foreground hover:text-foreground cursor-pointer"
      >
        Volver
      </button>
    </div>
  )
}
