'use client'

import { useEffect } from 'react'
import { useRouter } from 'next/navigation'
import { useAuthStore } from '@/stores/authStore'
import { Sidebar } from '@/components/shared/Sidebar'
import { ErrorBoundary } from '@/components/shared/ErrorBoundary'

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter()
  const accessToken = useAuthStore((s) => s.accessToken)

  useEffect(() => {
    if (!accessToken) router.replace('/login')
  }, [accessToken, router])

  if (!accessToken) {
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
