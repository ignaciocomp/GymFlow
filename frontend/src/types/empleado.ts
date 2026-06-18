export interface Empleado {
  id: string
  nombre: string
  apellido: string
  correo: string
  rolId: string | null
  rolNombre: string | null
  estaActivo: boolean
  fechaCreacion: string
}

export interface CrearEmpleadoRequest {
  nombre: string
  apellido: string
  correo: string
  rolId: string
  unidadIds?: string[]
}

export interface ActualizarEmpleadoRequest {
  nombre: string
  apellido: string
  correo: string
  rolId: string
  unidadIds?: string[]
}

export interface CambiarPasswordRequest {
  nuevaPassword: string
}
