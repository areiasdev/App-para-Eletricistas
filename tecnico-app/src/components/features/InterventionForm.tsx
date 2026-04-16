'use client'

import { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { cn } from '@/lib/utils'
import { useClients } from '@/hooks/useClients'
import { useEquipmentList } from '@/hooks/useEquipment'
import type { InterventionMaterial } from '@/types'

const interventionSchema = z.object({
  title: z.string().min(1, 'O título é obrigatório.').max(300),
  description: z.string().max(5000).optional().or(z.literal('')),
  clientId: z.string().min(1, 'Seleciona um cliente.'),
  scheduledAt: z.string().optional(),
  technicianNotes: z.string().max(5000).optional().or(z.literal('')),
  quoteId: z.string().optional(),
  equipmentIds: z.array(z.string()),
  photos: z.array(z.string().url('URL inválido')).optional(),
})

export type InterventionFormValues = z.infer<typeof interventionSchema>

interface InterventionFormProps {
  defaultValues?: Partial<InterventionFormValues>
  defaultMaterials?: InterventionMaterial[]
  onSubmit: (values: InterventionFormValues, materials: InterventionMaterial[]) => void
  isLoading?: boolean
  submitLabel?: string
}

export function InterventionForm({
  defaultValues,
  defaultMaterials = [],
  onSubmit,
  isLoading,
  submitLabel = 'Guardar',
}: InterventionFormProps) {
  const { register, handleSubmit, watch, setValue, formState: { errors } } =
    useForm<InterventionFormValues>({
      resolver: zodResolver(interventionSchema),
      defaultValues: { equipmentIds: [], photos: [], ...defaultValues },
    })

  const clientId = watch('clientId')
  const selectedEquipmentIds = watch('equipmentIds') ?? []
  const photos = watch('photos') ?? []
  const [photoInput, setPhotoInput] = useState('')

  // Materials state — managed outside RHF (complex nested object)
  const [materials, setMaterials] = useState<InterventionMaterial[]>(defaultMaterials)
  const [matName, setMatName] = useState('')
  const [matQty, setMatQty] = useState('1')
  const [matCost, setMatCost] = useState('')

  const addPhoto = () => {
    const url = photoInput.trim()
    if (!url) return
    setValue('photos', [...photos, url])
    setPhotoInput('')
  }

  const removePhoto = (i: number) => {
    setValue('photos', photos.filter((_, idx) => idx !== i))
  }

  const addMaterial = () => {
    const name = matName.trim()
    const qty = parseFloat(matQty)
    const cost = parseFloat(matCost)
    if (!name || isNaN(qty) || qty <= 0 || isNaN(cost) || cost < 0) return
    setMaterials(prev => [...prev, { name, quantity: qty, unitCost: cost }])
    setMatName('')
    setMatQty('1')
    setMatCost('')
  }

  const removeMaterial = (i: number) => {
    setMaterials(prev => prev.filter((_, idx) => idx !== i))
  }

  const { data: clientsData } = useClients({ pageSize: 200 })

  // When editing, clientsData loads after mount — re-apply the default clientId so
  // the uncontrolled select picks up the correct option once the options are in the DOM.
  useEffect(() => {
    if (defaultValues?.clientId) {
      setValue('clientId', defaultValues.clientId, { shouldValidate: false })
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [clientsData])

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

  const materialsCost = materials.reduce((sum, m) => sum + m.quantity * m.unitCost, 0)

  return (
    <form onSubmit={handleSubmit((v) => onSubmit(v, materials))} className="space-y-5">
      {/* Main info */}
      <div className="rounded-xl border p-6 space-y-4" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
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
      <div className="rounded-xl border p-6 space-y-4" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
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

      {/* Materials */}
      <div className="rounded-xl border p-6 space-y-4" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <div className="flex items-center justify-between">
          <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
            Materiais utilizados
          </h2>
          {materials.length > 0 && (
            <span className="text-xs font-semibold font-mono" style={{ color: 'var(--color-brand-500)' }}>
              Total: {materialsCost.toFixed(2)} €
            </span>
          )}
        </div>

        <div className="grid grid-cols-12 gap-2">
          <input
            type="text"
            value={matName}
            onChange={e => setMatName(e.target.value)}
            placeholder="Descrição (ex: Cabo elétrico 2.5mm²)"
            className={cn(inputCls(false), 'col-span-12 sm:col-span-6')}
          />
          <input
            type="number"
            value={matQty}
            onChange={e => setMatQty(e.target.value)}
            placeholder="Qtd."
            min="0.01"
            step="0.01"
            className={cn(inputCls(false), 'col-span-5 sm:col-span-2')}
          />
          <input
            type="number"
            value={matCost}
            onChange={e => setMatCost(e.target.value)}
            placeholder="€/un."
            min="0"
            step="0.01"
            className={cn(inputCls(false), 'col-span-5 sm:col-span-2')}
          />
          <button
            type="button"
            onClick={addMaterial}
            className="col-span-2 rounded-lg text-sm font-medium transition-all duration-150"
            style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
          >
            +
          </button>
        </div>

        {materials.length > 0 && (
          <div className="rounded-lg border overflow-hidden" style={{ borderColor: 'var(--color-line)' }}>
            <table className="w-full text-sm">
              <thead>
                <tr style={{ backgroundColor: 'var(--color-canvas)', borderBottom: '1px solid var(--color-line)' }}>
                  <th className="text-left px-3 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>Material</th>
                  <th className="text-right px-3 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>Qtd.</th>
                  <th className="text-right px-3 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>€/un.</th>
                  <th className="text-right px-3 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>Total</th>
                  <th className="px-3 py-2 w-8" />
                </tr>
              </thead>
              <tbody>
                {materials.map((m, i) => (
                  <tr key={i} style={{ borderTop: i > 0 ? '1px solid var(--color-line)' : undefined }}>
                    <td className="px-3 py-2" style={{ color: 'var(--color-ink)' }}>{m.name}</td>
                    <td className="px-3 py-2 text-right font-mono text-xs" style={{ color: 'var(--color-muted)' }}>{m.quantity}</td>
                    <td className="px-3 py-2 text-right font-mono text-xs" style={{ color: 'var(--color-muted)' }}>{m.unitCost.toFixed(2)}</td>
                    <td className="px-3 py-2 text-right font-mono text-xs font-semibold" style={{ color: 'var(--color-ink)' }}>
                      {(m.quantity * m.unitCost).toFixed(2)} €
                    </td>
                    <td className="px-3 py-2 text-center">
                      <button
                        type="button"
                        onClick={() => removeMaterial(i)}
                        className="text-xs rounded px-1 transition-colors duration-150"
                        style={{ color: '#dc2626' }}
                      >
                        ×
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Technician notes */}
      <div className="rounded-xl border p-6 space-y-4" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
          Notas técnicas
        </h2>
        <Field label="Notas do técnico" error={errors.technicianNotes?.message}>
          <textarea
            {...register('technicianNotes')}
            rows={4}
            placeholder="Observações, próximas ações..."
            className={cn(inputCls(false), 'resize-none')}
          />
        </Field>
      </div>

      {/* Photos */}
      <div className="rounded-xl border p-6 space-y-4" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
          Fotos
        </h2>
        <p className="text-xs" style={{ color: 'var(--color-subtle)' }}>
          Adiciona URLs de fotos (ex: Google Drive, Dropbox, Imgur).
        </p>
        <div className="flex gap-2">
          <input
            type="url"
            value={photoInput}
            onChange={e => setPhotoInput(e.target.value)}
            onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); addPhoto() } }}
            placeholder="https://exemplo.com/foto.jpg"
            className={cn(inputCls(false), 'flex-1')}
          />
          <button
            type="button"
            onClick={addPhoto}
            className="rounded-lg px-4 py-2 text-sm font-medium transition-all duration-150"
            style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
          >
            Adicionar
          </button>
        </div>
        {photos.length > 0 && (
          <ul className="space-y-2">
            {photos.map((url, i) => (
              <li key={i} className="flex items-center gap-2 rounded-lg border px-3 py-2"
                style={{ borderColor: 'var(--color-line)', backgroundColor: 'var(--color-canvas)' }}>
                <span className="text-xs truncate flex-1" style={{ color: 'var(--color-ink)' }}>{url}</span>
                <button
                  type="button"
                  onClick={() => removePhoto(i)}
                  className="shrink-0 text-xs px-2 py-0.5 rounded transition-colors duration-150"
                  style={{ color: '#dc2626' }}
                >
                  Remover
                </button>
              </li>
            ))}
          </ul>
        )}
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
