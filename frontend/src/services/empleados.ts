import api from './api'
import type {
  Empleado,
  CrearEmpleadoRequest,
  ActualizarEmpleadoRequest,
  CambiarPasswordRequest,
} from '@/types/empleado'

export async function listarEmpleados(activo?: boolean): Promise<Empleado[]> {
  const { data } = await api.get<Empleado[]>('/empleados', { params: { activo } })
  return data
}

export async function obtenerEmpleado(id: string): Promise<Empleado> {
  const { data } = await api.get<Empleado>(`/empleados/${id}`)
  return data
}

export async function crearEmpleado(req: CrearEmpleadoRequest): Promise<Empleado> {
  const { data } = await api.post<Empleado>('/empleados', req)
  return data
}

export async function actualizarEmpleado(id: string, req: ActualizarEmpleadoRequest): Promise<void> {
  await api.put(`/empleados/${id}`, req)
}

export async function cambiarPasswordEmpleado(
  id: string,
  req: CambiarPasswordRequest,
): Promise<void> {
  await api.patch(`/empleados/${id}/password`, req)
}

export async function darDeBajaEmpleado(id: string): Promise<void> {
  await api.delete(`/empleados/${id}`)
}

export async function reactivarEmpleado(id: string, rolId?: string): Promise<void> {
  await api.patch(`/empleados/${id}/reactivar`, { rolId: rolId ?? null })
}

export async function resetearMfaEmpleado(id: string): Promise<void> {
  await api.post(`/empleados/${id}/mfa/reset`)
}
