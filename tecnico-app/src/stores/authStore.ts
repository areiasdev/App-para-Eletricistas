import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { User } from '@/types'

interface AuthState {
  user: User | null
  accessToken: string | null
  csrfToken: string | null
  _hasHydrated: boolean
  setAuth: (user: User, accessToken: string, csrfToken: string) => void
  setAccessToken: (accessToken: string, csrfToken: string) => void
  clearAuth: () => void
  setHasHydrated: (v: boolean) => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      accessToken: null,
      csrfToken: null,
      _hasHydrated: false,
      setAuth: (user, accessToken, csrfToken) => set({ user, accessToken, csrfToken }),
      setAccessToken: (accessToken, csrfToken) => set({ accessToken, csrfToken }),
      clearAuth: () => set({ user: null, accessToken: null, csrfToken: null }),
      setHasHydrated: (v) => set({ _hasHydrated: v }),
    }),
    {
      name: 'tecnicoapp-auth',
      // Access token lives in memory only (XSS mitigation) — never persisted.
      // Refresh token is stored in an httpOnly cookie (not accessible to JS at all).
      // csrfToken IS persisted: it's a double-submit value, not a bearer credential —
      // it only proves the request originated from JS able to read our own storage
      // (same-origin), which a cross-site CSRF page can't do. It must survive reloads
      // so the silent-refresh-on-reload flow (see (dashboard)/layout.tsx) can pass it.
      partialize: (state) => ({ user: state.user, csrfToken: state.csrfToken }),
      onRehydrateStorage: () => (state) => {
        state?.setHasHydrated(true)
      },
    }
  )
)
