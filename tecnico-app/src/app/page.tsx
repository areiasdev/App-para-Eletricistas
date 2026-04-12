'use client'

import { useEffect } from 'react'
import { useRouter } from 'next/navigation'
import { useAuthStore } from '@/stores/authStore'

export default function Home() {
  const router = useRouter()
  const accessToken = useAuthStore((s) => s.accessToken)

  useEffect(() => {
    if (accessToken) {
      router.replace('/dashboard')
    } else {
      router.replace('/login')
    }
  }, [accessToken, router])

  return (
    <div className="min-h-screen flex items-center justify-center" style={{ backgroundColor: 'var(--color-canvas)' }}>
      <div className="flex items-center gap-2.5">
        <span
          className="flex items-center justify-center w-8 h-8 rounded-md text-base font-bold"
          style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
        >
          ⚡
        </span>
        <span className="text-sm font-semibold tracking-tight" style={{ color: 'var(--color-ink)' }}>
          TécnicoApp
        </span>
      </div>
    </div>
  )
}
