'use client'

import { useState, useEffect, Suspense } from 'react'
import { useSearchParams, useRouter } from 'next/navigation'
import Link from 'next/link'
import { useAcceptInvite } from '@/hooks/useTeam'
import { getErrorMessage } from '@/lib/api/client'

function AceitarConviteInner() {
  const searchParams = useSearchParams()
  const router = useRouter()
  const token = searchParams.get('token') ?? ''

  const [fullName, setFullName] = useState('')
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState(false)

  const accept = useAcceptInvite()

  useEffect(() => {
    if (!token) setError('Link de convite inválido ou expirado.')
  }, [token])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    if (password !== confirm) {
      setError('As palavras-passe não coincidem.')
      return
    }
    accept.mutate(
      { token, fullName, newPassword: password },
      {
        onSuccess: () => setDone(true),
        onError: (err) => setError(getErrorMessage(err)),
      }
    )
  }

  if (done) {
    return (
      <div className="min-h-screen flex items-center justify-center" style={{ backgroundColor: 'var(--color-canvas)' }}>
        <div className="w-full max-w-sm text-center space-y-4">
          <div className="w-12 h-12 rounded-full flex items-center justify-center mx-auto"
            style={{ backgroundColor: 'rgba(16,185,129,0.15)' }}>
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
              <path d="M5 13l4 4L19 7" stroke="#34d399" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
          </div>
          <h1 className="text-xl font-bold" style={{ color: 'var(--color-ink)' }}>Convite aceite!</h1>
          <p className="text-sm" style={{ color: 'var(--color-muted)' }}>
            A tua conta está ativa. Podes fazer login agora.
          </p>
          <Link
            href="/login"
            className="inline-block rounded-lg px-6 py-2.5 text-sm font-semibold transition-all duration-150 hover:brightness-110"
            style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
          >
            Ir para o login →
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center px-4" style={{ backgroundColor: 'var(--color-canvas)' }}>
      <div className="w-full max-w-sm space-y-6">
        <div className="text-center space-y-1">
          <div className="flex items-center justify-center gap-2 mb-4">
            <span className="flex items-center justify-center w-8 h-8 rounded-md text-base font-bold"
              style={{ backgroundColor: 'var(--color-brand-500)', color: '#17171a' }}>
              T
            </span>
            <span className="text-lg font-bold" style={{ color: 'var(--color-ink)' }}>TécnicoApp</span>
          </div>
          <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Aceitar convite</h1>
          <p className="text-sm" style={{ color: 'var(--color-muted)' }}>
            Define o teu nome e palavra-passe para ativar a conta.
          </p>
        </div>

        {error && (
          <p className="text-sm rounded-lg px-4 py-3" style={{ color: '#dc2626', backgroundColor: '#fef2f2' }}>
            {error}
          </p>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
              Nome completo
            </label>
            <input
              type="text"
              value={fullName}
              onChange={e => setFullName(e.target.value)}
              placeholder="João Silva"
              required
              className="w-full rounded-lg border px-3 py-2.5 text-sm outline-none transition-all duration-150"
              style={{
                borderColor: 'var(--color-line-strong)',
                backgroundColor: 'var(--color-canvas)',
                color: 'var(--color-ink)',
              }}
            />
          </div>
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
              Nova palavra-passe
            </label>
            <input
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              placeholder="Mínimo 8 caracteres"
              required
              className="w-full rounded-lg border px-3 py-2.5 text-sm outline-none transition-all duration-150"
              style={{
                borderColor: 'var(--color-line-strong)',
                backgroundColor: 'var(--color-canvas)',
                color: 'var(--color-ink)',
              }}
            />
          </div>
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
              Confirmar palavra-passe
            </label>
            <input
              type="password"
              value={confirm}
              onChange={e => setConfirm(e.target.value)}
              placeholder="Repetir palavra-passe"
              required
              className="w-full rounded-lg border px-3 py-2.5 text-sm outline-none transition-all duration-150"
              style={{
                borderColor: 'var(--color-line-strong)',
                backgroundColor: 'var(--color-canvas)',
                color: 'var(--color-ink)',
              }}
            />
          </div>
          <button
            type="submit"
            disabled={accept.isPending || !token}
            className="w-full rounded-lg py-2.5 text-sm font-semibold transition-all duration-150 disabled:opacity-60 hover:brightness-110"
            style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
          >
            {accept.isPending ? 'A ativar...' : 'Ativar conta'}
          </button>
        </form>
      </div>
    </div>
  )
}

export default function AceitarConvitePage() {
  return (
    <Suspense>
      <AceitarConviteInner />
    </Suspense>
  )
}
