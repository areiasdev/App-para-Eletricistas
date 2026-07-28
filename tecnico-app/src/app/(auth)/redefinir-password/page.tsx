'use client'

import { useState, Suspense } from 'react'
import { useSearchParams, useRouter } from 'next/navigation'
import Link from 'next/link'
import { authApi } from '@/lib/api/auth'
import { getErrorMessage } from '@/lib/api/client'

function ResetPasswordForm() {
  const searchParams = useSearchParams()
  const router = useRouter()
  const email = searchParams.get('email') ?? ''
  const token = searchParams.get('token') ?? ''

  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [isPending, setIsPending] = useState(false)
  const [error, setError] = useState('')
  const [done, setDone] = useState(false)

  if (!email || !token) {
    return (
      <div className="space-y-3">
        <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Link inválido</h1>
        <p className="text-sm" style={{ color: 'var(--color-muted)' }}>
          Este link de redefinição é inválido ou já foi usado.
        </p>
        <Link href="/esqueci-password" className="text-sm font-semibold" style={{ color: 'var(--color-brand-600)' }}>
          Pedir novo link
        </Link>
      </div>
    )
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (password !== confirm) {
      setError('As passwords não coincidem.')
      return
    }
    if (password.length < 8) {
      setError('A password deve ter pelo menos 8 caracteres, uma maiúscula e um número.')
      return
    }
    if (!/[A-Z]/.test(password) || !/\d/.test(password)) {
      setError('A password deve ter pelo menos uma letra maiúscula e um número.')
      return
    }
    setError('')
    setIsPending(true)
    try {
      await authApi.resetPassword({ email, token, newPassword: password })
      setDone(true)
      setTimeout(() => router.push('/login'), 3000)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setIsPending(false)
    }
  }

  if (done) {
    return (
      <div className="space-y-4">
        <div className="w-12 h-12 rounded-full flex items-center justify-center" style={{ backgroundColor: '#d1fae5' }}>
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
            <path d="M5 13l4 4L19 7" stroke="#059669" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </div>
        <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Password atualizada!</h1>
        <p className="text-sm" style={{ color: 'var(--color-muted)' }}>
          A tua password foi redefinida com sucesso. Vais ser redirecionado para o login...
        </p>
        <Link
          href="/login"
          className="block text-sm font-semibold"
          style={{ color: 'var(--color-brand-600)' }}
        >
          Ir para o login →
        </Link>
      </div>
    )
  }

  return (
    <>
      <div className="mb-8">
        <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>
          Redefinir password
        </h1>
        <p className="mt-1 text-sm" style={{ color: 'var(--color-muted)' }}>
          Escolhe uma nova password para a tua conta.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
            Nova password
          </label>
          <input
            type="password"
            required
            minLength={8}
            autoComplete="new-password"
            value={password}
            onChange={e => setPassword(e.target.value)}
            className="auth-input"
            placeholder="Mínimo 8 chars, 1 maiúscula, 1 número"
          />
        </div>

        <div>
          <label className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
            Confirmar password
          </label>
          <input
            type="password"
            required
            autoComplete="new-password"
            value={confirm}
            onChange={e => setConfirm(e.target.value)}
            className="auth-input"
            placeholder="Repete a password"
          />
        </div>

        {error && (
          <p className="text-sm rounded-lg px-4 py-3 border" style={{ color: '#dc2626', backgroundColor: '#fef2f2', borderColor: '#fecaca' }}>
            {error}
          </p>
        )}

        <button
          type="submit"
          disabled={isPending || !password || !confirm}
          className="w-full rounded-lg py-2.5 text-sm font-semibold transition-all duration-150 disabled:opacity-60 disabled:cursor-not-allowed hover:brightness-110 active:scale-[0.99]"
          style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
        >
          {isPending ? 'A guardar...' : 'Redefinir password'}
        </button>
      </form>
    </>
  )
}

export default function ResetPasswordPage() {
  return (
    <div className="min-h-screen flex" style={{ backgroundColor: 'var(--color-canvas)' }}>
      {/* Left panel */}
      <div
        className="hidden lg:flex w-80 shrink-0 flex-col justify-between p-10"
        style={{ backgroundColor: 'var(--color-sidebar)' }}
      >
        <div className="flex items-center gap-2.5">
          <span
            className="flex items-center justify-center w-8 h-8 rounded-md text-base font-bold"
            style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
          >
            T
          </span>
          <span className="text-sm font-semibold text-white/90 tracking-tight">TécnicoApp</span>
        </div>
        <p className="text-sm" style={{ color: 'rgba(255,255,255,0.4)' }}>
          Redefine a tua password de forma segura.
        </p>
        <p className="text-xs" style={{ color: 'rgba(255,255,255,0.25)' }}>
          © 2026 TécnicoApp
        </p>
      </div>

      {/* Right panel */}
      <div className="flex-1 flex items-center justify-center px-6 py-12">
        <div className="w-full max-w-sm animate-fade-up">
          <div className="flex items-center gap-2 mb-8 lg:hidden">
            <span
              className="flex items-center justify-center w-8 h-8 rounded-md text-base font-bold"
              style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
            >
              T
            </span>
            <span className="text-sm font-semibold tracking-tight" style={{ color: 'var(--color-ink)' }}>
              TécnicoApp
            </span>
          </div>

          <Suspense fallback={<div className="h-48 animate-pulse rounded-xl" style={{ backgroundColor: 'var(--color-line)' }} />}>
            <ResetPasswordForm />
          </Suspense>
        </div>
      </div>

      <style>{`
        .auth-input {
          width: 100%;
          border-radius: 0.5rem;
          border: 1.5px solid var(--color-line-strong);
          padding: 0.625rem 0.875rem;
          font-size: 0.875rem;
          background-color: var(--color-card);
          color: var(--color-ink);
          outline: none;
          transition: border-color 0.15s;
          font-family: var(--font-outfit), system-ui, sans-serif;
        }
        .auth-input:focus {
          border-color: var(--color-brand-500);
          box-shadow: 0 0 0 3px rgba(245, 158, 11, 0.12);
        }
        .auth-input::placeholder {
          color: var(--color-subtle);
        }
      `}</style>
    </div>
  )
}
