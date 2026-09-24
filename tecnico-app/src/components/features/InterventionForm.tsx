'use client'

import { useState, useEffect, useRef } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { cn } from '@/lib/utils'
import { formatCurrency, formatUnitPrice } from '@/lib/utils/formatters'
import { PhotoUploader } from '@/components/features/PhotoUploader'
import { isValidPhotoUrl } from '@/lib/photos'
import { useClients } from '@/hooks/useClients'
import { useEquipmentList } from '@/hooks/useEquipment'
import { useQuotes } from '@/hooks/useQuotes'
import { useTeam } from '@/hooks/useTeam'
import type { InterventionMaterial } from '@/types'

const interventionSchema = z.object({
  title: z.string().min(1, 'O título é obrigatório.').max(300),
  description: z.string().max(5000).optional().or(z.literal('')),
  clientId: z.string().min(1, 'Seleciona um cliente.'),
  scheduledAt: z.string().optional(),
  technicianNotes: z.string().max(5000).optional().or(z.literal('')),
  quoteId: z.string().optional(),
  equipmentIds: z.array(z.string()),
  photos: z
    .array(z.string().refine(isValidPhotoUrl, 'Foto inválida.'))
    .max(20, 'Máximo de 20 fotos por intervenção.')
    .refine((urls) => new Set(urls).size === urls.length, 'Não são permitidas fotos duplicadas.')
    .optional(),
  assignedToUserId: z.string().optional(),
  laborHours: z.number().min(0).max(1000).optional(),
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

  // Materials state — managed outside RHF (complex nested object)
  const [materials, setMaterials] = useState<InterventionMaterial[]>(defaultMaterials)
  const [matName, setMatName] = useState('')
  const [matQty, setMatQty] = useState('1')
  const [matCost, setMatCost] = useState('')
  const [matPrice, setMatPrice] = useState('')

  const addMaterial = () => {
    const name = matName.trim()
    // Clamp to the precision the backend stores, to avoid float noise (0.1 + 0.2…)
    const qty = Math.round(parseFloat(matQty) * 1000) / 1000
    const cost = Math.round(parseFloat(matCost) * 10000) / 10000
    const price = matPrice.trim() === '' ? null : Math.round(parseFloat(matPrice) * 10000) / 10000
    if (!name || isNaN(qty) || qty <= 0 || isNaN(cost) || cost < 0) return
    if (price !== null && (isNaN(price) || price < 0)) return
    setMaterials(prev => [...prev, { name, quantity: qty, unitCost: cost, unitPrice: price }])
    setMatName('')
    setMatQty('1')
    setMatCost('')
    setMatPrice('')
  }

  const removeMaterial = (i: number) => {
    setMaterials(prev => prev.filter((_, idx) => idx !== i))
  }

  const { data: clientsData } = useClients({ pageSize: 200 })
  const { data: teamMembers = [] } = useTeam()

  // When editing, clientsData loads after mount — re-apply the default clientId once so
  // the uncontrolled select picks up the correct option once the options are in the DOM.
  // Guarded to run only once: without the ref, this fired on every clientsData refetch
  // (window refocus, unrelated cache invalidation), silently reverting an in-progress edit.
  const appliedDefaultClientRef = useRef(false)
  useEffect(() => {
    if (appliedDefaultClientRef.current) return
    if (defaultValues?.clientId && clientsData) {
      setValue('clientId', defaultValues.clientId, { shouldValidate: false })
      appliedDefaultClientRef.current = true
    }
  }, [clientsData, defaultValues?.clientId, setValue])

  // Clear equipment selection when the client actually changes (not on the initial
  // defaultValues application above) — otherwise switching clients can leave another
  // client's equipment IDs in the submitted payload.
  const previousClientIdRef = useRef(clientId)
  useEffect(() => {
    if (previousClientIdRef.current !== clientId) {
      setValue('equipmentIds', [])
      setValue('quoteId', '')
      previousClientIdRef.current = clientId
    }
  }, [clientId, setValue])

  const { data: equipmentData } = useEquipmentList({
    clientId: clientId || undefined,
    pageSize: 100,
  })

  const { data: quotesData } = useQuotes({
    clientId: clientId || undefined,
    pageSize: 100,
  })

  // Same async-options problem as clientId above: quoteId's <select> options depend on
  // quotesData (loaded after mount), so the uncontrolled defaultValue can't select it yet.
  const appliedDefaultQuoteRef = useRef(false)
  useEffect(() => {
    if (appliedDefaultQuoteRef.current) return
    if (defaultValues?.quoteId && quotesData) {
      setValue('quoteId', defaultValues.quoteId, { shouldValidate: false })
      appliedDefaultQuoteRef.current = true
    }
  }, [quotesData, defaultValues?.quoteId, setValue])

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

        <Field label="Título *" id="if-title" error={errors.title?.message}>
          <input
            id="if-title"
            {...register('title')}
            placeholder="Ex: Revisão anual ar condicionado"
            className={inputCls(!!errors.title)}
          />
        </Field>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Field label="Cliente *" id="if-clientId" error={errors.clientId?.message}>
            <select id="if-clientId" {...register('clientId')} className={inputCls(!!errors.clientId)}>
              <option value="">Selecionar cliente...</option>
              {clientsData?.items.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </Field>

          <Field label="Data agendada" id="if-scheduledAt" error={errors.scheduledAt?.message}>
            <input id="if-scheduledAt" type="datetime-local" {...register('scheduledAt')} className={inputCls(false)} />
          </Field>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          {teamMembers.length > 0 && (
            <Field label="Atribuir a" id="if-assignedToUserId" error={undefined}>
              <select id="if-assignedToUserId" {...register('assignedToUserId')} className={inputCls(false)}>
                <option value="">— Não atribuído —</option>
                {teamMembers.map((m) => (
                  <option key={m.memberId} value={m.memberId}>
                    {m.fullName || m.email}
                  </option>
                ))}
              </select>
            </Field>
          )}

          <Field label="Orçamento associado" id="if-quoteId" error={undefined}>
            <select id="if-quoteId" {...register('quoteId')} className={inputCls(false)} disabled={!clientId}>
              <option value="">— Sem orçamento —</option>
              {quotesData?.items.map((q) => (
                <option key={q.id} value={q.id}>
                  {q.number} · {formatCurrency(q.total)}
                </option>
              ))}
            </select>
            {!clientId && (
              <p className="mt-1.5 text-xs" style={{ color: 'var(--color-subtle)' }}>
                Seleciona um cliente para ver os seus orçamentos.
              </p>
            )}
          </Field>
        </div>

        <Field label="Descrição" id="if-description" error={errors.description?.message}>
          <textarea
            id="if-description"
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
                  className="flex items-center gap-3 rounded-lg border px-4 py-3 text-left transition-colors duration-100"
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
            <span className="text-xs font-semibold font-mono" style={{ color: 'var(--color-brand-text)' }}>
              Total: {formatCurrency(materialsCost)}
            </span>
          )}
        </div>

        <div className="grid grid-cols-12 gap-2">
          <input
            type="text"
            aria-label="Descrição do material"
            value={matName}
            onChange={e => setMatName(e.target.value)}
            placeholder="Descrição (ex: Tubo PVC 32mm)"
            className={cn(inputCls(false), 'col-span-12 sm:col-span-4')}
          />
          <input
            type="number"
            aria-label="Quantidade"
            value={matQty}
            onChange={e => setMatQty(e.target.value)}
            placeholder="Qtd."
            inputMode="decimal"
            min="0.001"
            step="0.001"
            className={cn(inputCls(false), 'col-span-4 sm:col-span-2')}
          />
          <input
            type="number"
            aria-label="Custo por unidade"
            value={matCost}
            onChange={e => setMatCost(e.target.value)}
            placeholder="Custo €/un."
            inputMode="decimal"
            min="0"
            step="0.0001"
            className={cn(inputCls(false), 'col-span-4 sm:col-span-2')}
          />
          <input
            type="number"
            aria-label="Preço de venda por unidade"
            title="Preço cobrado ao cliente ao faturar a intervenção (se vazio, usa o custo)"
            value={matPrice}
            onChange={e => setMatPrice(e.target.value)}
            placeholder="Venda €/un."
            inputMode="decimal"
            min="0"
            step="0.0001"
            className={cn(inputCls(false), 'col-span-4 sm:col-span-2')}
          />
          <button
            type="button"
            onClick={addMaterial}
            aria-label="Adicionar material"
            className="col-span-12 sm:col-span-2 rounded-lg py-2 text-sm font-medium transition-colors duration-100"
            style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-on-brand)' }}
          >
            + Adicionar
          </button>
        </div>

        {materials.length > 0 && (
          <div className="rounded-lg border overflow-hidden" style={{ borderColor: 'var(--color-line)' }}>
            <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr style={{ backgroundColor: 'var(--color-canvas)', borderBottom: '1px solid var(--color-line)' }}>
                  <th className="text-left px-3 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>Material</th>
                  <th className="text-right px-3 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>Qtd.</th>
                  <th className="text-right px-3 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>Custo/un.</th>
                  <th className="text-right px-3 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>Venda/un.</th>
                  <th className="text-right px-3 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>Total</th>
                  <th className="px-3 py-2 w-8" />
                </tr>
              </thead>
              <tbody>
                {materials.map((m, i) => (
                  <tr key={i} style={{ borderTop: i > 0 ? '1px solid var(--color-line)' : undefined }}>
                    <td className="px-3 py-2" style={{ color: 'var(--color-ink)' }}>{m.name}</td>
                    <td className="px-3 py-2 text-right font-mono text-xs" style={{ color: 'var(--color-muted)' }}>{m.quantity}</td>
                    <td className="px-3 py-2 text-right font-mono text-xs" style={{ color: 'var(--color-muted)' }}>{formatUnitPrice(m.unitCost)}</td>
                    <td className="px-3 py-2 text-right font-mono text-xs" style={{ color: 'var(--color-muted)' }}>{m.unitPrice != null ? formatUnitPrice(m.unitPrice) : '—'}</td>
                    <td className="px-3 py-2 text-right font-mono text-xs font-semibold" style={{ color: 'var(--color-ink)' }}>
                      {formatCurrency(m.quantity * m.unitCost)}
                    </td>
                    <td className="px-3 py-2 text-center">
                      <button
                        type="button"
                        onClick={() => removeMaterial(i)}
                        className="text-xs rounded px-1 transition-colors duration-150"
                        style={{ color: 'var(--color-danger-600)' }}
                      >
                        ×
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            </div>
          </div>
        )}
      </div>

      {/* Technician notes */}
      <div className="rounded-xl border p-6 space-y-4" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
          Trabalho realizado
        </h2>
        <Field label="Horas de trabalho" id="if-laborHours" error={errors.laborHours?.message}>
          <input
            id="if-laborHours"
            type="number"
            inputMode="decimal"
            step="0.25"
            min="0"
            placeholder="Ex.: 2,5"
            {...register('laborHours', {
              setValueAs: (v) => (v === '' || v === null || v === undefined ? undefined : Number(v)),
            })}
            className={cn(inputCls(!!errors.laborHours), 'max-w-40')}
          />
          <p className="mt-1.5 text-xs" style={{ color: 'var(--color-subtle)' }}>
            Faturadas ao preço/hora definido no Perfil.
          </p>
        </Field>

        <Field label="Notas do técnico" id="if-technicianNotes" error={errors.technicianNotes?.message}>
          <textarea
            id="if-technicianNotes"
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
        <PhotoUploader value={photos} onChange={(urls) => setValue('photos', urls, { shouldDirty: true })} />
      </div>

      <button
        type="submit"
        disabled={isLoading}
        className="rounded-lg px-6 py-2.5 text-sm font-semibold transition-colors duration-100 disabled:opacity-60"
        style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-on-brand)' }}
      >
        {isLoading ? 'A guardar...' : submitLabel}
      </button>
    </form>
  )
}

function Field({ label, id, error, children }: { label: string; id?: string; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label htmlFor={id} className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
        {label}
      </label>
      {children}
      {error && <p className="mt-1.5 text-xs" style={{ color: 'var(--color-danger-600)' }}>{error}</p>}
    </div>
  )
}

const inputCls = (hasError: boolean) =>
  cn(
    'w-full rounded-lg border px-3 py-2.5 text-sm transition-colors duration-100 outline-none',
    'bg-[var(--color-canvas)] text-[var(--color-ink)]',
    hasError
      ? 'border-red-400 focus:border-red-500 ring-0 focus:ring-2 focus:ring-red-200'
      : 'border-[var(--color-line-strong)] focus:border-[var(--color-brand-500)] focus:ring-2 focus:ring-amber-100'
  )
