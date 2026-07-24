import { api } from './client'
import { useAuthStore } from '@/stores/authStore'
import type { AuthResponse } from '@/types'

// X-Csrf-Token proves a refresh/logout request came from our own JS reading our own
// storage (double-submit CSRF check) — a cross-site forged request can't know this value.
const csrfHeaders = () => ({ headers: { 'X-Csrf-Token': useAuthStore.getState().csrfToken ?? '' } })

export interface RegisterRequest {
  fullName: string
  email: string
  password: string
}

export interface LoginRequest {
  email: string
  password: string
}

export const authApi = {
  register: (data: RegisterRequest) =>
    api.post<AuthResponse>('/auth/register', data).then((r) => r.data),

  login: (data: LoginRequest) =>
    api.post<AuthResponse>('/auth/login', data).then((r) => r.data),

  // No body — refresh token is read from the httpOnly cookie automatically
  refresh: () =>
    api.post<AuthResponse>('/auth/refresh', {}, csrfHeaders()).then((r) => r.data),

  logout: () =>
    api.post('/auth/logout', {}, csrfHeaders()),

  forgotPassword: (email: string) =>
    api.post('/auth/forgot-password', { email }),

  resetPassword: (data: { email: string; token: string; newPassword: string }) =>
    api.post('/auth/reset-password', data),
}
