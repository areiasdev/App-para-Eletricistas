import axios, { type AxiosInstance, type InternalAxiosRequestConfig } from 'axios'
import { useAuthStore } from '@/stores/authStore'

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'

export const api: AxiosInstance = axios.create({
  baseURL: `${BASE_URL}/api/v1`,
  headers: { 'Content-Type': 'application/json' },
  // withCredentials is required so the httpOnly refresh token cookie is sent
  // on every request (and received when the backend sets it).
  withCredentials: true,
})

// Attach access token (in-memory, not localStorage) to every request
api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = useAuthStore.getState().accessToken
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Handle 401 — attempt silent refresh then retry
let isRefreshing = false
let failedQueue: Array<{
  resolve: (token: string) => void
  reject: (err: unknown) => void
}> = []

const processQueue = (error: unknown, token: string | null) => {
  failedQueue.forEach((p) => (error ? p.reject(error) : p.resolve(token!)))
  failedQueue = []
}

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config

    // 403 Forbidden — user is authenticated but lacks permission; redirect to dashboard
    if (error.response?.status === 403) {
      if (typeof window !== 'undefined') window.location.href = '/dashboard'
      return Promise.reject(error)
    }

    // Exclude all /auth/* endpoints — a failed login/register attempt returns 401/409
    // for its own reasons (wrong password, etc.) and must never trigger a silent-refresh
    // attempt, which would swallow the real error behind a refresh failure instead.
    if (error.response?.status !== 401 || originalRequest._retry || originalRequest.url?.includes('/auth/')) {
      return Promise.reject(error)
    }

    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        failedQueue.push({ resolve, reject })
      }).then((token) => {
        originalRequest.headers.Authorization = `Bearer ${token}`
        return api(originalRequest)
      })
    }

    originalRequest._retry = true
    isRefreshing = true

    try {
      // POST to /auth/refresh — no body, refresh token comes from httpOnly cookie.
      // X-Csrf-Token proves this request came from our own JS (double-submit CSRF check).
      const { data } = await axios.post(
        `${BASE_URL}/api/v1/auth/refresh`,
        {},
        {
          withCredentials: true,
          headers: { 'X-Csrf-Token': useAuthStore.getState().csrfToken ?? '' },
        }
      )
      useAuthStore.getState().setAccessToken(data.accessToken, data.csrfToken)
      processQueue(null, data.accessToken)
      originalRequest.headers.Authorization = `Bearer ${data.accessToken}`
      return api(originalRequest)
    } catch (err) {
      processQueue(err, null)
      useAuthStore.getState().clearAuth()
      window.location.href = '/login'
      return Promise.reject(err)
    } finally {
      isRefreshing = false
    }
  }
)

export const getErrorMessage = (error: unknown): string => {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data
    // Validation errors from Ardalis.Result (422) — flatten first error value
    if (data?.errors && typeof data.errors === 'object') {
      const first = Object.values(data.errors)[0]
      if (Array.isArray(first) && first.length > 0) return first[0] as string
    }
    if (data?.detail) return data.detail
    if (data?.title && data.title !== 'One or more validation errors occurred.')
      return data.title
    if (typeof data === 'string') return data
  }
  return 'Ocorreu um erro inesperado. Tenta novamente.'
}
