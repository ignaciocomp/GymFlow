import { describe, it, expect, vi, beforeEach } from 'vitest'

// Mockeamos axios.create para capturar las llamadas que hace el módulo de servicios.
const post = vi.fn()
const get = vi.fn()
const del = vi.fn()
const put = vi.fn()
const patch = vi.fn()
const interceptorsUse = vi.fn()

vi.mock('axios', () => {
  const instance = {
    post, get, delete: del, put, patch,
    defaults: { headers: { common: {} as Record<string, string> } },
    interceptors: { response: { use: interceptorsUse } },
  }
  return {
    default: { create: () => instance },
    isAxiosError: () => false,
  }
})

// Import después del mock para que el módulo use la instancia mockeada.
const { authApi } = await import('@/services/api')

beforeEach(() => {
  post.mockReset()
  get.mockReset()
  del.mockReset()
})

describe('authApi.login (LoginResultado)', () => {
  it('devuelve el LoginResultado tal cual del backend', async () => {
    post.mockResolvedValueOnce({
      data: { requiereMfa: true, setupRequerido: true, mfaToken: 'tok-123', sesion: null },
    })

    const res = await authApi.login('admin@gymflow.com', 'secreto')

    expect(post).toHaveBeenCalledWith('/auth/login', { correo: 'admin@gymflow.com', password: 'secreto' })
    expect(res.requiereMfa).toBe(true)
    expect(res.setupRequerido).toBe(true)
    expect(res.mfaToken).toBe('tok-123')
    expect(res.sesion).toBeNull()
  })
})

describe('authApi MFA: mandan el mfaToken por Authorization Bearer', () => {
  it('mfaSetup llama /auth/mfa/setup con el Bearer del mfaToken', async () => {
    post.mockResolvedValueOnce({ data: { uriOtpauth: 'otpauth://x', qrDataUri: 'data:image/png;base64,AA', claveManual: 'JBSW' } })

    const res = await authApi.mfaSetup('mfa-tok')

    expect(post).toHaveBeenCalledWith('/auth/mfa/setup', undefined, {
      headers: { Authorization: 'Bearer mfa-tok' },
    })
    expect(res.claveManual).toBe('JBSW')
  })

  it('mfaActivate manda el código y el Bearer, y devuelve sesión + códigos', async () => {
    post.mockResolvedValueOnce({
      data: { sesion: { token: 'jwt', nombre: 'Ana', apellido: 'Pérez', correo: 'a@a.com', rolNombre: 'Admin', permisos: [] }, codigosRecuperacion: ['AAA', 'BBB'] },
    })

    const res = await authApi.mfaActivate('mfa-tok', '123456')

    expect(post).toHaveBeenCalledWith('/auth/mfa/activate', { codigo: '123456' }, {
      headers: { Authorization: 'Bearer mfa-tok' },
    })
    expect(res.sesion.token).toBe('jwt')
    expect(res.codigosRecuperacion).toEqual(['AAA', 'BBB'])
  })

  it('mfaVerify llama /auth/mfa/verify con Bearer y devuelve LoginResponse', async () => {
    post.mockResolvedValueOnce({ data: { token: 'jwt', nombre: 'Ana', apellido: 'Pérez', correo: 'a@a.com', rolNombre: 'Admin', permisos: [] } })

    const res = await authApi.mfaVerify('mfa-tok', '654321')

    expect(post).toHaveBeenCalledWith('/auth/mfa/verify', { codigo: '654321' }, {
      headers: { Authorization: 'Bearer mfa-tok' },
    })
    expect(res.token).toBe('jwt')
  })

  it('mfaRecovery llama /auth/mfa/recovery con Bearer y devuelve LoginResponse', async () => {
    post.mockResolvedValueOnce({ data: { token: 'jwt', nombre: 'Ana', apellido: 'Pérez', correo: 'a@a.com', rolNombre: 'Admin', permisos: [] } })

    const res = await authApi.mfaRecovery('mfa-tok', 'CODIGO-RECU')

    expect(post).toHaveBeenCalledWith('/auth/mfa/recovery', { codigo: 'CODIGO-RECU' }, {
      headers: { Authorization: 'Bearer mfa-tok' },
    })
    expect(res.token).toBe('jwt')
  })
})

describe('empleados: resetearMfaEmpleado', () => {
  it('hace POST a /empleados/{id}/mfa/reset', async () => {
    const { resetearMfaEmpleado } = await import('@/services/empleados')
    post.mockResolvedValueOnce({ data: undefined })

    await resetearMfaEmpleado('emp-1')

    expect(post).toHaveBeenCalledWith('/empleados/emp-1/mfa/reset')
  })
})
