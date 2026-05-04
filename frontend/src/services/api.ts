import axios from 'axios'
import type { Unidad, Socio, CreateSocioRequest, UpdateSocioRequest, DeleteSocioRequest, Plan, AuditoriaEntry, CreatePlanRequest, UpdatePlanRequest, SolicitarModificacionRequest, SolicitarBajaRequest } from '@/types'

const api = axios.create({
  baseURL: '/api',
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && !error.config?.url?.includes('/auth/')) {
      localStorage.removeItem('gymflow_token')
      delete api.defaults.headers.common['Authorization']
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

export const unidadesApi = {
  getAll: async (): Promise<Unidad[]> => {
    const { data } = await api.get<Unidad[]>('/unidades')
    return data
  },
}

export const sociosApi = {
  getAll: async (params?: {
    nombre?: string
    unidadId?: string
    planId?: string
    estaActivo?: boolean
  }): Promise<Socio[]> => {
    const { data } = await api.get<Socio[]>('/socios', { params })
    return data
  },

  create: async (request: CreateSocioRequest): Promise<Socio> => {
    const { data } = await api.post<Socio>('/socios', request)
    return data
  },

  getById: async (id: string): Promise<Socio> => {
    const { data } = await api.get<Socio>(`/socios/${id}`)
    return data
  },

  update: async (id: string, request: UpdateSocioRequest): Promise<Socio> => {
    const { data } = await api.put<Socio>(`/socios/${id}`, request)
    return data
  },

  reactivate: async (id: string): Promise<Socio> => {
    const { data } = await api.patch<Socio>(`/socios/${id}/reactivar`)
    return data
  },

  delete: async (id: string, request?: DeleteSocioRequest): Promise<void> => {
    await api.delete(`/socios/${id}`, { data: request })
  },
}

export const planesApi = {
  getAll: async (unidadId?: string, includeInactive?: boolean): Promise<Plan[]> => {
    const params: Record<string, string> = {}
    if (unidadId) params.unidadId = unidadId
    if (includeInactive) params.includeInactive = 'true'
    const { data } = await api.get<Plan[]>('/planes', { params })
    return data
  },

  getById: async (id: string): Promise<Plan> => {
    const { data } = await api.get<Plan>(`/planes/${id}`)
    return data
  },

  create: async (request: CreatePlanRequest): Promise<Plan> => {
    const { data } = await api.post<Plan>('/planes', request)
    return data
  },

  update: async (id: string, request: UpdatePlanRequest): Promise<Plan> => {
    const { data } = await api.put<Plan>(`/planes/${id}`, request)
    return data
  },

  reactivate: async (id: string): Promise<Plan> => {
    const { data } = await api.patch<Plan>(`/planes/${id}/reactivar`)
    return data
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/planes/${id}`)
  },
}

export const portalApi = {
  getPerfil: async (): Promise<Socio> => {
    const { data } = await api.get<Socio>('/portal/perfil')
    return data
  },

  solicitarModificacion: async (request: SolicitarModificacionRequest): Promise<{ mensaje: string }> => {
    const { data } = await api.post<{ mensaje: string }>('/portal/solicitar-modificacion', request)
    return data
  },

  solicitarBaja: async (request?: SolicitarBajaRequest): Promise<{ mensaje: string }> => {
    const { data } = await api.post<{ mensaje: string }>('/portal/solicitar-baja', request ?? { motivo: null })
    return data
  },
}

export const auditoriaApi = {
  getAll: async (params?: {
    desde?: string
    hasta?: string
    tipoAccion?: string
    entidadId?: string
  }): Promise<AuditoriaEntry[]> => {
    const { data } = await api.get<AuditoriaEntry[]>('/auditoria', { params })
    return data
  },
}

export default api
