export type Modulo = 'Socios' | 'Planes' | 'Unidades' | 'Auditoria' | 'Empleados' | 'Cuotas' | 'Clases'
export type Operacion = 'Lectura' | 'Escritura' | 'Modificacion' | 'Eliminacion'

export interface Permiso {
  id: string
  modulo: Modulo
  operacion: Operacion
}

export interface Rol {
  id: string
  nombre: string
  esSistema: boolean
  fechaCreacion: string
  permisos: Permiso[]
}

export interface CrearRolRequest {
  nombre: string
  permisoIds: string[]
}

export interface ActualizarRolRequest {
  nombre: string
  permisoIds: string[]
}
