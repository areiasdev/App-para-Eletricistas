'use client'

import { useEffect, useRef, useState } from 'react'
import { useRouter } from 'next/navigation'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { useAuthStore } from '@/stores/authStore'
import { useCanManage } from '@/hooks/useCanManage'
import { useProfile, useUpdateProfile, useUploadLogo } from '@/hooks/useProfile'
import { useCreateClient } from '@/hooks/useClients'
import { ClientForm, type ClientFormValues } from '@/components/features/ClientForm'
import { getErrorMessage } from '@/lib/api/client'
import { companyInfoSchema, type CompanyInfoFormValues } from './schema'

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'

// Same default used by the Perfil page (perfil/page.tsx) — a fresh install's brand
// color falls back to this amber until the owner picks their own.
const DEFAULT_BRAND_COLOR = 'var(--color-brand-500)'

const STEPS = [
  { id: 1, label: 'Empresa' },
  { id: 2, label: 'Marca' },
  { id: 3, label: 'Primeiro cliente' },
] as const

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
      {error && <p className="mt-1 text-xs" style={{ color: 'var(--color-danger-600)' }}>{error}</p>}
    </div>
  )
}

function StepIndicator({ current }: { current: 1 | 2 | 3 }) {
  return (
    <div className="flex items-center justify-center gap-3 mb-2">
      {STEPS.map((step) => (
        <div key={step.id} className="flex items-center gap-2">
          <span
            className="w-2.5 h-2.5 rounded-full transition-colors"
            style={{ backgroundColor: step.id <= current ? 'var(--color-brand-500)' : 'var(--color-line-strong)' }}
          />
          <span
            className="text-xs font-medium hidden sm:inline"
            style={{ color: step.id === current ? 'var(--color-ink)' : 'var(--color-subtle)' }}
          >
            {step.label}
          </span>
        </div>
      ))}
    </div>
  )
}

