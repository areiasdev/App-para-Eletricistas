'use client'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { authApi } from '@/lib/api/auth'
import { registerSchema, type RegisterFormValues } from '@/lib/validations/auth'
import { useAuthStore } from '@/stores/authStore'
import { getErrorMessage } from '@/lib/api/client'

export default function RegisterPage() {
  const router = useRouter()
  const setAuth = useAuthStore((s) => s.setAuth)

  const { register, handleSubmit, formState: { errors } } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
  })

  const registerMutation = useMutation({
    mutationFn: (data: RegisterFormValues) =>
      authApi.register({ fullName: data.fullName, email: data.email, password: data.password }),
    onSuccess: (data) => {
      setAuth(data.user, data.accessToken, data.refreshToken)
      localStorage.setItem('accessToken', data.accessToken)
      localStorage.setItem('refreshToken', data.refreshToken)
      router.push('/dashboard')
    },
  })

  return (
    <div className="min-h-screen flex items-center justify-center px-6 py-12" style={{ backgroundColor: 'var(--color-canvas)' }}>
      <div className="w-full max-w-sm animate-fade-up">

        <div className="flex items-center gap-2 mb-10 justify-center">
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

        <div className="mb-8 text-center">
          <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>
            Cria a tua conta
          </h1>
          <p className="mt-1 text-sm" style={{ color: 'var(--color-muted)' }}>
            Gratuito para começar · Sem cartão de crédito
          </p>
        </div>

        <div
          className="rounded-2xl p-6 border"
          style={{ backgroundColor: 'white', borderColor: 'var(--color-line)' }}
        >
          <form onSubmit={handleSubmit((d) => registerMutation.mutate(d))} className="space-y-4">
            <Field label="Nome completo" error={errors.fullName?.message}>
              <input
                type="text"
                autoComplete="name"
                {...register('fullName')}
                className="auth-input"
                placeholder="João Silva"
              />
            </Field>

            <Field label="Email" error={errors.email?.message}>
              <input
                type="email"
                autoComplete="email"
                {...register('email')}
                className="auth-input"
                placeholder="joao@empresa.pt"
              />
            </Field>

            <Field label="Password" error={errors.password?.message}>
              <input
                type="password"
                autoComplete="new-password"
                {...register('password')}
                className="auth-input"
                placeholder="Mínimo 8 caracteres"
              />
            </Field>

            <Field label="Confirmar password" error={errors.confirmPassword?.message}>
              <input
                type="password"
                autoComplete="new-password"
                {...register('confirmPassword')}
                className="auth-input"
                placeholder="••••••••"
              />
            </Field>

            {registerMutation.isError && (
              <p className="text-sm rounded-lg px-4 py-3 border" style={{ color: '#dc2626', backgroundColor: '#fef2f2', borderColor: '#fecaca' }}>
                {getErrorMessage(registerMutation.error)}
              </p>
            )}

            <button
              type="submit"
              disabled={registerMutation.isPending}
              className="w-full rounded-lg py-2.5 text-sm font-semibold transition-all duration-150 disabled:opacity-60 disabled:cursor-not-allowed hover:brightness-110 active:scale-[0.99] mt-2"
              style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
            >
              {registerMutation.isPending ? 'A criar conta...' : 'Criar conta gratuita'}
            </button>
          </form>
        </div>

        <p className="mt-6 text-center text-sm" style={{ color: 'var(--color-muted)' }}>
          Já tens conta?{' '}
          <Link
            href="/login"
            className="font-semibold transition-colors"
            style={{ color: 'var(--color-brand-600)' }}
          >
            Inicia sessão
          </Link>
        </p>
      </div>

      <style>{`
        .auth-input {
          width: 100%;
          border-radius: 0.5rem;
          border: 1.5px solid var(--color-line-strong);
          padding: 0.625rem 0.875rem;
          font-size: 0.875rem;
          background-color: var(--color-canvas);
          color: var(--color-ink);
          outline: none;
          transition: border-color 0.15s;
          font-family: var(--font-outfit), system-ui, sans-serif;
        }
        .auth-input:focus {
          border-color: var(--color-brand-500);
          box-shadow: 0 0 0 3px rgba(245, 158, 11, 0.12);
          background-color: white;
        }
        .auth-input::placeholder { color: var(--color-subtle); }
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
