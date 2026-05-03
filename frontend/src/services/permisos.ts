import api from './api'
import type { Permiso } from '@/types/permisos'

export async function listarPermisos(): Promise<Permiso[]> {
  const { data } = await api.get<Permiso[]>('/permisos')
  return data
}
