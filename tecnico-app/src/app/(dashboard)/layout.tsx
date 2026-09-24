'use client'

import { useEffect, useMemo, useState } from 'react'
import { useRouter } from 'next/navigation'
import { useAuthStore } from '@/stores/authStore'
import { authApi } from '@/lib/api/auth'
import { Sidebar } from '@/components/shared/Sidebar'
import { ErrorBoundary } from '@/components/shared/ErrorBoundary'
import { brandStyleSheet } from '@/lib/utils/color'
import { useCanManage } from '@/hooks/useCanManage'

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter()
  const { user, accessToken, _hasHydrated, setAuth, clearAuth } = useAuthStore()
  const canManage = useCanManage()
  const [ready, setReady] = useState(false)

  useEffect(() => {
    // Wait for Zustand persist to hydrate from localStorage before checking auth.
    // Without this, user is null on first render and we'd redirect incorrectly.
    if (!_hasHydrated) return

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
        setAuth(data.user, data.accessToken, data.csrfToken)
        setReady(true)
      })
      .catch(() => {
        clearAuth()
        router.replace('/login')
      })
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [_hasHydrated])

  // First-run onboarding: an Owner/Admin with no company set up yet has nothing
  // meaningful to see on the dashboard, so send them through the wizard first.
  // Technician/Commercial are never redirected here — company setup is Owner/Admin-only
  // and useCanManage() already gates that, so this can't ever trap a non-manager.
  useEffect(() => {
    if (!ready) return
    if (canManage && !user?.companyName) {
      router.replace('/onboarding')
    }
  }, [ready, canManage, user?.companyName, router])

  // Per-install rebranding: a company's chosen brand color overrides the default amber
  // token set at runtime, so a fresh install just needs Perfil filled in, not a rebuild.
  const brandStyle = useMemo(() => {
    if (!user?.brandColor) return null
    return brandStyleSheet(user.brandColor)
  }, [user?.brandColor])

  const needsOnboarding = canManage && !user?.companyName

  if (!ready || needsOnboarding) {
    return (
      <div className="min-h-screen flex items-center justify-center" style={{ backgroundColor: 'var(--color-canvas)' }}>
        <div className="w-5 h-5 rounded-full border-2 animate-spin" style={{ borderColor: 'var(--color-brand-500)', borderTopColor: 'transparent' }} />
      </div>
    )
  }

  return (
    <div className="flex h-screen" style={{ backgroundColor: 'var(--color-canvas)' }}>
      {brandStyle && <style>{brandStyle}</style>}
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
