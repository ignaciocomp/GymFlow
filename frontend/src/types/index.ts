export interface Unidad {
  id: string
  nombre: string
  direccion: string
}

export interface Plan {
  id: string
  nombre: string
  precio: number
  descripcion: string
  unidadId: string
}

export interface Socio {
  id: string
  nombre: string
  apellido: string
  correo: string
  telefono: string | null
  documentoIdentidad: string | null
  fechaNacimiento: string | null
  fechaAlta: string
  estaActivo: boolean
  planId: string | null
  planNombre: string | null
  unidades: Unidad[]
}

export interface CreateSocioRequest {
  nombre: string
  apellido: string
  correo: string
  telefono: string | null
  documentoIdentidad: string | null
  fechaNacimiento: string | null
  planId: string | null
  unidadIds: string[]
  consentimientoInformado: boolean
}

export interface UpdateSocioRequest {
  nombre: string
  apellido: string
  correo: string
  telefono: string | null
  documentoIdentidad: string | null
  fechaNacimiento: string | null
  planId: string | null
  unidadIds: string[]
}

export interface DeleteSocioRequest {
  motivo: string | null
}
