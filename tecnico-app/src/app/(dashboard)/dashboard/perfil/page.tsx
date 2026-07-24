'use client'

import { useEffect, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import { useProfile, useUpdateProfile, useUploadLogo } from '@/hooks/useProfile'
import { useCanManage } from '@/hooks/useCanManage'
import { getErrorMessage } from '@/lib/api/client'
import { validateNif } from '@/lib/utils/formatters'

const API_BASE = (process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000')

const DEFAULT_BRAND_COLOR = '#f59e0b'

const profileSchema = z.object({
  fullName: z.string().min(1, 'O nome é obrigatório.').max(200),
  companyName: z.string().max(200).optional().or(z.literal('')),
  nif: z.string().regex(/^\d{9}$/, 'O NIF deve ter 9 dígitos.').refine((v) => validateNif(v), 'NIF inválido.').optional().or(z.literal('')),
  phone: z.string().max(20).optional().or(z.literal('')),
  brandColor: z.string().regex(/^#[0-9a-fA-F]{6}$/, 'Cor inválida.'),
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
  const canManage = useCanManage()
  const { data: profile, isLoading } = useProfile()
  const updateProfile = useUpdateProfile()
  const uploadLogo = useUploadLogo()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [logoError, setLogoError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors, isDirty },
  } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: { fullName: '', companyName: '', nif: '', phone: '', brandColor: DEFAULT_BRAND_COLOR },
  })

  const brandColor = watch('brandColor')

  useEffect(() => {
    if (profile) {
      reset({
        fullName: profile.fullName,
        companyName: profile.companyName ?? '',
        nif: profile.nif ?? '',
        phone: profile.phone ?? '',
        brandColor: profile.brandColor ?? DEFAULT_BRAND_COLOR,
      })
    }
  }, [profile, reset])

  const onSubmit = (values: ProfileFormValues) => {
    updateProfile.mutate(values, {
      onSuccess: () => toast.success('Perfil atualizado.'),
      onError: (err) => toast.error(getErrorMessage(err)),
    })
  }

  const handleLogoSelect = (file: File | undefined) => {
    setLogoError(null)
    if (!file) return

    if (!['image/png', 'image/jpeg', 'image/webp'].includes(file.type)) {
      setLogoError('O logótipo deve ser uma imagem PNG, JPEG ou WEBP.')
      return
    }
    if (file.size > 2 * 1024 * 1024) {
      setLogoError('O logótipo não pode exceder 2MB.')
      return
    }

    uploadLogo.mutate(file, {
      onSuccess: () => toast.success('Logótipo atualizado.'),
      onError: (err) => setLogoError(getErrorMessage(err)),
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

          {!canManage ? (
            <div className="p-5">
              <p className="text-sm" style={{ color: 'var(--color-muted)' }}>
                Só o proprietário ou administradores podem alterar os dados da empresa.
              </p>
            </div>
          ) : (
            <div className="p-5 space-y-5">
              {/* Logo */}
              <FormField label="Logótipo" hint="PNG, JPEG ou WEBP, até 2MB. Usado nos PDFs de orçamento.">
                <div className="flex items-center gap-4">
                  <div
                    className="w-16 h-16 rounded-lg border flex items-center justify-center overflow-hidden shrink-0"
                    style={{ borderColor: 'var(--color-line-strong)', backgroundColor: 'var(--color-canvas)' }}
                  >
                    {profile?.logoUrl ? (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img src={`${API_BASE}${profile.logoUrl}?v=${Date.now()}`} alt="Logótipo" className="w-full h-full object-contain" />
                    ) : (
                      <span className="text-xs" style={{ color: 'var(--color-subtle)' }}>Sem logo</span>
                    )}
                  </div>
                  <div>
                    <input
                      ref={fileInputRef}
                      type="file"
                      accept="image/png,image/jpeg,image/webp"
                      className="hidden"
                      onChange={(e) => handleLogoSelect(e.target.files?.[0])}
                    />
                    <button
                      type="button"
                      onClick={() => fileInputRef.current?.click()}
                      disabled={uploadLogo.isPending}
                      className="rounded-lg border px-4 py-2 text-sm font-medium transition-all duration-150 disabled:opacity-60"
                      style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
                    >
                      {uploadLogo.isPending ? 'A enviar...' : 'Carregar logótipo'}
                    </button>
                    {logoError && <p className="mt-1.5 text-xs" style={{ color: '#dc2626' }}>{logoError}</p>}
                  </div>
                </div>
              </FormField>

              {/* Brand color */}
              <FormField label="Cor da marca" error={errors.brandColor?.message} hint="Aplica-se à interface e aos PDFs de orçamento.">
                <div className="flex items-center gap-3">
                  <input
                    type="color"
                    value={brandColor}
                    onChange={(e) => setValue('brandColor', e.target.value, { shouldDirty: true })}
                    className="w-11 h-9 rounded-md border cursor-pointer"
                    style={{ borderColor: 'var(--color-line-strong)' }}
                  />
                  <input
                    {...register('brandColor')}
                    placeholder="#f59e0b"
                    className="form-input"
                    style={{ maxWidth: 140, borderColor: errors.brandColor ? '#fca5a5' : 'var(--color-line-strong)' }}
                  />
                </div>
              </FormField>

              <FormField label="Nome da empresa" error={errors.companyName?.message}>
                <input
                  {...register('companyName')}
                  placeholder="Ex: Construções Silva Lda."
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
            </div>
          )}
        </section>

        {/* Always visible — a technician can still rename themselves even though the
            company section above is read-only for them; the backend applies the same split. */}
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
