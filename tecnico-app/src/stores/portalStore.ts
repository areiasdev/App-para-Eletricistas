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
    {
      name: 'tecnicoapp-portal',
      // Only persist display info — the access token lives in memory only (XSS mitigation),
      // matching authStore. The portal magic link is reusable, so a client can simply
      // re-click the emailed link to re-authenticate after a reload or closed tab.
      partialize: (state) => ({ clientName: state.clientName, clientEmail: state.clientEmail }),
    }
  )
)
