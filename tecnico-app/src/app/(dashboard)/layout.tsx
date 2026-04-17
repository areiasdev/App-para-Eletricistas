'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { useAuthStore } from '@/stores/authStore'
import { authApi } from '@/lib/api/auth'
import { Sidebar } from '@/components/shared/Sidebar'
import { ErrorBoundary } from '@/components/shared/ErrorBoundary'

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter()
  const { user, accessToken, setAuth, clearAuth } = useAuthStore()
  const [ready, setReady] = useState(false)

  useEffect(() => {
    // Case 1: fresh session — no user at all → go to login
    if (!user) {
      router.replace('/login')
      return
    }

    // Case 2: accessToken already in memory (normal nav within session)
    if (accessToken) {
      setReady(true)
      return
    }

    // Case 3: page reload — user persisted but token is gone from memory.
    // Attempt a silent refresh using the httpOnly cookie.
    authApi
      .refresh()
      .then((data) => {
        setAuth(data.user, data.accessToken)
        setReady(true)
      })
      .catch(() => {
        clearAuth()
        router.replace('/login')
      })
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  if (!ready) {
    return (
      <div className="min-h-screen flex items-center justify-center" style={{ backgroundColor: 'var(--color-canvas)' }}>
        <div className="w-5 h-5 rounded-full border-2 animate-spin" style={{ borderColor: 'var(--color-brand-500)', borderTopColor: 'transparent' }} />
      </div>
    )
  }

  return (
    <div className="flex h-screen" style={{ backgroundColor: 'var(--color-canvas)' }}>
      <Sidebar />
      {/* pt-14 on mobile to clear the fixed top bar; lg:pt-0 since sidebar is inline */}
      <main className="flex-1 overflow-y-auto relative pt-14 lg:pt-0">
        <div className="p-6 lg:p-8 max-w-5xl mx-auto animate-fade-up">
            <ErrorBoundary>{children}</ErrorBoundary>
          </div>
      </main>
    </div>
  )
}
