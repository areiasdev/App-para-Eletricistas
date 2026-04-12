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

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
  })

  const registerMutation = useMutation({
    mutationFn: (data: RegisterFormValues) =>
      authApi.register({
        fullName: data.fullName,
        email: data.email,
        password: data.password,
      }),
    onSuccess: (data) => {
      setAuth(data.user, data.accessToken, data.refreshToken)
      localStorage.setItem('accessToken', data.accessToken)
      localStorage.setItem('refreshToken', data.refreshToken)
      router.push('/dashboard')
    },
  })

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="w-full max-w-sm space-y-6">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-gray-900">TécnicoApp</h1>
          <p className="mt-1 text-sm text-gray-500">Cria a tua conta gratuita</p>
        </div>

        <form
          onSubmit={handleSubmit((data) => registerMutation.mutate(data))}
          className="space-y-4"
        >
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Nome completo
            </label>
            <input
              type="text"
              autoComplete="name"
              {...register('fullName')}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
            />
            {errors.fullName && (
              <p className="mt-1 text-xs text-red-600">{errors.fullName.message}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Email
            </label>
            <input
              type="email"
              autoComplete="email"
              {...register('email')}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
            />
            {errors.email && (
              <p className="mt-1 text-xs text-red-600">{errors.email.message}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Password
            </label>
            <input
              type="password"
              autoComplete="new-password"
              {...register('password')}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
            />
            {errors.password && (
              <p className="mt-1 text-xs text-red-600">{errors.password.message}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Confirmar password
            </label>
            <input
              type="password"
              autoComplete="new-password"
              {...register('confirmPassword')}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
            />
            {errors.confirmPassword && (
              <p className="mt-1 text-xs text-red-600">{errors.confirmPassword.message}</p>
            )}
          </div>

          {registerMutation.isError && (
            <p className="text-sm text-red-600 bg-red-50 rounded-md px-3 py-2">
              {getErrorMessage(registerMutation.error)}
            </p>
          )}

          <button
            type="submit"
            disabled={registerMutation.isPending}
            className="w-full rounded-md bg-blue-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-blue-500 disabled:opacity-60 disabled:cursor-not-allowed transition-colors"
          >
            {registerMutation.isPending ? 'A criar conta...' : 'Criar conta'}
          </button>
        </form>

        <p className="text-center text-sm text-gray-500">
          Já tens conta?{' '}
          <Link href="/login" className="font-medium text-blue-600 hover:text-blue-500">
            Inicia sessão
          </Link>
        </p>
      </div>
    </div>
  )
}
