export interface Unidad {
  id: string
  nombre: string
  direccion: string
}

export interface UnidadConPlan {
  id: string
  nombre: string
  direccion: string
  planId: string | null
  planNombre: string | null
}

export interface Plan {
  id: string
  nombre: string
  precio: number
  descripcion: string
  unidadId: string
  unidadNombre: string
  estaActivo: boolean
}

export interface CreatePlanRequest {
  nombre: string
  unidadId: string
  precio: number
  descripcion: string | null
}

export interface UpdatePlanRequest {
  nombre: string
  precio: number
  descripcion: string | null
}

export type TipoDocumento = 'CI' | 'Pasaporte' | 'Otro'

export interface Socio {
  id: string
  nombre: string
  apellido: string
  correo: string
  telefono: string | null
  tipoDocumento: TipoDocumento | null
  documentoIdentidad: string | null
  fechaNacimiento: string | null
  fechaAlta: string
  estaActivo: boolean
  unidades: UnidadConPlan[]
}

export interface CreateSocioRequest {
  nombre: string
  apellido: string
  correo: string
  telefono: string | null
  tipoDocumento: TipoDocumento | null
  documentoIdentidad: string | null
  fechaNacimiento: string | null
  unidades: { unidadId: string; planId: string | null }[]
  consentimientoInformado: boolean
  fechaAlta?: string | null
}

export interface UpdateSocioRequest {
  nombre: string
  apellido: string
  correo: string
  telefono: string | null
  tipoDocumento: TipoDocumento | null
  documentoIdentidad: string | null
  fechaNacimiento: string | null
  unidades: { unidadId: string; planId: string | null }[]
  fechaAlta?: string | null
}

export interface DeleteSocioRequest {
  motivo: string | null
}

export type TipoAccionAuditoria = 'Creacion' | 'Modificacion' | 'Baja' | 'Reactivacion' | 'InicioSesion' | 'SolicitudModificacion' | 'SolicitudBaja'

export interface SolicitarModificacionRequest {
  detalle: string
}

export interface SolicitarBajaRequest {
  motivo: string | null
}

export interface AuditoriaEntry {
  id: string
  usuarioId: string
  usuarioNombre: string
  tipoAccion: TipoAccionAuditoria
  entidadAfectada: string
  entidadId: string | null
  descripcion: string
  detallesCambios: string | null
  fechaHora: string
}

export type EstadoCuota = 'Pendiente' | 'Pagada' | 'Anulada'

export interface CuotaDto {
  id: string
  nombrePlan: string
  nombreUnidad: string
  nombreSocio: string | null
  monto: number
  fechaEmision: string
  fechaVencimiento: string
  estado: EstadoCuota
  fechaPago: string | null
  fechaBaja: string | null
}

export type EstadoPago = 'Pendiente' | 'Aprobado' | 'Rechazado'

export interface PagoDto {
  id: string
  fecha: string
  monto: number
  medioPago: string | null
  mpPaymentId: string | null
  estado: EstadoPago
  nombrePlan: string
}

export interface Clase {
  id: string
  nombre: string
  descripcion: string
  capacidadMaxima: number
  duracionMinutos: number
  instructor: string
  unidadId: string
  unidadNombre: string
  estaActivo: boolean
}

export interface CreateClaseRequest {
  nombre: string
  descripcion: string | null
  capacidadMaxima: number
  duracionMinutos: number
  instructor: string
  unidadId: string
}

export interface UpdateClaseRequest {
  nombre: string
  descripcion: string | null
  capacidadMaxima: number
  duracionMinutos: number
  instructor: string
}

export type DiaSemana = 'Lunes' | 'Martes' | 'Miercoles' | 'Jueves' | 'Viernes' | 'Sabado' | 'Domingo'

export interface HorarioClase {
  id: string
  claseId: string
  claseNombre: string
  instructor: string
  unidadId: string
  unidadNombre: string
  diaSemana: DiaSemana
  horaInicio: string
  horaFin: string
  sala: string | null
  capacidadMaxima: number
  inscripcionesActivas: number
}

