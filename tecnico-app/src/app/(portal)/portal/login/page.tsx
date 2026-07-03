'use client'

import { useEffect, useState, Suspense } from 'react'
import { useSearchParams, useRouter } from 'next/navigation'
import { portal, setPortalToken } from '@/lib/api/portal'
import { usePortalStore } from '@/stores/portalStore'

function PortalLoginInner() {
  const searchParams = useSearchParams()
  const router = useRouter()
  const token = searchParams.get('token') ?? ''
  const { setPortal } = usePortalStore()
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!token) {
      setError('Link de acesso inválido ou expirado. Pede ao teu técnico um novo link.')
      return
    }

    setLoading(true)
    portal
      .login(token)
      .then((data) => {
        setPortalToken(data.accessToken)
        setPortal(data.accessToken, data.clientName, data.clientEmail)
        router.replace('/portal/dashboard')
      })
      .catch(() => {
        setError('Link de acesso inválido ou expirado. Pede ao teu técnico um novo link.')
        setLoading(false)
      })
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token])

  return (
    <div className="min-h-screen flex items-center justify-center px-4" style={{ backgroundColor: 'var(--color-canvas)' }}>
      <div className="w-full max-w-sm text-center space-y-6">
        <div className="flex items-center justify-center gap-2">
          <span className="flex items-center justify-center w-8 h-8 rounded-md text-base font-bold"
            style={{ backgroundColor: 'var(--color-brand-500)', color: '#17171a' }}>
            ⚡
          </span>
          <span className="text-lg font-bold" style={{ color: 'var(--color-ink)' }}>TécnicoApp</span>
        </div>

        {loading && (
          <div className="space-y-3">
            <div className="w-8 h-8 rounded-full border-2 animate-spin mx-auto"
              style={{ borderColor: 'var(--color-brand-500)', borderTopColor: 'transparent' }} />
            <p className="text-sm" style={{ color: 'var(--color-muted)' }}>A verificar o acesso…</p>
          </div>
        )}

        {error && (
          <div className="rounded-xl border px-5 py-6 space-y-3"
            style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
            <p className="text-sm font-medium" style={{ color: 'var(--color-ink)' }}>{error}</p>
            <p className="text-xs" style={{ color: 'var(--color-muted)' }}>
              Contacta o teu técnico para receber um novo link de acesso.
            </p>
          </div>
        )}
      </div>
    </div>
  )
}

export default function PortalLoginPage() {
  return (
    <Suspense>
      <PortalLoginInner />
    </Suspense>
  )
}
