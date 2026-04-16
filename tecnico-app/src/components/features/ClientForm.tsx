'use client'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'

const addressSchema = z.object({
  street: z.string().min(1, 'Obrigatório'),
  city: z.string().min(1, 'Obrigatório'),
  postalCode: z.string().regex(/^\d{4}-\d{3}$/, 'Formato: 0000-000'),
  country: z.string().optional(),
})

const clientSchema = z.object({
  name: z.string().min(1, 'O nome é obrigatório.').max(200),
  nif: z
    .string()
    .regex(/^\d{9}$/, 'O NIF deve ter 9 dígitos.')
    .optional()
    .or(z.literal('')),
  email: z.string().email('Email inválido.').optional().or(z.literal('')),
  phone: z.string().max(20).optional().or(z.literal('')),
  notes: z.string().optional(),
  hasAddress: z.boolean(),
  address: addressSchema.optional(),
})

export type ClientFormValues = z.infer<typeof clientSchema>

interface ClientFormProps {
  defaultValues?: Partial<ClientFormValues>
  onSubmit: (values: ClientFormValues) => void
  isLoading?: boolean
  submitLabel?: string
}

export function ClientForm({
  defaultValues,
  onSubmit,
  isLoading,
  submitLabel = 'Guardar',
}: ClientFormProps) {
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<ClientFormValues>({
    resolver: zodResolver(clientSchema),
    defaultValues: { hasAddress: false, ...defaultValues },
  })

  const hasAddress = watch('hasAddress')

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">

      {/* Dados principais */}
      <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <div className="px-5 py-3.5 border-b" style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}>
          <h2 className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-muted)' }}>
            Dados do cliente
          </h2>
        </div>

        <div className="p-5 space-y-4">
          <FormField label="Nome *" id="cf-name" error={errors.name?.message}>
            <input
              id="cf-name"
              {...register('name')}
              placeholder="Ex: João Silva"
              className="form-input"
              style={{ borderColor: errors.name ? '#fca5a5' : 'var(--color-line-strong)' }}
            />
          </FormField>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField label="NIF" id="cf-nif" error={errors.nif?.message}>
              <input
                id="cf-nif"
                {...register('nif')}
                maxLength={9}
                placeholder="123456789"
                className="form-input"
                style={{ borderColor: errors.nif ? '#fca5a5' : 'var(--color-line-strong)' }}
              />
            </FormField>
            <FormField label="Telefone" id="cf-phone" error={errors.phone?.message}>
              <input
                id="cf-phone"
                {...register('phone')}
                placeholder="+351 912 345 678"
                className="form-input"
                style={{ borderColor: 'var(--color-line-strong)' }}
              />
            </FormField>
          </div>

          <FormField label="Email" id="cf-email" error={errors.email?.message}>
            <input
              id="cf-email"
              type="email"
              {...register('email')}
              placeholder="cliente@exemplo.pt"
              className="form-input"
              style={{ borderColor: errors.email ? '#fca5a5' : 'var(--color-line-strong)' }}
            />
          </FormField>

          <FormField label="Notas internas" id="cf-notes" error={errors.notes?.message}>
            <textarea
              id="cf-notes"
              {...register('notes')}
              rows={3}
              placeholder="Observações sobre o cliente..."
              className="form-input resize-none"
              style={{ borderColor: 'var(--color-line-strong)' }}
            />
          </FormField>
        </div>
      </section>

      {/* Morada */}
      <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <div className="px-5 py-3.5 border-b" style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}>
          <label htmlFor="hasAddress" className="flex items-center gap-2.5 cursor-pointer">
            <input
              type="checkbox"
              id="hasAddress"
              {...register('hasAddress')}
              className="w-4 h-4 rounded"
              style={{ accentColor: 'var(--color-brand-500)' }}
            />
            <span className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-muted)' }}>
              Morada
            </span>
          </label>
        </div>

        {hasAddress && (
          <div className="p-5 space-y-4">
            <FormField label="Rua *" id="cf-street" error={errors.address?.street?.message}>
              <input
                id="cf-street"
                {...register('address.street')}
                placeholder="Ex: Rua das Flores, 123"
                className="form-input"
                style={{ borderColor: errors.address?.street ? '#fca5a5' : 'var(--color-line-strong)' }}
              />
            </FormField>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <FormField label="Cidade *" id="cf-city" error={errors.address?.city?.message}>
                <input
                  id="cf-city"
                  {...register('address.city')}
                  placeholder="Ex: Lisboa"
                  className="form-input"
                  style={{ borderColor: errors.address?.city ? '#fca5a5' : 'var(--color-line-strong)' }}
                />
              </FormField>
              <FormField label="Código postal *" id="cf-postal" error={errors.address?.postalCode?.message}>
                <input
                  id="cf-postal"
                  {...register('address.postalCode')}
                  placeholder="1000-001"
                  className="form-input"
                  style={{ borderColor: errors.address?.postalCode ? '#fca5a5' : 'var(--color-line-strong)' }}
                />
              </FormField>
            </div>
          </div>
        )}
      </section>

      <div className="flex justify-end">
        <button
          type="submit"
          disabled={isLoading}
          className="rounded-lg px-6 py-2.5 text-sm font-semibold transition-all duration-150 disabled:opacity-60 hover:brightness-110 active:scale-[0.99]"
          style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
        >
          {isLoading ? 'A guardar...' : submitLabel}
        </button>
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
          box-shadow: 0 0 0 3px rgba(245,158,11,0.12);
        }
        .form-input::placeholder {
          color: var(--color-subtle);
        }
      `}</style>
    </form>
  )
}

function FormField({ label, id, error, children }: { label: string; id?: string; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label htmlFor={id} className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
        {label}
      </label>
      {children}
      {error && <p className="mt-1 text-xs" style={{ color: '#dc2626' }}>{error}</p>}
    </div>
  )
}
