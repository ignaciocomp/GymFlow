import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { eventosApi, unidadesApi } from '@/services/api'
import { formatDateTime } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import {
  Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle,
} from '@/components/ui/dialog'
import { CalendarDays, Plus, Pencil, Trash2, Mail, CheckCircle2 } from 'lucide-react'
import type { Evento } from '@/types'

// Convierte un ISO (UTC desde el back) al formato que espera <input type="datetime-local"> (hora local).
function isoToLocalInput(iso: string): string {
  const d = new Date(iso)
  const pad = (n: number) => n.toString().padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

export default function EventosPage() {
  const queryClient = useQueryClient()

  const [unidadFilter, setUnidadFilter] = useState<string>('')
  const [formDialog, setFormDialog] = useState<{ mode: 'crear' | 'editar'; evento?: Evento } | null>(null)
  const [cancelDialog, setCancelDialog] = useState<Evento | null>(null)
  const [notificarDialog, setNotificarDialog] = useState<Evento | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)

  // Form state
  const [formTitulo, setFormTitulo] = useState('')
  const [formDescripcion, setFormDescripcion] = useState('')
  const [formFecha, setFormFecha] = useState('')

  const { data: unidades } = useQuery({
    queryKey: ['unidades'],
    queryFn: unidadesApi.getAll,
  })

  const { data: eventos, isLoading } = useQuery({
    queryKey: ['eventos', unidadFilter],
    queryFn: () => eventosApi.getAll(unidadFilter, true),
    enabled: !!unidadFilter,
  })

  // Cantidad de socios activos que recibirán el correo, para mostrarlo en el diálogo de confirmación (#51).
  const { data: destinatarios, isLoading: isLoadingDestinatarios } = useQuery({
    queryKey: ['evento-destinatarios', notificarDialog?.id],
    queryFn: () => eventosApi.getDestinatarios(notificarDialog!.id),
    enabled: !!notificarDialog,
  })

  const onMutationError = (fallback: string) => (err: unknown) => {
    if (err && typeof err === 'object' && 'response' in err) {
      const axiosErr = err as { response?: { data?: { error?: string } } }
      setFormError(axiosErr.response?.data?.error || fallback)
    } else {
      setFormError(fallback)
    }
  }

  const createMutation = useMutation({
    mutationFn: eventosApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['eventos'] })
      setFormDialog(null)
      setSuccessMsg('Evento creado. Se notificó por email a los socios de la sede.')
    },
    onError: onMutationError('Error al crear el evento.'),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, titulo, descripcion, fecha }: { id: string; titulo: string; descripcion: string | null; fecha: string }) =>
      eventosApi.update(id, { titulo, descripcion, fecha }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['eventos'] })
      setFormDialog(null)
      setSuccessMsg('Evento actualizado correctamente.')
    },
    onError: onMutationError('Error al actualizar el evento.'),
  })

  const cancelMutation = useMutation({
    mutationFn: (id: string) => eventosApi.cancel(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['eventos'] })
      setCancelDialog(null)
      setSuccessMsg('Evento cancelado.')
    },
    onError: onMutationError('Error al cancelar el evento.'),
  })

  const notificarMutation = useMutation({
    mutationFn: (id: string) => eventosApi.notificar(id),
    onSuccess: (res) => {
      setNotificarDialog(null)
      setSuccessMsg(res.mensaje || 'Se reenviaron las notificaciones a los socios de la sede.')
    },
    onError: onMutationError('Error al reenviar las notificaciones.'),
  })

  const openCrear = () => {
    setFormError(null)
    setFormTitulo('')
    setFormDescripcion('')
    setFormFecha('')
    setFormDialog({ mode: 'crear' })
  }

  const openEditar = (evento: Evento) => {
    setFormError(null)
    setFormTitulo(evento.titulo)
    setFormDescripcion(evento.descripcion || '')
    setFormFecha(isoToLocalInput(evento.fecha))
    setFormDialog({ mode: 'editar', evento })
  }

  const handleSubmit = () => {
    setFormError(null)
    if (!formTitulo.trim()) { setFormError('El título es obligatorio.'); return }
    if (!formFecha) { setFormError('La fecha y hora son obligatorias.'); return }
    const fechaIso = new Date(formFecha).toISOString()
    const descripcion = formDescripcion.trim() || null

    if (formDialog?.mode === 'editar' && formDialog.evento) {
      updateMutation.mutate({ id: formDialog.evento.id, titulo: formTitulo.trim(), descripcion, fecha: fechaIso })
    } else {
      createMutation.mutate({ titulo: formTitulo.trim(), descripcion, fecha: fechaIso, unidadId: unidadFilter })
    }
  }

  const isSaving = createMutation.isPending || updateMutation.isPending

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <CalendarDays className="h-5 w-5" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">Eventos</h1>
            <p className="text-sm text-muted-foreground">
              Gestiona los eventos especiales de cada sede
            </p>
          </div>
        </div>
        <Button className="cursor-pointer gap-2" onClick={openCrear} disabled={!unidadFilter}>
          <Plus className="h-4 w-4" />
          Nuevo evento
        </Button>
      </div>

      {/* Success banner */}
      {successMsg && (
        <div className="flex items-start gap-2 rounded-lg border border-primary/40 bg-primary/10 p-3">
          <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-primary" />
          <p className="flex-1 text-sm text-foreground">{successMsg}</p>
          <button
            onClick={() => setSuccessMsg(null)}
            className="text-xs text-muted-foreground hover:text-foreground cursor-pointer"
          >
            Cerrar
          </button>
        </div>
      )}

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <Select value={unidadFilter} onValueChange={(val) => setUnidadFilter(val ?? '')}>
          <SelectTrigger className="w-[220px] bg-card border-border">
            <SelectValue placeholder="Seleccionar sede">
              {unidadFilter
                ? unidades?.find(u => u.id === unidadFilter)?.nombre || 'Sede'
                : undefined}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            {unidades?.map((u) => (
              <SelectItem key={u.id} value={u.id}>{u.nombre}</SelectItem>
            ))}
          </SelectContent>
        </Select>
        {unidadFilter && eventos && (
          <span className="text-sm text-muted-foreground">
            {eventos.length} evento{eventos.length !== 1 ? 's' : ''}
          </span>
        )}
      </div>

      {/* Empty: sin sede seleccionada */}
      {!unidadFilter ? (
        <div className="flex h-64 flex-col items-center justify-center gap-5 rounded-xl border border-border bg-card text-center">
          <p className="text-lg font-medium text-foreground">
            Seleccioná una sede para ver sus eventos
          </p>
          <div className="flex flex-wrap items-center justify-center gap-3">
            {unidades?.map((u) => (
              <Button
                key={u.id}
                variant="outline"
                className="cursor-pointer"
                onClick={() => setUnidadFilter(u.id)}
              >
                {u.nombre}
              </Button>
            ))}
          </div>
        </div>
      ) : (
        <>
          {/* Tabla (sm+) */}
          <div className="hidden sm:block rounded-xl border border-border bg-card overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow className="border-border hover:bg-transparent">
                  <TableHead className="text-muted-foreground">Título</TableHead>
                  <TableHead className="text-muted-foreground">Fecha</TableHead>
                  <TableHead className="text-muted-foreground">Estado</TableHead>
                  <TableHead className="text-muted-foreground text-right">Acciones</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {isLoading && (
                  <TableRow>
                    <TableCell colSpan={4} className="h-32 text-center text-muted-foreground">
                      Cargando...
                    </TableCell>
                  </TableRow>
                )}
                {eventos && eventos.length === 0 && !isLoading && (
                  <TableRow>
                    <TableCell colSpan={4} className="h-32 text-center">
                      <div className="flex flex-col items-center gap-2">
                        <CalendarDays className="h-8 w-8 text-muted-foreground/50" />
                        <p className="text-muted-foreground">No hay eventos en esta sede.</p>
                        <Button variant="outline" size="sm" className="mt-2 cursor-pointer gap-2" onClick={openCrear}>
                          <Plus className="h-4 w-4" />
                          Crear evento
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                )}
                {eventos?.map((evento) => (
                  <TableRow key={evento.id} className="border-border hover:bg-muted/30">
                    <TableCell className="font-medium text-foreground">
                      <div className="flex flex-col">
                        <span>{evento.titulo}</span>
                        {evento.descripcion && (
                          <span className="text-xs text-muted-foreground line-clamp-1">{evento.descripcion}</span>
                        )}
                      </div>
                    </TableCell>
                    <TableCell className="text-muted-foreground tabular-nums">{formatDateTime(evento.fecha)}</TableCell>
                    <TableCell>
                      {evento.estaActivo ? (
                        <Badge variant="secondary" className="bg-primary/10 text-primary border-0">
                          Activo
                        </Badge>
                      ) : (
                        <Badge variant="secondary" className="bg-muted text-muted-foreground border-0">
                          Cancelado
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-1">
                        {evento.estaActivo && (
                          <>
                            <button
                              className="flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-foreground transition-colors cursor-pointer"
                              title="Reenviar notificación"
                              onClick={() => { setFormError(null); setNotificarDialog(evento) }}
                            >
                              <Mail className="h-4 w-4" />
                            </button>
                            <button
                              className="flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-foreground transition-colors cursor-pointer"
                              title="Editar"
                              onClick={() => openEditar(evento)}
                            >
                              <Pencil className="h-4 w-4" />
                            </button>
                            <button
                              className="flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground hover:bg-destructive/10 hover:text-destructive transition-colors cursor-pointer"
                              title="Cancelar evento"
                              onClick={() => { setFormError(null); setCancelDialog(evento) }}
                            >
                              <Trash2 className="h-4 w-4" />
                            </button>
                          </>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          {/* Cards (mobile) */}
          <div className="sm:hidden space-y-3">
            {isLoading && (
              <p className="text-center text-muted-foreground py-8">Cargando...</p>
            )}
            {eventos && eventos.length === 0 && !isLoading && (
              <div className="flex flex-col items-center gap-2 rounded-xl border border-border bg-card py-10">
                <CalendarDays className="h-8 w-8 text-muted-foreground/50" />
                <p className="text-muted-foreground">No hay eventos en esta sede.</p>
                <Button variant="outline" size="sm" className="mt-2 cursor-pointer gap-2" onClick={openCrear}>
                  <Plus className="h-4 w-4" />
                  Crear evento
                </Button>
              </div>
            )}
            {eventos?.map((evento) => (
              <div key={evento.id} className="rounded-xl border border-border bg-card p-4 space-y-3">
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="font-medium text-foreground">{evento.titulo}</p>
                    <p className="text-sm text-muted-foreground tabular-nums">{formatDateTime(evento.fecha)}</p>
                  </div>
                  {evento.estaActivo ? (
                    <Badge variant="secondary" className="bg-primary/10 text-primary border-0 shrink-0">Activo</Badge>
                  ) : (
                    <Badge variant="secondary" className="bg-muted text-muted-foreground border-0 shrink-0">Cancelado</Badge>
                  )}
                </div>
                {evento.descripcion && (
                  <p className="text-sm text-muted-foreground">{evento.descripcion}</p>
                )}
                {evento.estaActivo && (
                  <div className="flex gap-2">
                    <Button size="sm" variant="outline" className="cursor-pointer gap-1.5"
                      onClick={() => { setFormError(null); setNotificarDialog(evento) }}>
                      <Mail className="h-3.5 w-3.5" />
                      Notificar
                    </Button>
                    <Button size="sm" variant="outline" className="cursor-pointer gap-1.5" onClick={() => openEditar(evento)}>
                      <Pencil className="h-3.5 w-3.5" />
                      Editar
                    </Button>
                    <Button size="sm" variant="outline" className="cursor-pointer gap-1.5 text-destructive hover:text-destructive"
                      onClick={() => { setFormError(null); setCancelDialog(evento) }}>
                      <Trash2 className="h-3.5 w-3.5" />
                      Cancelar
                    </Button>
                  </div>
                )}
              </div>
            ))}
          </div>
        </>
      )}

      {/* Crear / Editar Dialog */}
      <Dialog open={!!formDialog} onOpenChange={(open) => { if (!open) { setFormDialog(null); setFormError(null) } }}>
        <DialogContent className="bg-card border-border sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="text-foreground">
              {formDialog?.mode === 'editar' ? 'Editar evento' : 'Nuevo evento'}
            </DialogTitle>
            <DialogDescription className="text-muted-foreground">
              {formDialog?.mode === 'editar'
                ? 'Modificá los datos del evento.'
                : 'Al crear el evento se notificará por email a los socios de la sede.'}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-2">
              <Label className="text-muted-foreground">Título *</Label>
              <Input
                value={formTitulo}
                onChange={(e) => setFormTitulo(e.target.value)}
                placeholder="Título del evento"
                className="bg-muted/30 border-border"
              />
            </div>

            <div className="space-y-2">
              <Label className="text-muted-foreground">Fecha y hora *</Label>
              <Input
                type="datetime-local"
                value={formFecha}
                onChange={(e) => setFormFecha(e.target.value)}
                className="bg-muted/30 border-border"
              />
            </div>

            <div className="space-y-2">
              <Label className="text-muted-foreground">Descripción</Label>
              <Textarea
                value={formDescripcion}
                onChange={(e) => setFormDescripcion(e.target.value)}
                placeholder="Descripción del evento (opcional)"
                className="bg-muted/30 border-border"
                rows={3}
              />
            </div>
          </div>

          {formError && (
            <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3">
              <p className="text-sm text-destructive">{formError}</p>
            </div>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => { setFormDialog(null); setFormError(null) }} className="cursor-pointer">
              Cancelar
            </Button>
            <Button onClick={handleSubmit} disabled={isSaving} className="cursor-pointer">
              {isSaving ? 'Guardando...' : (formDialog?.mode === 'editar' ? 'Guardar cambios' : 'Crear evento')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Cancelar Dialog */}
      <Dialog open={!!cancelDialog} onOpenChange={(open) => { if (!open) { setCancelDialog(null); setFormError(null) } }}>
        <DialogContent className="bg-card border-border">
          <DialogHeader>
            <DialogTitle className="text-foreground">Cancelar evento</DialogTitle>
            <DialogDescription className="text-muted-foreground">
              Estás por cancelar el evento <strong className="text-foreground">{cancelDialog?.titulo}</strong>.
              Dejará de mostrarse a los socios. Esta acción es una baja lógica.
            </DialogDescription>
          </DialogHeader>
          {formError && (
            <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3">
              <p className="text-sm text-destructive">{formError}</p>
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => { setCancelDialog(null); setFormError(null) }} className="cursor-pointer">
              Volver
            </Button>
            <Button
              variant="destructive"
              onClick={() => { if (cancelDialog) cancelMutation.mutate(cancelDialog.id) }}
              disabled={cancelMutation.isPending}
              className="cursor-pointer"
            >
              {cancelMutation.isPending ? 'Cancelando...' : 'Cancelar evento'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Notificar Dialog */}
      <Dialog open={!!notificarDialog} onOpenChange={(open) => { if (!open) { setNotificarDialog(null); setFormError(null) } }}>
        <DialogContent className="bg-card border-border">
          <DialogHeader>
            <DialogTitle className="text-foreground">Reenviar notificación</DialogTitle>
            <DialogDescription className="text-muted-foreground">
              {isLoadingDestinatarios ? (
                <>
                  ¿Reenviar la notificación del evento{' '}
                  <strong className="text-foreground">{notificarDialog?.titulo}</strong> por email a los socios de la sede?
                </>
              ) : destinatarios && destinatarios.cantidad > 0 ? (
                <>
                  Se enviará la notificación del evento{' '}
                  <strong className="text-foreground">{notificarDialog?.titulo}</strong> por email a{' '}
                  <strong className="text-foreground">
                    {destinatarios.cantidad} socio{destinatarios.cantidad !== 1 ? 's' : ''}
                  </strong>{' '}
                  de la sede <strong className="text-foreground">{destinatarios.sede}</strong>.
                </>
              ) : (
                <>
                  No hay socios activos en la sede{' '}
                  <strong className="text-foreground">{destinatarios?.sede || notificarDialog?.unidadNombre}</strong>{' '}
                  para notificar del evento{' '}
                  <strong className="text-foreground">{notificarDialog?.titulo}</strong>.
                </>
              )}
            </DialogDescription>
          </DialogHeader>
          {formError && (
            <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-3">
              <p className="text-sm text-destructive">{formError}</p>
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => { setNotificarDialog(null); setFormError(null) }} className="cursor-pointer">
              Volver
            </Button>
            <Button
              onClick={() => { if (notificarDialog) notificarMutation.mutate(notificarDialog.id) }}
              disabled={notificarMutation.isPending || isLoadingDestinatarios || destinatarios?.cantidad === 0}
              className="cursor-pointer gap-2"
            >
              <Mail className="h-4 w-4" />
              {notificarMutation.isPending ? 'Enviando...' : 'Reenviar'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
