import { useState } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { portalApi } from '@/services/api'
import { formatDate } from '@/lib/utils'
import { useAuth } from '@/context/AuthContext'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter,
} from '@/components/ui/dialog'
import {
  User, CreditCard, Building2, Pencil, AlertTriangle, CheckCircle2, Calendar, Phone, FileText,
} from 'lucide-react'
import type { TipoDocumento } from '@/types'

const tipoDocumentoLabel: Record<TipoDocumento, string> = {
  CI: 'Cédula de identidad',
  Pasaporte: 'Pasaporte',
  Otro: 'Otro',
}


function InfoRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5 py-2.5">
      <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</span>
      <span className="text-sm text-foreground">{value || '—'}</span>
    </div>
  )
}

export default function PerfilSocioPage() {
  const [showModificacion, setShowModificacion] = useState(false)
  const [showBaja, setShowBaja] = useState(false)
  const [detalle, setDetalle] = useState('')
  const [motivoBaja, setMotivoBaja] = useState('')
  const [successMsg, setSuccessMsg] = useState<string | null>(null)
  const [errorMsg, setErrorMsg] = useState<string | null>(null)

  const { logout } = useAuth()

  const { data: perfil, isLoading, isError } = useQuery({
    queryKey: ['portal-perfil'],
    queryFn: portalApi.getPerfil,
  })

  const modificacionMutation = useMutation({
    mutationFn: () => portalApi.solicitarModificacion({ detalle }),
    onSuccess: (res) => {
      setShowModificacion(false)
      setDetalle('')
      setSuccessMsg(res.mensaje)
      setErrorMsg(null)
    },
    onError: () => setErrorMsg('No se pudo registrar la solicitud. Intentá de nuevo.'),
  })

  const bajaMutation = useMutation({
    mutationFn: () => portalApi.solicitarBaja({ motivo: motivoBaja || null }),
    onSuccess: () => {
      setShowBaja(false)
      setMotivoBaja('')
      setTimeout(() => logout(), 3000)
      setSuccessMsg('Tu solicitud de baja fue registrada. Cerrando sesión...')
      setErrorMsg(null)
    },
    onError: () => setErrorMsg('No se pudo registrar la solicitud. Intentá de nuevo.'),
  })

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <p className="text-muted-foreground">Cargando tu perfil...</p>
      </div>
    )
  }

  if (isError || !perfil) {
    return (
      <div className="rounded-xl border border-destructive/50 bg-destructive/10 p-6 text-center">
        <p className="text-sm text-destructive">No se pudo cargar tu perfil. Por favor intentá de nuevo.</p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Page header */}
      <div className="flex items-center gap-4">
        <div className="flex h-14 w-14 items-center justify-center rounded-full bg-primary/10 text-primary text-xl font-bold">
          {perfil.nombre.charAt(0)}{perfil.apellido.charAt(0)}
        </div>
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">
            {perfil.nombre} {perfil.apellido}
          </h1>
          <p className="text-sm text-muted-foreground">{perfil.correo}</p>
        </div>
      </div>

      {/* Feedback banners */}
      {successMsg && (
        <div className="flex items-start gap-3 rounded-xl border border-green-200 bg-green-50 p-4">
          <CheckCircle2 className="mt-0.5 h-5 w-5 shrink-0 text-green-600" />
          <p className="text-sm text-green-800">{successMsg}</p>
        </div>
      )}
      {errorMsg && (
        <div className="flex items-start gap-3 rounded-xl border border-destructive/40 bg-destructive/10 p-4">
          <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0 text-destructive" />
          <p className="text-sm text-destructive">{errorMsg}</p>
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Datos personales */}
        <div className="rounded-xl border border-border bg-card p-6 space-y-1">
          <div className="mb-4 flex items-center gap-2">
            <User className="h-4 w-4 text-primary" />
            <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
              Datos personales
            </h2>
          </div>
          <div className="divide-y divide-border">
            <InfoRow label="Nombre" value={`${perfil.nombre} ${perfil.apellido}`} />
            <InfoRow label="Correo electrónico" value={perfil.correo} />
            <InfoRow
              label="Teléfono"
              value={
                perfil.telefono ? (
                  <span className="flex items-center gap-1.5">
                    <Phone className="h-3.5 w-3.5 text-muted-foreground" />
                    {perfil.telefono}
                  </span>
                ) : null
              }
            />
            <InfoRow
              label="Documento"
              value={
                perfil.tipoDocumento && perfil.documentoIdentidad ? (
                  <span className="flex items-center gap-1.5">
                    <FileText className="h-3.5 w-3.5 text-muted-foreground" />
                    {tipoDocumentoLabel[perfil.tipoDocumento]} — {perfil.documentoIdentidad}
                  </span>
                ) : null
              }
            />
            <InfoRow
              label="Fecha de nacimiento"
              value={
                perfil.fechaNacimiento ? (
                  <span className="flex items-center gap-1.5">
                    <Calendar className="h-3.5 w-3.5 text-muted-foreground" />
                    {formatDate(perfil.fechaNacimiento)}
                  </span>
                ) : null
              }
            />
            <InfoRow
              label="Miembro desde"
              value={
                <span className="flex items-center gap-1.5">
                  <Calendar className="h-3.5 w-3.5 text-muted-foreground" />
                  {formatDate(perfil.fechaAlta)}
                </span>
              }
            />
          </div>
        </div>

        {/* Plan y acceso */}
        <div className="rounded-xl border border-border bg-card p-6 space-y-4">
          <div className="flex items-center gap-2">
            <CreditCard className="h-4 w-4 text-primary" />
            <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
              Plan y acceso
            </h2>
          </div>

          {perfil.unidades.length === 0 ? (
            <p className="text-sm text-muted-foreground">Sin unidades asignadas.</p>
          ) : (
            <div className="space-y-3">
              {perfil.unidades.map((u) => (
                <div key={u.id} className="rounded-lg border border-border bg-muted/30 p-4">
                  <div className="flex items-start gap-3">
                    <div className="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
                      <Building2 className="h-4 w-4" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="font-medium text-foreground text-sm">{u.nombre}</p>
                      {u.planNombre ? (
                        <div className="mt-1 inline-flex items-center gap-1.5 rounded-full bg-primary/10 px-2.5 py-0.5 text-xs font-medium text-primary">
                          <CreditCard className="h-3 w-3" />
                          {u.planNombre}
                        </div>
                      ) : (
                        <p className="mt-1 text-xs text-muted-foreground">Sin plan asignado</p>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Acciones RGPD / RNF-09b */}
      <div className="rounded-xl border border-border bg-card p-6 space-y-4">
        <div>
          <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
            Gestión de datos personales
          </h2>
          <p className="mt-1 text-xs text-muted-foreground">
            De acuerdo a la Ley 18.331, tenés derecho a solicitar la modificación o eliminación de tus datos.
          </p>
        </div>
        <div className="flex flex-wrap gap-3">
          <Button
            variant="outline"
            className="gap-2 cursor-pointer"
            onClick={() => { setSuccessMsg(null); setErrorMsg(null); setShowModificacion(true) }}
          >
            <Pencil className="h-4 w-4" />
            Solicitar modificación de datos
          </Button>
          <Button
            variant="outline"
            className="gap-2 cursor-pointer border-destructive/50 text-destructive hover:bg-destructive/10 hover:text-destructive"
            onClick={() => { setSuccessMsg(null); setErrorMsg(null); setShowBaja(true) }}
          >
            <AlertTriangle className="h-4 w-4" />
            Solicitar baja de cuenta
          </Button>
        </div>
      </div>

      {/* Dialog: Solicitar Modificación */}
      <Dialog open={showModificacion} onOpenChange={setShowModificacion}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Solicitar modificación de datos</DialogTitle>
            <DialogDescription>
              Describí qué datos querés modificar. El equipo se contactará contigo para procesarlo.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <Label htmlFor="detalle" className="text-muted-foreground">
              ¿Qué información querés cambiar? *
            </Label>
            <Textarea
              id="detalle"
              value={detalle}
              onChange={(e) => setDetalle(e.target.value)}
              placeholder="Ej: Quiero actualizar mi número de teléfono a 099 999 999"
              rows={4}
              className="bg-muted/30 border-border resize-none"
            />
            {modificacionMutation.isError && (
              <p className="text-xs text-destructive">No se pudo registrar la solicitud. Intentá de nuevo.</p>
            )}
          </div>
          <DialogFooter className="gap-2">
            <Button variant="outline" onClick={() => setShowModificacion(false)} className="cursor-pointer">
              Cancelar
            </Button>
            <Button
              onClick={() => modificacionMutation.mutate()}
              disabled={!detalle.trim() || modificacionMutation.isPending}
              className="cursor-pointer"
            >
              {modificacionMutation.isPending ? 'Enviando...' : 'Enviar solicitud'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Dialog: Solicitar Baja */}
      <Dialog open={showBaja} onOpenChange={setShowBaja}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-destructive" />
              Solicitar baja de cuenta
            </DialogTitle>
            <DialogDescription>
              Al confirmar, registraremos tu solicitud de baja. El equipo la procesará en los próximos días hábiles.
              Esta acción no es inmediata.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <Label htmlFor="motivoBaja" className="text-muted-foreground">
              Motivo (opcional)
            </Label>
            <Textarea
              id="motivoBaja"
              value={motivoBaja}
              onChange={(e) => setMotivoBaja(e.target.value)}
              placeholder="Ej: Me mudo de ciudad"
              rows={3}
              className="bg-muted/30 border-border resize-none"
            />
          </div>
          <DialogFooter className="gap-2">
            <Button variant="outline" onClick={() => setShowBaja(false)} className="cursor-pointer">
              Cancelar
            </Button>
            <Button
              variant="destructive"
              onClick={() => bajaMutation.mutate()}
              disabled={bajaMutation.isPending}
              className="cursor-pointer"
            >
              {bajaMutation.isPending ? 'Enviando...' : 'Confirmar solicitud de baja'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
