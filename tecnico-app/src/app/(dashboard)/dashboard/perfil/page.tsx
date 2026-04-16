'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import { useProfile, useUpdateProfile } from '@/hooks/useProfile'
import { getErrorMessage } from '@/lib/api/client'

const profileSchema = z.object({
  fullName: z.string().min(1, 'O nome é obrigatório.').max(200),
  companyName: z.string().max(200).optional().or(z.literal('')),
  nif: z.string().regex(/^\d{9}$/, 'O NIF deve ter 9 dígitos.').optional().or(z.literal('')),
  phone: z.string().max(20).optional().or(z.literal('')),
  logoUrl: z.string().url('URL inválido.').optional().or(z.literal('')),
})

type ProfileFormValues = z.infer<typeof profileSchema>

function FormField({ label, hint, error, children }: {
  label: string
  hint?: string
  error?: string
  children: React.ReactNode
}) {
  return (
    <div>
      <label className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
        {label}
      </label>
      {children}
      {hint && !error && <p className="mt-1 text-xs" style={{ color: 'var(--color-subtle)' }}>{hint}</p>}
      {error && <p className="mt-1 text-xs" style={{ color: '#dc2626' }}>{error}</p>}
    </div>
  )
}

export default function PerfilPage() {
  const { data: profile, isLoading } = useProfile()
  const updateProfile = useUpdateProfile()

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: { fullName: '', companyName: '', nif: '', phone: '', logoUrl: '' },
  })

  useEffect(() => {
    if (profile) {
      reset({
        fullName: profile.fullName,
        companyName: profile.companyName ?? '',
        nif: profile.nif ?? '',
        phone: profile.phone ?? '',
        logoUrl: profile.logoUrl ?? '',
      })
    }
  }, [profile, reset])

  const onSubmit = (values: ProfileFormValues) => {
    updateProfile.mutate(values, {
      onSuccess: () => toast.success('Perfil atualizado.'),
      onError: (err) => toast.error(getErrorMessage(err)),
    })
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-8 w-40 rounded animate-pulse" style={{ backgroundColor: 'var(--color-line)' }} />
        <div className="h-64 rounded-xl animate-pulse" style={{ backgroundColor: 'var(--color-line)' }} />
      </div>
    )
  }

  return (
    <div className="space-y-6 max-w-2xl">
      <div>
        <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Perfil</h1>
        <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
          Informação da tua conta e empresa
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">

        {/* Conta */}
        <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
          <div className="px-5 py-3.5 border-b" style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}>
            <h2 className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-muted)' }}>
              Conta
            </h2>
          </div>
          <div className="p-5 space-y-4">
            <FormField label="Nome completo *" error={errors.fullName?.message}>
              <input
                {...register('fullName')}
                placeholder="Ex: João Silva"
                className="form-input"
                style={{ borderColor: errors.fullName ? '#fca5a5' : 'var(--color-line-strong)' }}
              />
            </FormField>

            <FormField label="Email">
              <input
                type="email"
                value={profile?.email ?? ''}
                disabled
                className="form-input"
                style={{ borderColor: 'var(--color-line)', opacity: 0.6, cursor: 'not-allowed' }}
              />
              <p className="mt-1 text-xs" style={{ color: 'var(--color-subtle)' }}>
                O email não pode ser alterado aqui.
              </p>
            </FormField>
          </div>
        </section>

        {/* Empresa */}
        <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
          <div className="px-5 py-3.5 border-b" style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}>
            <h2 className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-muted)' }}>
              Empresa
            </h2>
          </div>
          <div className="p-5 space-y-4">
            <FormField label="Nome da empresa" error={errors.companyName?.message}>
              <input
                {...register('companyName')}
                placeholder="Ex: Eletricidade Silva Lda."
                className="form-input"
                style={{ borderColor: 'var(--color-line-strong)' }}
              />
            </FormField>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <FormField label="NIF" error={errors.nif?.message}>
                <input
                  {...register('nif')}
                  maxLength={9}
                  placeholder="123456789"
                  className="form-input"
                  style={{ borderColor: errors.nif ? '#fca5a5' : 'var(--color-line-strong)' }}
                />
              </FormField>
              <FormField label="Telefone" error={errors.phone?.message}>
                <input
                  {...register('phone')}
                  placeholder="+351 912 345 678"
                  className="form-input"
                  style={{ borderColor: 'var(--color-line-strong)' }}
                />
              </FormField>
            </div>

            <FormField
              label="URL do logótipo"
              error={errors.logoUrl?.message}
              hint="Usado nos PDFs de orçamento. Deve ser uma URL pública de imagem."
            >
              <input
                {...register('logoUrl')}
                type="url"
                placeholder="https://..."
                className="form-input"
                style={{ borderColor: errors.logoUrl ? '#fca5a5' : 'var(--color-line-strong)' }}
              />
            </FormField>
          </div>
        </section>

        {/* Plano */}
        <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
          <div className="px-5 py-3.5 border-b" style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}>
            <h2 className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-muted)' }}>
              Plano
            </h2>
          </div>
          <div className="p-5 flex items-center justify-between">
            <div>
              <p className="text-sm font-semibold" style={{ color: 'var(--color-ink)' }}>
                {profile?.plan === 'Free' ? 'Plano Free' : `Plano ${profile?.plan}`}
              </p>
              {profile?.plan === 'Free' && (
                <p className="text-xs mt-0.5" style={{ color: 'var(--color-muted)' }}>
                  Faz upgrade para aceder a PDF, histórico ilimitado e mais.
                </p>
              )}
            </div>
            {profile?.plan === 'Free' && (
              <a
                href="/dashboard/planos"
                className="rounded-lg px-4 py-2 text-sm font-semibold transition-all duration-150 hover:brightness-110 active:scale-[0.99]"
                style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
              >
                Fazer upgrade
              </a>
            )}
          </div>
        </section>

        <div className="flex justify-end">
          <button
            type="submit"
            disabled={updateProfile.isPending || !isDirty}
            className="rounded-lg px-6 py-2.5 text-sm font-semibold transition-all duration-150 disabled:opacity-60 hover:brightness-110 active:scale-[0.99]"
            style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
          >
            {updateProfile.isPending ? 'A guardar...' : 'Guardar alterações'}
          </button>
        </div>
      </form>

      <style>{`
        .form-input {
          width: 100%;
          border-radius: 0.5rem;
          border: 1.5px solid var(--color-line-strong);
          padding: 0.5rem 0.75rem;
          font-size: 0.875rem;
          background-color: var(--color-card);
          color: var(--color-ink);
          outline: none;
          transition: border-color 0.15s, box-shadow 0.15s;
          font-family: var(--font-outfit), system-ui, sans-serif;
        }
        .form-input:focus {
          border-color: var(--color-brand-500);
          box-shadow: 0 0 0 3px rgba(245,158,11,0.12);
        }
        .form-input::placeholder {
          color: var(--color-subtle);
        }
      `}</style>
    </div>
  )
}
