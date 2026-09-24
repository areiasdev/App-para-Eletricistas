'use client'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useClients } from '@/hooks/useClients'
import { FormField } from '@/components/ui/FormField'
import { PhotoUploader } from '@/components/features/PhotoUploader'
import { isValidPhotoUrl } from '@/lib/photos'

const equipmentSchema = z.object({
  clientId: z.string().min(1, 'Seleciona um cliente.'),
  type: z.string().min(1, 'O tipo é obrigatório.').max(100),
  brand: z.string().max(100).optional().or(z.literal('')),
  model: z.string().max(100).optional().or(z.literal('')),
  serialNumber: z.string().max(100).optional().or(z.literal('')),
  installedAt: z.string().optional(),
  nextMaintenance: z.string().optional(),
  maintenanceIntervalMonths: z.number().int().min(1).max(120).optional(),
  notes: z.string().optional(),
  photos: z
    .array(z.string().refine(isValidPhotoUrl, 'Foto inválida.'))
    .max(20, 'Máximo de 20 fotos por equipamento.')
    .optional(),
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
    watch,
    setValue,
    formState: { errors },
  } = useForm<EquipmentFormValues>({
    resolver: zodResolver(equipmentSchema),
    defaultValues: { photos: [], ...defaultValues },
  })

  const photos = watch('photos') ?? []

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">

      {/* ── Identificação ── */}
      <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <div className="px-5 py-3.5 border-b" style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}>
          <h2 className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-muted)' }}>
            Identificação
          </h2>
        </div>

        <div className="p-5 space-y-4">
          {!lockClient && (
            <FormField label="Cliente *" id="ef-clientId" error={errors.clientId?.message}>
              <select
                id="ef-clientId"
                {...register('clientId')}
                className="form-input"
                style={{ borderColor: errors.clientId ? 'var(--color-danger-300)' : 'var(--color-line-strong)' }}
              >
                <option value="">Selecionar cliente...</option>
                {clientsData?.items.map(c => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </select>
            </FormField>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField label="Tipo *" id="ef-type" error={errors.type?.message}>
              <input
                id="ef-type"
                {...register('type')}
                placeholder="Ex: Ar condicionado, Caldeira, Elevador"
                className="form-input"
                style={{ borderColor: errors.type ? 'var(--color-danger-300)' : 'var(--color-line-strong)' }}
              />
            </FormField>
            <FormField label="Marca" id="ef-brand" error={errors.brand?.message}>
              <input
                id="ef-brand"
                {...register('brand')}
                placeholder="Ex: Daikin, Bosch"
                className="form-input"
                style={{ borderColor: 'var(--color-line-strong)' }}
              />
            </FormField>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField label="Modelo" id="ef-model" error={errors.model?.message}>
              <input
                id="ef-model"
                {...register('model')}
                placeholder="Ex: FTX35K"
                className="form-input"
                style={{ borderColor: 'var(--color-line-strong)' }}
              />
            </FormField>
            <FormField label="Número de série" id="ef-serialNumber" error={errors.serialNumber?.message}>
              <input
                id="ef-serialNumber"
                {...register('serialNumber')}
                placeholder="Ex: SN-12345678"
                className="form-input"
                style={{ borderColor: 'var(--color-line-strong)' }}
              />
            </FormField>
          </div>
        </div>
      </section>

      {/* ── Manutenção ── */}
      <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <div className="px-5 py-3.5 border-b" style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}>
          <h2 className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-muted)' }}>
            Manutenção
          </h2>
        </div>

        <div className="p-5 space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField label="Data de instalação" id="ef-installedAt" error={errors.installedAt?.message}>
              <input
                id="ef-installedAt"
                type="date"
                {...register('installedAt')}
                className="form-input"
                style={{ borderColor: 'var(--color-line-strong)' }}
              />
            </FormField>
            <FormField label="Próxima manutenção" id="ef-nextMaintenance" error={errors.nextMaintenance?.message}>
              <input
                id="ef-nextMaintenance"
                type="date"
                {...register('nextMaintenance')}
                className="form-input"
                style={{ borderColor: 'var(--color-line-strong)' }}
              />
              <p className="text-xs mt-1" style={{ color: 'var(--color-subtle)' }}>
                Receberás um alerta 7 dias antes.
              </p>
            </FormField>

            <FormField label="Periodicidade da manutenção" id="ef-interval" error={errors.maintenanceIntervalMonths?.message}>
              <select
                id="ef-interval"
                {...register('maintenanceIntervalMonths', {
                  setValueAs: (v) => (v === '' || v === undefined || v === null ? undefined : Number(v)),
                })}
                className="form-input"
                style={{ borderColor: 'var(--color-line-strong)' }}
              >
                <option value="">Sem periodicidade</option>
                <option value="1">Mensal</option>
                <option value="3">Trimestral</option>
                <option value="6">Semestral</option>
                <option value="12">Anual</option>
                <option value="24">Bienal</option>
                <option value="60">5 em 5 anos</option>
              </select>
              <p className="text-xs mt-1" style={{ color: 'var(--color-subtle)' }}>
                Ao concluir uma intervenção neste equipamento, a próxima manutenção é marcada automaticamente.
              </p>
            </FormField>
          </div>

          <FormField label="Notas" id="ef-notes" error={errors.notes?.message}>
            <textarea
              id="ef-notes"
              {...register('notes')}
              rows={3}
              placeholder="Observações, histórico de avarias, etc."
              className="form-input resize-none"
              style={{ borderColor: 'var(--color-line-strong)' }}
            />
          </FormField>
        </div>
      </section>

      {/* ── Fotos ── */}
      <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <div className="px-5 py-3.5 border-b" style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}>
          <h2 className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-muted)' }}>
            Fotos
          </h2>
        </div>
        <div className="p-5 space-y-3">
          <PhotoUploader value={photos} onChange={(urls) => setValue('photos', urls, { shouldDirty: true })} />
        </div>
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
    </form>
  )
}
