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
import { APP_INITIAL, APP_NAME } from '@/lib/config'

export default function LoginPage() {
  const router = useRouter()
  const setAuth = useAuthStore((s) => s.setAuth)

  const { register, handleSubmit, formState: { errors } } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
  })

  const login = useMutation({
    mutationFn: authApi.login,
    onSuccess: (data) => {
      setAuth(data.user, data.accessToken, data.csrfToken)
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
            style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-on-brand)' }}
          >
            {APP_INITIAL}
          </span>
          <span className="text-sm font-semibold text-[var(--color-sidebar-text)] tracking-tight">{APP_NAME}</span>
        </div>

        <div className="space-y-3">
          <p className="text-xl font-bold text-white" style={{ fontFamily: 'var(--font-body)' }}>
            Clientes, orçamentos e intervenções — numa só ferramenta.
          </p>
          <p className="text-sm" style={{ color: 'var(--color-sidebar-muted)' }}>
            BackOffice interno para a tua equipa.
          </p>
        </div>

        <p className="text-xs" style={{ color: 'var(--color-sidebar-muted)' }}>
          © {new Date().getFullYear()} {APP_NAME}
        </p>
      </div>

      {/* Right panel — form */}
      <div className="flex-1 flex items-center justify-center px-6 py-12">
        <div className="w-full max-w-sm animate-fade-up">

          {/* Mobile logo */}
          <div className="flex items-center gap-2 mb-8 lg:hidden">
            <span
              className="flex items-center justify-center w-8 h-8 rounded-md text-base font-bold"
              style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-on-brand)' }}
            >
              {APP_INITIAL}
            </span>
            <span className="text-sm font-semibold tracking-tight" style={{ color: 'var(--color-ink)' }}>
              {APP_NAME}
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
              <p className="text-sm rounded-lg px-4 py-3 border" style={{ color: 'var(--color-danger-600)', backgroundColor: 'var(--color-danger-50)', borderColor: 'var(--color-danger-200)' }}>
                {getErrorMessage(login.error)}
              </p>
            )}

            <button
              type="submit"
              disabled={login.isPending}
              className="w-full rounded-lg py-2.5 text-sm font-semibold transition-colors duration-100 disabled:opacity-60 disabled:cursor-not-allowed"
              style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-on-brand)' }}
            >
              {login.isPending ? 'A entrar...' : 'Entrar'}
            </button>
          </form>

          <p className="mt-4 text-center text-sm">
            <Link
              href="/esqueci-password"
              className="font-medium transition-colors"
              style={{ color: 'var(--color-muted)' }}
            >
              Esqueceste a password?
            </Link>
          </p>

          <p className="mt-4 text-center text-sm" style={{ color: 'var(--color-muted)' }}>
            Não tens conta?{' '}
            <Link
              href="/register"
              className="font-semibold transition-colors"
              style={{ color: 'var(--color-brand-text)' }}
            >
              Regista-te
            </Link>
          </p>
        </div>
      </div>
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
      {error && <p className="mt-1.5 text-xs" style={{ color: 'var(--color-danger-600)' }}>{error}</p>}
    </div>
  )
}
