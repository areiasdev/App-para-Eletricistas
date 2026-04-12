'use client'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { cn } from '@/lib/utils'
import { useClients } from '@/hooks/useClients'

const equipmentSchema = z.object({
  clientId: z.string().min(1, 'Seleciona um cliente.'),
  type: z.string().min(1, 'O tipo é obrigatório.').max(100),
  brand: z.string().max(100).optional().or(z.literal('')),
  model: z.string().max(100).optional().or(z.literal('')),
  serialNumber: z.string().max(100).optional().or(z.literal('')),
  installedAt: z.string().optional(),
  nextMaintenance: z.string().optional(),
  notes: z.string().optional(),
})

export type EquipmentFormValues = z.infer<typeof equipmentSchema>

interface EquipmentFormProps {
  defaultValues?: Partial<EquipmentFormValues>
  onSubmit: (values: EquipmentFormValues) => void
  isLoading?: boolean
  submitLabel?: string
  lockClient?: boolean
}

export function EquipmentForm({
  defaultValues,
  onSubmit,
  isLoading,
  submitLabel = 'Guardar',
  lockClient = false,
}: EquipmentFormProps) {
  const { data: clientsData } = useClients({ pageSize: 200 })

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<EquipmentFormValues>({
    resolver: zodResolver(equipmentSchema),
    defaultValues,
  })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      <div className="bg-white rounded-lg border border-gray-200 p-6 space-y-4">
        <h2 className="text-sm font-semibold text-gray-900 uppercase tracking-wide">
          Dados do equipamento
        </h2>

        {!lockClient && (
          <Field label="Cliente *" error={errors.clientId?.message}>
            <select {...register('clientId')} className={inputCls(!!errors.clientId)}>
              <option value="">Selecionar cliente...</option>
              {clientsData?.items.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </Field>
        )}

        <div className="grid grid-cols-2 gap-4">
          <Field label="Tipo *" error={errors.type?.message}>
            <input {...register('type')} placeholder="Ex: Ar condicionado" className={inputCls(!!errors.type)} />
          </Field>
          <Field label="Marca" error={errors.brand?.message}>
            <input {...register('brand')} placeholder="Ex: Daikin" className={inputCls(!!errors.brand)} />
          </Field>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <Field label="Modelo" error={errors.model?.message}>
            <input {...register('model')} className={inputCls(!!errors.model)} />
          </Field>
          <Field label="Número de série" error={errors.serialNumber?.message}>
            <input {...register('serialNumber')} className={inputCls(!!errors.serialNumber)} />
          </Field>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <Field label="Data de instalação" error={errors.installedAt?.message}>
            <input type="date" {...register('installedAt')} className={inputCls(false)} />
          </Field>
          <Field label="Próxima manutenção" error={errors.nextMaintenance?.message}>
            <input type="date" {...register('nextMaintenance')} className={inputCls(false)} />
          </Field>
        </div>

        <Field label="Notas" error={errors.notes?.message}>
          <textarea {...register('notes')} rows={3} className={inputCls(false) + ' resize-none'} />
        </Field>
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

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
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
