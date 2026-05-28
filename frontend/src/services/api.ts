import axios from 'axios'
import type { Unidad, Socio, CreateSocioRequest, UpdateSocioRequest, DeleteSocioRequest, Plan, AuditoriaEntry, CreatePlanRequest, UpdatePlanRequest, SolicitarModificacionRequest, SolicitarBajaRequest, CuotaDto, SocioConEstadoCuotaDto, Clase, CreateClaseRequest, UpdateClaseRequest, HorarioClase, CreateHorarioClaseRequest, UpdateHorarioClaseRequest, InscripcionClase } from '@/types'

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

export const cuotasApi = {
  getMisCuotas: async (): Promise<CuotaDto[]> => {
    const { data } = await api.get<CuotaDto[]>('/cuotas/mis-cuotas')
    return data
  },

  getAdmin: async (params: {
    documentoIdentidad: string
    estado?: string
    mes?: number
    anio?: number
    unidadId?: string
    incluirAnuladas?: boolean
  }): Promise<CuotaDto[]> => {
    const { data } = await api.get<CuotaDto[]>('/cuotas/admin', { params })
    return data
  },

  marcarPagada: async (id: string): Promise<void> => {
    await api.put(`/cuotas/${id}/pagar`)
  },

  anular: async (id: string): Promise<void> => {
    await api.delete(`/cuotas/${id}`)
  },

  revertirPago: async (id: string): Promise<void> => {
    await api.put(`/cuotas/${id}/revertir-pago`)
  },

  revertirAnulacion: async (id: string): Promise<void> => {
    await api.put(`/cuotas/${id}/revertir-anulacion`)
  },

  notificar: async (id: string): Promise<void> => {
    await api.post(`/cuotas/${id}/notificar`)
  },

  getSociosEstado: async (unidadId?: string): Promise<SocioConEstadoCuotaDto[]> => {
    const params: Record<string, string> = {}
    if (unidadId) params.unidadId = unidadId
    const { data } = await api.get<SocioConEstadoCuotaDto[]>('/cuotas/socios-estado', { params })
    return data
  },

  getBySocioId: async (socioId: string, params?: {
    estado?: string
    mes?: number
    anio?: number
    unidadId?: string
    incluirAnuladas?: boolean
  }): Promise<CuotaDto[]> => {
    const { data } = await api.get<CuotaDto[]>(`/cuotas/admin/socio/${socioId}`, { params })
    return data
  },
}

export const clasesApi = {
  getAll: async (unidadId?: string, includeInactive?: boolean): Promise<Clase[]> => {
    const params: Record<string, string> = {}
    if (unidadId) params.unidadId = unidadId
    if (includeInactive) params.includeInactive = 'true'
    const { data } = await api.get<Clase[]>('/clases', { params })
    return data
  },

  getById: async (id: string): Promise<Clase> => {
    const { data } = await api.get<Clase>(`/clases/${id}`)
    return data
  },

  create: async (request: CreateClaseRequest): Promise<Clase> => {
    const { data } = await api.post<Clase>('/clases', request)
    return data
  },

  update: async (id: string, request: UpdateClaseRequest): Promise<Clase> => {
    const { data } = await api.put<Clase>(`/clases/${id}`, request)
    return data
  },

  cancel: async (id: string): Promise<void> => {
    await api.delete(`/clases/${id}`)
  },

  reactivate: async (id: string): Promise<Clase> => {
    const { data } = await api.patch<Clase>(`/clases/${id}/reactivar`)
    return data
  },
}

export const horariosApi = {
  getAll: async (unidadId?: string): Promise<HorarioClase[]> => {
    const params: Record<string, string> = {}
    if (unidadId) params.unidadId = unidadId
    const { data } = await api.get<HorarioClase[]>('/horarios', { params })
    return data
  },

  getById: async (id: string): Promise<HorarioClase> => {
    const { data } = await api.get<HorarioClase>(`/horarios/${id}`)
    return data
  },

  create: async (request: CreateHorarioClaseRequest): Promise<HorarioClase> => {
    const { data } = await api.post<HorarioClase>('/horarios', request)
    return data
  },

  update: async (id: string, request: UpdateHorarioClaseRequest): Promise<HorarioClase> => {
    const { data } = await api.put<HorarioClase>(`/horarios/${id}`, request)
    return data
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/horarios/${id}`)
  },
}

export const inscripcionesApi = {
  inscribirse: async (claseId: string): Promise<InscripcionClase> => {
    const { data } = await api.post<InscripcionClase>('/inscripciones', { claseId })
    return data
  },

  getMisInscripciones: async (): Promise<InscripcionClase[]> => {
    const { data } = await api.get<InscripcionClase[]>('/inscripciones/mis-inscripciones')
    return data
  },

  cancelar: async (id: string): Promise<void> => {
    await api.delete(`/inscripciones/${id}`)
  },
}

export default api