export default function OnboardingPage() {
  const router = useRouter()
  const { user, _hasHydrated } = useAuthStore()
  const canManage = useCanManage()
  const { data: profile } = useProfile()
  const updateProfile = useUpdateProfile()
  const uploadLogo = useUploadLogo()
  const createClient = useCreateClient()

  const [ready, setReady] = useState(false)
  const [step, setStep] = useState<1 | 2 | 3>(1)

  const fileInputRef = useRef<HTMLInputElement>(null)
  const [logoError, setLogoError] = useState<string | null>(null)
  const [brandColorInput, setBrandColorInput] = useState(DEFAULT_BRAND_COLOR)

  useEffect(() => {
    // Wait for Zustand persist to hydrate before checking auth — same pattern as
    // (dashboard)/layout.tsx. Deliberately depends only on _hasHydrated (not on
    // user/companyName) so that completing step 1 — which updates user.companyName
    // in the store — does not re-trigger this "already onboarded" check and bounce
    // the wizard back to /dashboard mid-flow.
    if (!_hasHydrated) return

    if (!user) {
      router.replace('/login')
      return
    }
    if (!canManage) {
      // Technician/Commercial can't act on company setup — never trap them here.
      router.replace('/dashboard')
      return
    }
    if (user.companyName) {
      // Already onboarded — someone navigated back to /onboarding manually.
      router.replace('/dashboard')
      return
    }

    setBrandColorInput(user.brandColor ?? DEFAULT_BRAND_COLOR)
    setReady(true)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [_hasHydrated])

  const {
    register: registerCompany,
    handleSubmit: handleSubmitCompany,
    formState: { errors: companyErrors },
  } = useForm<CompanyInfoFormValues>({
    resolver: zodResolver(companyInfoSchema),
    defaultValues: { companyName: '', nif: '', phone: '' },
  })

  const onSubmitCompanyInfo = (values: CompanyInfoFormValues) => {
    if (!user) return
    updateProfile.mutate(
      {
        fullName: user.fullName,
        companyName: values.companyName,
        nif: values.nif || undefined,
        phone: values.phone || undefined,
        brandColor: user.brandColor ?? DEFAULT_BRAND_COLOR,
      },
      {
        onSuccess: () => setStep(2),
        onError: (err) => toast.error(getErrorMessage(err)),
      }
    )
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

  // Persists the brand color as soon as a fully-formed #rrggbb value is available —
  // fired both by the native color-swatch input (always emits a complete hex) and by
  // the paired hex text input (only once it matches the full pattern, so we don't spam
  // updateProfile with invalid partial hex strings while the user is still typing).
  const commitBrandColor = (color: string) => {
    if (!user || !/^#[0-9a-fA-F]{6}$/.test(color)) return
    updateProfile.mutate(
      {
        fullName: user.fullName,
        companyName: user.companyName ?? profile?.companyName ?? '',
        nif: profile?.nif,
        phone: profile?.phone,
        brandColor: color,
      },
      { onError: (err) => toast.error(getErrorMessage(err)) }
    )
  }

  const handleColorSwatchChange = (color: string) => {
    setBrandColorInput(color)
    commitBrandColor(color)
  }

  const handleColorTextChange = (color: string) => {
    setBrandColorInput(color)
    if (/^#[0-9a-fA-F]{6}$/.test(color)) commitBrandColor(color)
  }

  const handleCreateClient = (values: ClientFormValues) => {
    createClient.mutate(
      {
        name: values.name,
        nif: values.nif || undefined,
        email: values.email || undefined,
        phone: values.phone || undefined,
        notes: values.notes,
        address: values.hasAddress && values.address
          ? {
              street: values.address.street,
              city: values.address.city,
              postalCode: values.address.postalCode,
              country: values.address.country ?? 'Portugal',
            }
          : undefined,
      },
      {
        onSuccess: () => {
          toast.success('Cliente criado.')
          router.push('/dashboard')
        },
        onError: (err) => toast.error(getErrorMessage(err)),
      }
    )
  }

  if (!ready) {
    return (
      <div className="min-h-screen flex items-center justify-center" style={{ backgroundColor: 'var(--color-canvas)' }}>
        <div className="w-5 h-5 rounded-full border-2 animate-spin" style={{ borderColor: 'var(--color-brand-500)', borderTopColor: 'transparent' }} />
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-start justify-center px-6 py-12" style={{ backgroundColor: 'var(--color-canvas)' }}>
      <div className="w-full max-w-2xl animate-fade-up space-y-6">

        <div className="flex items-center gap-2 justify-center">
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

        <div className="text-center">
          <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>
            Vamos configurar o teu BackOffice
          </h1>
          <p className="mt-1 text-sm" style={{ color: 'var(--color-muted)' }}>
            Passo {step} de 3
          </p>
        </div>

        <StepIndicator current={step} />

        {step === 1 && (
          <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
            <div className="px-5 py-3.5 border-b" style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}>
              <h2 className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-muted)' }}>
                Dados da empresa
              </h2>
            </div>
            <form onSubmit={handleSubmitCompany(onSubmitCompanyInfo)} className="p-5 space-y-4">
              <FormField label="Nome da empresa *" error={companyErrors.companyName?.message}>
                <input
                  {...registerCompany('companyName')}
                  placeholder="Ex: Construções Silva Lda."
                  className="form-input"
                  style={{ borderColor: companyErrors.companyName ? 'var(--color-danger-300)' : 'var(--color-line-strong)' }}
                  autoFocus
                />
              </FormField>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <FormField label="NIF" error={companyErrors.nif?.message}>
                  <input
                    {...registerCompany('nif')}
                    maxLength={9}
                    placeholder="123456789"
                    className="form-input"
                    style={{ borderColor: companyErrors.nif ? 'var(--color-danger-300)' : 'var(--color-line-strong)' }}
                  />
                </FormField>
                <FormField label="Telefone" error={companyErrors.phone?.message}>
                  <input
                    {...registerCompany('phone')}
                    placeholder="+351 912 345 678"
                    className="form-input"
                    style={{ borderColor: 'var(--color-line-strong)' }}
                  />
                </FormField>
              </div>

              <div className="flex justify-end pt-2">
                <button
                  type="submit"
                  disabled={updateProfile.isPending}
                  className="rounded-lg px-6 py-2.5 text-sm font-semibold transition-all duration-150 disabled:opacity-60 hover:brightness-110 active:scale-[0.99]"
                  style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
                >
                  {updateProfile.isPending ? 'A guardar...' : 'Continuar'}
                </button>
              </div>
            </form>
          </section>
        )}

        {step === 2 && (
          <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
            <div className="px-5 py-3.5 border-b" style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}>
              <h2 className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-muted)' }}>
                Identidade visual
              </h2>
            </div>
            <div className="p-5 space-y-5">
              <FormField label="Logótipo" hint="PNG, JPEG ou WEBP, até 2MB. Usado nos PDFs de orçamento. Opcional — podes fazer isto mais tarde no Perfil.">
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
                    {logoError && <p className="mt-1.5 text-xs" style={{ color: 'var(--color-danger-600)' }}>{logoError}</p>}
                  </div>
                </div>
              </FormField>

              <FormField label="Cor da marca" hint="Aplica-se à interface e aos PDFs de orçamento.">
                <div className="flex items-center gap-3">
                  <input
                    type="color"
                    value={/^#[0-9a-fA-F]{6}$/.test(brandColorInput) ? brandColorInput : DEFAULT_BRAND_COLOR}
                    onChange={(e) => handleColorSwatchChange(e.target.value)}
                    className="w-11 h-9 rounded-md border cursor-pointer"
                    style={{ borderColor: 'var(--color-line-strong)' }}
                  />
                  <input
                    value={brandColorInput}
                    onChange={(e) => handleColorTextChange(e.target.value)}
                    placeholder="var(--color-brand-500)"
                    className="form-input"
                    style={{ maxWidth: 140, borderColor: 'var(--color-line-strong)' }}
                  />
                </div>
              </FormField>

              <div className="flex justify-between items-center pt-2">
                <button
                  type="button"
                  onClick={() => setStep(3)}
                  className="text-sm font-medium"
                  style={{ color: 'var(--color-muted)' }}
                >
                  Saltar
                </button>
                <button
                  type="button"
                  onClick={() => setStep(3)}
                  className="rounded-lg px-6 py-2.5 text-sm font-semibold transition-all duration-150 hover:brightness-110 active:scale-[0.99]"
                  style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
                >
                  Continuar
                </button>
              </div>
            </div>
          </section>
        )}

        {step === 3 && (
          <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
            <div className="px-5 py-3.5 border-b" style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}>
              <h2 className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-muted)' }}>
                Adiciona o teu primeiro cliente
              </h2>
            </div>
            <div className="p-5 space-y-4">
              <ClientForm
                onSubmit={handleCreateClient}
                isLoading={createClient.isPending}
                submitLabel="Criar cliente e concluir"
              />
              <div className="flex justify-start">
                <button
                  type="button"
                  onClick={() => router.push('/dashboard')}
                  className="text-sm font-medium underline-offset-2 hover:underline"
                  style={{ color: 'var(--color-muted)' }}
                >
                  Saltar por agora
                </button>
              </div>
            </div>
          </section>
        )}
      </div>

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
          box-shadow: 0 0 0 3px color-mix(in srgb, var(--color-brand-500) 12%, transparent);
        }
        .form-input::placeholder {
          color: var(--color-subtle);
        }
      `}</style>
    </div>
  )
}
