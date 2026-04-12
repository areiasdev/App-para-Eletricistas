'use client'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { cn } from '@/lib/utils'
import type { Client } from '@/types'

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
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      {/* Dados principais */}
      <div className="bg-white rounded-lg border border-gray-200 p-6 space-y-4">
        <h2 className="text-sm font-semibold text-gray-900 uppercase tracking-wide">
          Dados do cliente
        </h2>

        <Field label="Nome *" error={errors.name?.message}>
          <input {...register('name')} className={inputCls(!!errors.name)} />
        </Field>

        <div className="grid grid-cols-2 gap-4">
          <Field label="NIF" error={errors.nif?.message}>
            <input {...register('nif')} maxLength={9} placeholder="123456789" className={inputCls(!!errors.nif)} />
          </Field>
          <Field label="Telefone" error={errors.phone?.message}>
            <input {...register('phone')} placeholder="+351 912 345 678" className={inputCls(!!errors.phone)} />
          </Field>
        </div>

        <Field label="Email" error={errors.email?.message}>
          <input type="email" {...register('email')} className={inputCls(!!errors.email)} />
        </Field>

        <Field label="Notas internas" error={errors.notes?.message}>
          <textarea {...register('notes')} rows={3} className={inputCls(false) + ' resize-none'} />
        </Field>
      </div>

      {/* Morada */}
      <div className="bg-white rounded-lg border border-gray-200 p-6 space-y-4">
        <div className="flex items-center gap-3">
          <input type="checkbox" id="hasAddress" {...register('hasAddress')} className="rounded" />
          <label htmlFor="hasAddress" className="text-sm font-semibold text-gray-900 uppercase tracking-wide cursor-pointer">
            Morada
          </label>
        </div>

        {hasAddress && (
          <div className="space-y-4 pt-2">
            <Field label="Rua *" error={errors.address?.street?.message}>
              <input {...register('address.street')} className={inputCls(!!errors.address?.street)} />
            </Field>
            <div className="grid grid-cols-2 gap-4">
              <Field label="Cidade *" error={errors.address?.city?.message}>
                <input {...register('address.city')} className={inputCls(!!errors.address?.city)} />
              </Field>
              <Field label="Código postal *" error={errors.address?.postalCode?.message}>
                <input {...register('address.postalCode')} placeholder="1000-001" className={inputCls(!!errors.address?.postalCode)} />
              </Field>
            </div>
          </div>
        )}
      </div>

      <button
        type="submit"
        disabled={isLoading}
        className="w-full sm:w-auto rounded-md bg-blue-600 px-6 py-2 text-sm font-semibold text-white hover:bg-blue-500 disabled:opacity-60 transition-colors"
      >
        {isLoading ? 'A guardar...' : submitLabel}
      </button>
    </form>
  )
}

function Field({
  label,
  error,
  children,
}: {
  label: string
  error?: string
  children: React.ReactNode
}) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
      {children}
      {error && <p className="mt-1 text-xs text-red-600">{error}</p>}
    </div>
  )
}

const inputCls = (hasError: boolean) =>
  cn(
    'w-full rounded-md border px-3 py-2 text-sm shadow-sm focus:outline-none focus:ring-1',
    hasError
      ? 'border-red-400 focus:border-red-500 focus:ring-red-500'
      : 'border-gray-300 focus:border-blue-500 focus:ring-blue-500'
  )
