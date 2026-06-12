import { createContext, useContext, useState, useEffect, useCallback } from 'react'
import type { ReactNode } from 'react'
import api, { authApi } from '@/services/api'
import type { LoginResponse } from '@/services/api'
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
  login: (correo: string, password: string) => Promise<void>
  loginConGoogle: (idToken: string) => Promise<void>
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

  const logout = useCallback(() => {
    localStorage.removeItem('gymflow_token')
    delete api.defaults.headers.common['Authorization']
    setToken(null)
    setUser(null)
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
    setUser({
      nombre: data.nombre,
      apellido: data.apellido,
      correo: data.correo,
      rolNombre: data.rolNombre,
      permisos: data.permisos ?? [],
      unidadIds: data.unidadIds ?? [],
    })
  }

  const login = async (correo: string, password: string) => {
    aplicarSesion(await authApi.login(correo, password))
  }

  const loginConGoogle = async (idToken: string) => {
    aplicarSesion(await authApi.loginConGoogle(idToken))
  }

  const tienePermiso = (modulo: Modulo, operacion: Operacion): boolean =>
    user?.permisos.some(p => p.modulo === modulo && p.operacion === operacion) ?? false

  return (
    <AuthContext.Provider value={{ user, token, login, loginConGoogle, logout, isAuthenticated: !!user, isLoading, tienePermiso }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
