import api from './api'
import type { Rol, CrearRolRequest, ActualizarRolRequest } from '@/types/permisos'

export async function listarRoles(): Promise<Rol[]> {
  const { data } = await api.get<Rol[]>('/roles')
  return data
}

export async function obtenerRol(id: string): Promise<Rol> {
  const { data } = await api.get<Rol>(`/roles/${id}`)
  return data
}

export async function crearRol(req: CrearRolRequest): Promise<Rol> {
  const { data } = await api.post<Rol>('/roles', req)
  return data
}

export async function actualizarRol(id: string, req: ActualizarRolRequest): Promise<void> {
  await api.put(`/roles/${id}`, req)
}

export async function eliminarRol(id: string): Promise<void> {
  await api.delete(`/roles/${id}`)
}
