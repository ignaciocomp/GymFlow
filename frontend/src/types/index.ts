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

export type EstadoCuota = 'Pendiente' | 'Pagada'

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