export interface CreateHorarioClaseRequest {
  claseId: string
  diaSemana: DiaSemana
  horaInicio: string
  horaFin: string
  sala: string | null
}

export interface UpdateHorarioClaseRequest {
  diaSemana: DiaSemana
  horaInicio: string
  horaFin: string
  sala: string | null
}

export interface InscripcionClase {
  id: string
  horarioClaseId: string
  claseId: string
  claseNombre: string
  instructor: string
  unidadId: string
  unidadNombre: string
  diaSemana: DiaSemana
  horaInicio: string
  horaFin: string
  sala: string | null
  capacidadMaxima: number
  inscripcionesActivas: number
  fechaInscripcion: string
}

export interface Evento {
  id: string
  titulo: string
  descripcion: string
  fecha: string
  unidadId: string
  unidadNombre: string
  estaActivo: boolean
}

export interface CreateEventoRequest {
  titulo: string
  descripcion: string | null
  fecha: string
  unidadId: string
}

export interface UpdateEventoRequest {
  titulo: string
  descripcion: string | null
  fecha: string
}

export type TipoNotificacion =
  | 'RecordatorioCuota'
  | 'CambioHorario'
  | 'CancelacionClase'
  | 'ConfirmacionInscripcion'
  | 'EventoNuevo'

export interface Notificacion {
  id: string
  tipo: TipoNotificacion
  titulo: string
  mensaje: string
  leida: boolean
  fechaCreacion: string
}

export type EstadoGeneralCuotas = 'AlDia' | 'Pendiente' | 'Vencido'

export interface SocioConEstadoCuotaDto {
  socioId: string
  nombre: string
  apellido: string
  correo: string
  documentoIdentidad: string | null
  unidades: string[]
  estado: EstadoGeneralCuotas
  cuotasPendientes: number
  cuotasVencidas: number
}

// ── RF-18: Dashboard en tiempo real ──────────────────────────────────────────

/** Unidad visible para el filtro del dashboard. */
export interface DashboardUnidad {
  id: string
  nombre: string
}

export interface DashboardSociosPorUnidad {
  unidadId: string
  unidadNombre: string
  cantidad: number
}

export interface DashboardSociosActivos {
  total: number
  porUnidad: DashboardSociosPorUnidad[]
}

/** Counts de cuotas calculados en vivo (RN-17). */
export interface DashboardCuotas {
  proximasAVencer: number
  vencidas: number
  pagadasMes: number
}

/** Horario de clase del día actual; horas en formato HH:mm. */
export interface DashboardClaseDelDia {
  clase: string
  unidad: string
  horaInicio: string
  horaFin: string
  cupo: number
  inscriptos: number
}

export interface DashboardInscripcionReciente {
  socio: string
  clase: string
  unidad: string
  fecha: string
}

export interface DashboardSedeCantidad {
  sede: string
  cantidad: number
}

export interface DashboardEstadoCantidad {
  estado: string
  cantidad: number
}

/** Punto de la serie diaria; fecha en formato yyyy-MM-dd (UTC). */
export interface DashboardFechaCantidad {
  fecha: string
  cantidad: number
}

export interface DashboardGrafica {
  sociosPorSede: DashboardSedeCantidad[]
  cuotasPorEstado: DashboardEstadoCantidad[]
  inscripcionesUltimos7Dias: DashboardFechaCantidad[]
}

/** Snapshot del dashboard operativo (RF-18 / CU-10), servido por snapshot y stream SSE. */
export interface DashboardDto {
  generadoEn: string
  unidades: DashboardUnidad[]
  sociosActivos: DashboardSociosActivos
  cuotas: DashboardCuotas
  clasesDelDia: DashboardClaseDelDia[]
  inscripcionesRecientes: DashboardInscripcionReciente[]
  grafica: DashboardGrafica
}
