'use client'

import { useState } from 'react'
import Link from 'next/link'
import { authApi } from '@/lib/api/auth'
import { getErrorMessage } from '@/lib/api/client'

export default function EsqueciPasswordPage() {
  const [email, setEmail] = useState('')
  const [isPending, setIsPending] = useState(false)
  const [sent, setSent] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setIsPending(true)
    try {
      await authApi.forgotPassword(email)
      setSent(true)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setIsPending(false)
    }
  }

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
          Envia-te um email para redefires a tua password em segurança.
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

          {sent ? (
            <div className="space-y-4">
              <div className="w-12 h-12 rounded-full flex items-center justify-center" style={{ backgroundColor: '#d1fae5' }}>
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
                  <path d="M5 13l4 4L19 7" stroke="#059669" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
              </div>
              <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Email enviado</h1>
              <p className="text-sm" style={{ color: 'var(--color-muted)' }}>
                Se existe uma conta associada a <strong>{email}</strong>, receberás um email com instruções para redefinir a tua password.
                O link é válido durante 1 hora.
              </p>
              <p className="text-sm" style={{ color: 'var(--color-subtle)' }}>
                Não recebeste? Verifica a pasta de spam ou{' '}
                <button
                  onClick={() => setSent(false)}
                  className="underline underline-offset-2 font-medium"
                  style={{ color: 'var(--color-brand-600)' }}
                >
                  tenta novamente
                </button>.
              </p>
              <Link
                href="/login"
                className="block text-center mt-4 text-sm font-semibold"
                style={{ color: 'var(--color-muted)' }}
              >
                ← Voltar ao login
              </Link>
            </div>
          ) : (
            <>
              <div className="mb-8">
                <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>
                  Esqueceste a password?
                </h1>
                <p className="mt-1 text-sm" style={{ color: 'var(--color-muted)' }}>
                  Indica o teu email e enviamos instruções para redefini-la.
                </p>
              </div>

              <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                  <label className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
                    Email
                  </label>
                  <input
                    type="email"
                    required
                    autoComplete="email"
                    value={email}
                    onChange={e => setEmail(e.target.value)}
                    className="auth-input"
                    placeholder="tu@empresa.pt"
                  />
                </div>

                {error && (
                  <p className="text-sm rounded-lg px-4 py-3 border" style={{ color: '#dc2626', backgroundColor: '#fef2f2', borderColor: '#fecaca' }}>
                    {error}
                  </p>
                )}

                <button
                  type="submit"
                  disabled={isPending || !email}
                  className="w-full rounded-lg py-2.5 text-sm font-semibold transition-all duration-150 disabled:opacity-60 disabled:cursor-not-allowed hover:brightness-110 active:scale-[0.99]"
                  style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
                >
                  {isPending ? 'A enviar...' : 'Enviar email'}
                </button>
              </form>

              <p className="mt-6 text-center text-sm" style={{ color: 'var(--color-muted)' }}>
                <Link
                  href="/login"
                  className="font-semibold transition-colors"
                  style={{ color: 'var(--color-brand-600)' }}
                >
                  ← Voltar ao login
                </Link>
              </p>
            </>
          )}
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
