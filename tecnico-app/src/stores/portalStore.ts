import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface PortalState {
  accessToken: string | null
  clientName: string | null
  clientEmail: string | null
  setPortal: (accessToken: string, clientName: string, clientEmail: string | null) => void
  clearPortal: () => void
}

export const usePortalStore = create<PortalState>()(
  persist(
    (set) => ({
      accessToken: null,
      clientName: null,
      clientEmail: null,
      setPortal: (accessToken, clientName, clientEmail) =>
        set({ accessToken, clientName, clientEmail }),
      clearPortal: () => set({ accessToken: null, clientName: null, clientEmail: null }),
    }),
    { name: 'tecnicoapp-portal', storage: typeof window !== 'undefined' ? sessionStorageWrapper() : undefined }
  )
)

function sessionStorageWrapper() {
  return {
    getItem: (key: string) => {
      try { return JSON.parse(sessionStorage.getItem(key) ?? 'null') } catch { return null }
    },
    setItem: (key: string, value: unknown) => {
      sessionStorage.setItem(key, JSON.stringify(value))
    },
    removeItem: (key: string) => sessionStorage.removeItem(key),
  }
}
