'use client'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { cn } from '@/lib/utils'
import { useClients } from '@/hooks/useClients'
import { useEquipmentList } from '@/hooks/useEquipment'

const interventionSchema = z.object({
  title: z.string().min(1, 'O título é obrigatório.').max(300),
  description: z.string().max(5000).optional().or(z.literal('')),
  clientId: z.string().min(1, 'Seleciona um cliente.'),
  scheduledAt: z.string().optional(),
  technicianNotes: z.string().max(5000).optional().or(z.literal('')),
  quoteId: z.string().optional(),
  equipmentIds: z.array(z.string()),
})

export type InterventionFormValues = z.infer<typeof interventionSchema>

interface InterventionFormProps {
  defaultValues?: Partial<InterventionFormValues>
  onSubmit: (values: InterventionFormValues) => void
  isLoading?: boolean
  submitLabel?: string
}

export function InterventionForm({
  defaultValues,
  onSubmit,
  isLoading,
  submitLabel = 'Guardar',
}: InterventionFormProps) {
  const { register, handleSubmit, watch, setValue, formState: { errors } } =
    useForm<InterventionFormValues>({
      resolver: zodResolver(interventionSchema),
      defaultValues: { equipmentIds: [], ...defaultValues },
    })

  const clientId = watch('clientId')
  const selectedEquipmentIds = watch('equipmentIds') ?? []

  const { data: clientsData } = useClients({ pageSize: 200 })
  const { data: equipmentData } = useEquipmentList({
    clientId: clientId || undefined,
    pageSize: 100,
  })

  const toggleEquipment = (id: string) => {
    const next = selectedEquipmentIds.includes(id)
      ? selectedEquipmentIds.filter((e) => e !== id)
      : [...selectedEquipmentIds, id]
    setValue('equipmentIds', next)
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
      {/* Main info */}
      <div className="rounded-xl border p-6 space-y-4" style={{ backgroundColor: 'white', borderColor: 'var(--color-line)' }}>
        <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
          Dados da intervenção
        </h2>

        <Field label="Título *" error={errors.title?.message}>
          <input
            {...register('title')}
            placeholder="Ex: Revisão anual ar condicionado"
            className={inputCls(!!errors.title)}
          />
        </Field>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Field label="Cliente *" error={errors.clientId?.message}>
            <select {...register('clientId')} className={inputCls(!!errors.clientId)}>
              <option value="">Selecionar cliente...</option>
              {clientsData?.items.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </Field>

          <Field label="Data agendada" error={errors.scheduledAt?.message}>
            <input type="datetime-local" {...register('scheduledAt')} className={inputCls(false)} />
          </Field>
        </div>

        <Field label="Descrição" error={errors.description?.message}>
          <textarea
            {...register('description')}
            rows={3}
            placeholder="Descrição do trabalho a realizar..."
            className={cn(inputCls(false), 'resize-none')}
          />
        </Field>
      </div>

      {/* Equipment selection */}
      <div className="rounded-xl border p-6 space-y-4" style={{ backgroundColor: 'white', borderColor: 'var(--color-line)' }}>
        <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
          Equipamentos
        </h2>

        {!clientId ? (
          <p className="text-sm" style={{ color: 'var(--color-subtle)' }}>
            Seleciona um cliente para ver os equipamentos disponíveis.
          </p>
        ) : !equipmentData?.items.length ? (
          <p className="text-sm" style={{ color: 'var(--color-subtle)' }}>
            Este cliente não tem equipamentos registados.
          </p>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
            {equipmentData.items.map((eq) => {
              const checked = selectedEquipmentIds.includes(eq.id)
              return (
                <button
                  key={eq.id}
                  type="button"
                  onClick={() => toggleEquipment(eq.id)}
                  className="flex items-center gap-3 rounded-lg border px-4 py-3 text-left transition-all duration-150"
                  style={{
                    borderColor: checked ? 'var(--color-brand-500)' : 'var(--color-line)',
                    backgroundColor: checked ? 'var(--color-brand-50)' : 'var(--color-canvas)',
                  }}
                >
                  <span
                    className="w-4 h-4 rounded border-2 flex items-center justify-center shrink-0 transition-all"
                    style={{
                      borderColor: checked ? 'var(--color-brand-500)' : 'var(--color-line-strong)',
                      backgroundColor: checked ? 'var(--color-brand-500)' : 'transparent',
                    }}
                  >
                    {checked && (
                      <svg width="10" height="8" viewBox="0 0 10 8" fill="none">
                        <path d="M1 4l3 3 5-6" stroke="white" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                      </svg>
                    )}
                  </span>
                  <div className="min-w-0">
                    <p className="text-sm font-medium truncate" style={{ color: 'var(--color-ink)' }}>{eq.type}</p>
                    {(eq.brand || eq.model) && (
                      <p className="text-xs truncate" style={{ color: 'var(--color-muted)' }}>
                        {[eq.brand, eq.model].filter(Boolean).join(' ')}
                      </p>
                    )}
                  </div>
                </button>
              )
            })}
          </div>
        )}
      </div>

      {/* Technician notes */}
      <div className="rounded-xl border p-6 space-y-4" style={{ backgroundColor: 'white', borderColor: 'var(--color-line)' }}>
        <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
          Notas técnicas
        </h2>
        <Field label="Notas do técnico" error={errors.technicianNotes?.message}>
          <textarea
            {...register('technicianNotes')}
            rows={4}
            placeholder="Observações, materiais usados, próximas ações..."
            className={cn(inputCls(false), 'resize-none')}
          />
        </Field>
      </div>

      <button
        type="submit"
        disabled={isLoading}
        className="rounded-lg px-6 py-2.5 text-sm font-semibold transition-all duration-150 disabled:opacity-60 hover:brightness-110 active:scale-[0.99]"
        style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
      >
        {isLoading ? 'A guardar...' : submitLabel}
      </button>
    </form>
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

const inputCls = (hasError: boolean) =>
  cn(
    'w-full rounded-lg border px-3 py-2.5 text-sm transition-all duration-150 outline-none',
    'bg-[var(--color-canvas)] text-[var(--color-ink)]',
    hasError
      ? 'border-red-400 focus:border-red-500 ring-0 focus:ring-2 focus:ring-red-200'
      : 'border-[var(--color-line-strong)] focus:border-[var(--color-brand-500)] focus:ring-2 focus:ring-amber-100'
  )
