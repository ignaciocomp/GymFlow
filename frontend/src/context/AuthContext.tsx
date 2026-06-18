import { createContext, useContext, useState, useEffect, useCallback } from 'react'
import type { ReactNode } from 'react'
import api, { authApi } from '@/services/api'
import type { LoginResponse, LoginResultado, MfaSetupResponse } from '@/services/api'
import type { Permiso, Modulo, Operacion } from '@/types/permisos'

interface User {
  nombre: string
  apellido: string
  correo: string
  rolNombre: string
  permisos: Permiso[]
  unidadIds: string[]
}

interface AuthContextType {
  user: User | null
  token: string | null
  login: (correo: string, password: string) => Promise<LoginResultado>
  loginConGoogle: (idToken: string) => Promise<void>
  // Flujo de segundo factor (empleados). Tras el password, el mfaToken queda en memoria.
  mfaSetup: () => Promise<MfaSetupResponse>
  mfaActivate: (codigo: string) => Promise<string[]>
  mfaVerify: (codigo: string) => Promise<void>
  mfaRecovery: (codigo: string) => Promise<void>
  cancelarMfa: () => void
  logout: () => void
  isAuthenticated: boolean
  isLoading: boolean
  tienePermiso: (modulo: Modulo, operacion: Operacion) => boolean
}

const AuthContext = createContext<AuthContextType | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('gymflow_token'))
  // mfaToken intermedio: solo vive en memoria mientras dura el desafío del segundo factor.
  const [mfaToken, setMfaToken] = useState<string | null>(null)

  const logout = useCallback(() => {
    localStorage.removeItem('gymflow_token')
    delete api.defaults.headers.common['Authorization']
    setToken(null)
    setUser(null)
    setMfaToken(null)
  }, [])

  useEffect(() => {
    if (token) {
      api.defaults.headers.common['Authorization'] = `Bearer ${token}`
      api.get('/auth/me')
        .then(({ data }) => setUser({ ...data, unidadIds: data.unidadIds ?? [] }))
        .catch(() => logout())
        .finally(() => setIsLoading(false))
    } else {
      setIsLoading(false)
    }
  }, [token, logout])

  const aplicarSesion = (data: LoginResponse) => {
    localStorage.setItem('gymflow_token', data.token)
    api.defaults.headers.common['Authorization'] = `Bearer ${data.token}`
    setToken(data.token)
    setMfaToken(null)
    setUser({
      nombre: data.nombre,
      apellido: data.apellido,
      correo: data.correo,
      rolNombre: data.rolNombre,
      permisos: data.permisos ?? [],
      unidadIds: data.unidadIds ?? [],
    })
  }

  const login = async (correo: string, password: string): Promise<LoginResultado> => {
    const resultado = await authApi.login(correo, password)
    if (resultado.requiereMfa) {
      // Empleado: guardamos el mfaToken y dejamos que la UI ramifique a setup/verify.
      setMfaToken(resultado.mfaToken)
    } else if (resultado.sesion) {
      aplicarSesion(resultado.sesion)
    }
    return resultado
  }

  const loginConGoogle = async (idToken: string) => {
    aplicarSesion(await authApi.loginConGoogle(idToken))
  }

  const exigirMfaToken = (): string => {
    if (!mfaToken) throw new Error('No hay un desafío de MFA en curso.')
    return mfaToken
  }

  const mfaSetup = (): Promise<MfaSetupResponse> => authApi.mfaSetup(exigirMfaToken())

  const mfaActivate = async (codigo: string): Promise<string[]> => {
    const { sesion, codigosRecuperacion } = await authApi.mfaActivate(exigirMfaToken(), codigo)
    aplicarSesion(sesion)
    return codigosRecuperacion
  }

  const mfaVerify = async (codigo: string): Promise<void> => {
    aplicarSesion(await authApi.mfaVerify(exigirMfaToken(), codigo))
  }

  const mfaRecovery = async (codigo: string): Promise<void> => {
    aplicarSesion(await authApi.mfaRecovery(exigirMfaToken(), codigo))
  }

  const cancelarMfa = () => setMfaToken(null)

  const tienePermiso = (modulo: Modulo, operacion: Operacion): boolean =>
    user?.permisos.some(p => p.modulo === modulo && p.operacion === operacion) ?? false

  return (
    <AuthContext.Provider value={{ user, token, login, loginConGoogle, mfaSetup, mfaActivate, mfaVerify, mfaRecovery, cancelarMfa, logout, isAuthenticated: !!user, isLoading, tienePermiso }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
