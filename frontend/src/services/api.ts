import axios from 'axios'
import type { Unidad, Socio, CreateSocioRequest, UpdateSocioRequest, DeleteSocioRequest, Plan, AuditoriaEntry, CreatePlanRequest, UpdatePlanRequest, SolicitarModificacionRequest, SolicitarBajaRequest, CuotaDto, SocioConEstadoCuotaDto, Clase, CreateClaseRequest, UpdateClaseRequest, HorarioClase, CreateHorarioClaseRequest, UpdateHorarioClaseRequest, InscripcionClase, Evento, CreateEventoRequest, UpdateEventoRequest, Notificacion, PagoDto, DashboardDto } from '@/types'
import type { Permiso } from '@/types/permisos'

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

export interface LoginResponse {
  token: string
  nombre: string
  apellido: string
  correo: string
  rolNombre: string
  permisos: Permiso[]
  unidadIds: string[]
}

/**
 * Resultado del paso 1 del login. Para empleados: `requiereMfa=true` con un `mfaToken`
 * intermedio (y `setupRequerido` según tenga o no el segundo factor ya activado). Para
 * socios/legacy: `requiereMfa=false` y la `sesion` con el JWT ya emitido.
 */
export interface LoginResultado {
  requiereMfa: boolean
  setupRequerido: boolean
  mfaToken: string | null
  sesion: LoginResponse | null
}

/** Datos del alta de MFA: URI otpauth, QR como data URI PNG y la clave manual (base32). */
export interface MfaSetupResponse {
  uriOtpauth: string
  qrDataUri: string
  claveManual: string
}

/** Respuesta de la activación: la sesión emitida y los códigos de recuperación (una sola vez). */
export interface MfaActivarResponse {
  sesion: LoginResponse
  codigosRecuperacion: string[]
}

const bearer = (mfaToken: string) => ({ headers: { Authorization: `Bearer ${mfaToken}` } })

export const authApi = {
  login: async (correo: string, password: string): Promise<LoginResultado> => {
    const { data } = await api.post<LoginResultado>('/auth/login', { correo, password })
    return data
  },

  loginConGoogle: async (idToken: string): Promise<LoginResponse> => {
    const { data } = await api.post<LoginResponse>('/auth/google', { idToken })
    return data
  },

  // Endpoints del segundo factor. El mfaToken intermedio viaja por Authorization: Bearer.
  mfaSetup: async (mfaToken: string): Promise<MfaSetupResponse> => {
    const { data } = await api.post<MfaSetupResponse>('/auth/mfa/setup', undefined, bearer(mfaToken))
    return data
  },

  mfaActivate: async (mfaToken: string, codigo: string): Promise<MfaActivarResponse> => {
    const { data } = await api.post<MfaActivarResponse>('/auth/mfa/activate', { codigo }, bearer(mfaToken))
    return data
  },

  mfaVerify: async (mfaToken: string, codigo: string): Promise<LoginResponse> => {
    const { data } = await api.post<LoginResponse>('/auth/mfa/verify', { codigo }, bearer(mfaToken))
    return data
  },

  mfaRecovery: async (mfaToken: string, codigo: string): Promise<LoginResponse> => {
    const { data } = await api.post<LoginResponse>('/auth/mfa/recovery', { codigo }, bearer(mfaToken))
    return data
  },
}

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

  getEventos: async (): Promise<Evento[]> => {
    const { data } = await api.get<Evento[]>('/portal/eventos')
    return data
  },

  getNotificaciones: async (params?: { soloNoLeidas?: boolean; take?: number }): Promise<Notificacion[]> => {
    const { data } = await api.get<Notificacion[]>('/portal/notificaciones', { params })
    return data
  },

  contarNoLeidas: async (): Promise<number> => {
    const { data } = await api.get<{ count: number }>('/portal/notificaciones/no-leidas/count')
    return data.count
  },

  marcarLeida: async (id: string): Promise<void> => {
    await api.post(`/portal/notificaciones/${id}/leer`)
  },

  marcarTodasLeidas: async (): Promise<void> => {
    await api.post('/portal/notificaciones/leer-todas')
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

export const pagosApi = {
  // RF-21: inicia el pago online de una cuota pendiente y devuelve el init_point de Checkout Pro.
  iniciar: async (cuotaId: string): Promise<{ initPoint: string }> => {
    const { data } = await api.post<{ initPoint: string }>('/pagos/iniciar', { cuotaId })
    return data
  },

  // RF-21 / CU-08: historial de pagos del socio autenticado.
  getMisPagos: async (): Promise<PagoDto[]> => {
    const { data } = await api.get<PagoDto[]>('/pagos/mis-pagos')
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

export const eventosApi = {
  getAll: async (unidadId?: string, incluirInactivos?: boolean): Promise<Evento[]> => {
    const params: Record<string, string> = {}
    if (unidadId) params.unidadId = unidadId
    if (incluirInactivos) params.incluirInactivos = 'true'
    const { data } = await api.get<Evento[]>('/eventos', { params })
    return data
  },

  getById: async (id: string): Promise<Evento> => {
    const { data } = await api.get<Evento>(`/eventos/${id}`)
    return data
  },

  create: async (request: CreateEventoRequest): Promise<Evento> => {
    const { data } = await api.post<Evento>('/eventos', request)
    return data
  },

  update: async (id: string, request: UpdateEventoRequest): Promise<Evento> => {
    const { data } = await api.put<Evento>(`/eventos/${id}`, request)
    return data
  },

  cancel: async (id: string): Promise<void> => {
    await api.delete(`/eventos/${id}`)
  },

  notificar: async (id: string): Promise<{ mensaje: string }> => {
    const { data } = await api.post<{ mensaje: string }>(`/eventos/${id}/notificar`)
    return data
  },

  getDestinatarios: async (id: string): Promise<{ cantidad: number; sede: string }> => {
    const { data } = await api.get<{ cantidad: number; sede: string }>(`/eventos/${id}/destinatarios`)
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

export const dashboardApi = {
  // RF-18: snapshot del dashboard (carga inicial y polling de fallback).
  get: async (unidadId?: string): Promise<DashboardDto> => {
    const params: Record<string, string> = {}
    if (unidadId) params.unidadId = unidadId
    const { data } = await api.get<DashboardDto>('/dashboard', { params })
    return data
  },
}

export const inscripcionesApi = {
  inscribirse: async (horarioClaseId: string): Promise<InscripcionClase> => {
    const { data } = await api.post<InscripcionClase>('/inscripciones', { horarioClaseId })
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
