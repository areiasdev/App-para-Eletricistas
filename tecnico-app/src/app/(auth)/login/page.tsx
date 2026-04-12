'use client'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { authApi } from '@/lib/api/auth'
import { loginSchema, type LoginFormValues } from '@/lib/validations/auth'
import { useAuthStore } from '@/stores/authStore'
import { getErrorMessage } from '@/lib/api/client'

export default function LoginPage() {
  const router = useRouter()
  const setAuth = useAuthStore((s) => s.setAuth)

  const { register, handleSubmit, formState: { errors } } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
  })

  const login = useMutation({
    mutationFn: authApi.login,
    onSuccess: (data) => {
      setAuth(data.user, data.accessToken, data.refreshToken)
      localStorage.setItem('accessToken', data.accessToken)
      localStorage.setItem('refreshToken', data.refreshToken)
      router.push('/dashboard')
    },
  })

  return (
    <div
      className="min-h-screen flex"
      style={{ backgroundColor: 'var(--color-canvas)' }}
    >
      {/* Left panel — branding */}
      <div
        className="hidden lg:flex w-80 shrink-0 flex-col justify-between p-10"
        style={{ backgroundColor: 'var(--color-sidebar)' }}
      >
        <div className="flex items-center gap-2.5">
          <span
            className="flex items-center justify-center w-8 h-8 rounded-md text-base font-bold"
            style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
          >
            ⚡
          </span>
          <span className="text-sm font-semibold text-white/90 tracking-tight">TécnicoApp</span>
        </div>

        <div className="space-y-6">
          {[
            { n: '340+', l: 'orçamentos gerados' },
            { n: '98%',  l: 'aprovação dos clientes' },
            { n: '2×',   l: 'mais rápido a faturar' },
          ].map(({ n, l }) => (
            <div key={l}>
              <p className="text-3xl font-bold text-white" style={{ fontFamily: 'var(--font-outfit)' }}>{n}</p>
              <p className="text-sm mt-0.5" style={{ color: 'rgba(255,255,255,0.4)' }}>{l}</p>
            </div>
          ))}
        </div>

        <p className="text-xs" style={{ color: 'rgba(255,255,255,0.25)' }}>
          © 2026 TécnicoApp · Para eletricistas e técnicos de AVAC
        </p>
      </div>

      {/* Right panel — form */}
      <div className="flex-1 flex items-center justify-center px-6 py-12">
        <div className="w-full max-w-sm animate-fade-up">

          {/* Mobile logo */}
          <div className="flex items-center gap-2 mb-8 lg:hidden">
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

          <div className="mb-8">
            <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>
              Bem-vindo de volta
            </h1>
            <p className="mt-1 text-sm" style={{ color: 'var(--color-muted)' }}>
              Inicia sessão para continuar
            </p>
          </div>

          <form onSubmit={handleSubmit((d) => login.mutate(d))} className="space-y-4">
            <Field label="Email" error={errors.email?.message}>
              <input
                type="email"
                autoComplete="email"
                {...register('email')}
                className="auth-input"
                placeholder="tu@empresa.pt"
              />
            </Field>

            <Field label="Password" error={errors.password?.message}>
              <input
                type="password"
                autoComplete="current-password"
                {...register('password')}
                className="auth-input"
                placeholder="••••••••"
              />
            </Field>

            {login.isError && (
              <p className="text-sm rounded-lg px-4 py-3 border" style={{ color: '#dc2626', backgroundColor: '#fef2f2', borderColor: '#fecaca' }}>
                {getErrorMessage(login.error)}
              </p>
            )}

            <button
              type="submit"
              disabled={login.isPending}
              className="w-full rounded-lg py-2.5 text-sm font-semibold transition-all duration-150 disabled:opacity-60 disabled:cursor-not-allowed hover:brightness-110 active:scale-[0.99]"
              style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
            >
              {login.isPending ? 'A entrar...' : 'Entrar'}
            </button>
          </form>

          <p className="mt-6 text-center text-sm" style={{ color: 'var(--color-muted)' }}>
            Não tens conta?{' '}
            <Link
              href="/register"
              className="font-semibold transition-colors"
              style={{ color: 'var(--color-brand-600)' }}
            >
              Regista-te gratuitamente
            </Link>
          </p>
        </div>
      </div>

      <style>{`
        .auth-input {
          width: 100%;
          border-radius: 0.5rem;
          border: 1.5px solid var(--color-line-strong);
          padding: 0.625rem 0.875rem;
          font-size: 0.875rem;
          background-color: white;
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

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
        {label}
      </label>
      {children}
      {error && <p className="mt-1.5 text-xs" style={{ color: '#dc2626' }}>{error}</p>}
    </div>
  )
}
