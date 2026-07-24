'use client'

import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { formatCurrency } from '@/lib/utils/formatters'
import { useClients } from '@/hooks/useClients'

const lineSchema = z.object({
  description: z.string().min(1, 'Obrigatório').max(500),
  quantity: z.number().positive('> 0'),
  unitPrice: z.number().min(0, '>= 0'),
  vatRate: z.number().min(0).max(100),
})

const quoteSchema = z.object({
  clientId: z.string().min(1, 'Seleciona um cliente.'),
  discount: z.number().min(0).optional(),
  notes: z.string().optional(),
  validUntil: z.string().optional(),
  lines: z.array(lineSchema).min(1, 'Adiciona pelo menos uma linha.'),
})

export type QuoteFormValues = z.infer<typeof quoteSchema>

export function calculateQuoteTotals(
  lines: { quantity: number; unitPrice: number; vatRate: number }[] | undefined,
  discount: number | undefined
) {
  const subTotal = lines?.reduce((sum, l) => sum + (Number(l.quantity) * Number(l.unitPrice)), 0) ?? 0
  const vatTotal = lines?.reduce((sum, l) => sum + (Number(l.quantity) * Number(l.unitPrice) * (Number(l.vatRate) / 100)), 0) ?? 0
  const total = subTotal + vatTotal - (Number(discount) || 0)
  return { subTotal, vatTotal, total }
}

interface QuoteFormProps {
  defaultValues?: Partial<QuoteFormValues>
  onSubmit: (values: QuoteFormValues) => void
  isLoading?: boolean
  submitLabel?: string
}

export function QuoteForm({ defaultValues, onSubmit, isLoading, submitLabel = 'Guardar' }: QuoteFormProps) {
  const { data: clientsData } = useClients({ pageSize: 200 })

  const {
    register,
    control,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<QuoteFormValues>({
    resolver: zodResolver(quoteSchema),
    defaultValues: {
      lines: [{ description: '', quantity: 1, unitPrice: 0, vatRate: 23 }],
      ...defaultValues,
    },
  })

  const { fields, append, remove } = useFieldArray({ control, name: 'lines' })

  const lines = watch('lines')
  const discount = watch('discount')

  const { subTotal, vatTotal, total } = calculateQuoteTotals(lines, discount)

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">

      {/* ── Section 1: Dados gerais ── */}
      <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <div className="px-5 py-3.5 border-b" style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}>
          <h2 className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-muted)' }}>
            Dados gerais
          </h2>
        </div>

        <div className="p-5 grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField label="Cliente *" id="qf-client" error={errors.clientId?.message}>
            <select
              id="qf-client"
              {...register('clientId')}
              style={{
                borderColor: errors.clientId ? '#fca5a5' : 'var(--color-line-strong)',
                color: 'var(--color-ink)',
              }}
              className="form-input"
            >
              <option value="">Selecionar cliente...</option>
              {clientsData?.items.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </FormField>

          <FormField label="Válido até" id="qf-valid-until" error={errors.validUntil?.message}>
            <input
              id="qf-valid-until"
              type="date"
              {...register('validUntil')}
              className="form-input"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)' }}
            />
          </FormField>

          <FormField label="Desconto (€)" id="qf-discount" error={errors.discount?.message}>
            <input
              id="qf-discount"
              type="number"
              step="0.01"
              min="0"
              placeholder="0.00"
              {...register('discount', {
                // valueAsNumber turns an emptied input into NaN, which z.number().optional()
                // rejects with a confusing error — coerce empty string to undefined instead.
                setValueAs: (v) => (v === '' || v === null || v === undefined ? undefined : Number(v)),
              })}
              className="form-input"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)' }}
            />
          </FormField>
        </div>

        <div className="px-5 pb-5">
          <FormField label="Notas internas" id="qf-notes" error={errors.notes?.message}>
            <textarea
              id="qf-notes"
              {...register('notes')}
              rows={2}
              placeholder="Observações, condições, etc."
              className="form-input resize-none"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)' }}
            />
          </FormField>
        </div>
      </section>

      {/* ── Section 2: Linhas ── */}
      <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <div className="px-5 py-3.5 border-b flex items-center justify-between" style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}>
          <h2 className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-muted)' }}>
            Itens / Serviços
          </h2>
          <button
            type="button"
            onClick={() => append({ description: '', quantity: 1, unitPrice: 0, vatRate: 23 })}
            className="text-xs font-semibold flex items-center gap-1 transition-colors duration-150"
            style={{ color: 'var(--color-brand-600)' }}
          >
            <svg width="12" height="12" viewBox="0 0 12 12" fill="none">
              <path d="M6 1v10M1 6h10" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
            </svg>
            Adicionar linha
          </button>
        </div>

        {/* Table header */}
        <div
          className="hidden sm:grid px-5 py-2 text-xs font-semibold uppercase tracking-wide"
          style={{
            gridTemplateColumns: '1fr 80px 100px 80px 90px 32px',
            gap: '8px',
            color: 'var(--color-subtle)',
            borderBottom: '1px solid var(--color-line)',
          }}
        >
          <span>Descrição</span>
          <span>Qtd.</span>
          <span>Preço unit.</span>
          <span>IVA</span>
          <span className="text-right">Total</span>
          <span />
        </div>

        {errors.lines?.root && (
          <p className="px-5 py-2 text-xs" style={{ color: '#dc2626' }}>{errors.lines.root.message}</p>
        )}

        <div className="divide-y" style={{ borderColor: 'var(--color-line)' }}>
          {fields.map((field, index) => {
            const qty = Number(lines?.[index]?.quantity) || 0
            const price = Number(lines?.[index]?.unitPrice) || 0
            const vat = Number(lines?.[index]?.vatRate) || 0
            const lineTotal = qty * price * (1 + vat / 100)

            return (
              <div key={field.id} className="px-5 py-3 space-y-2 sm:space-y-0 sm:grid sm:items-center" style={{ gridTemplateColumns: '1fr 80px 100px 80px 90px 32px', gap: '8px' }}>
                {/* Description */}
                <div>
                  <label className="text-xs font-medium sm:hidden mb-1 block" style={{ color: 'var(--color-muted)' }}>Descrição</label>
                  <input
                    {...register(`lines.${index}.description`)}
                    placeholder="Ex: Instalação de tomada"
                    className="form-input"
                    style={{
                      borderColor: errors.lines?.[index]?.description ? '#fca5a5' : 'var(--color-line-strong)',
                      color: 'var(--color-ink)',
                    }}
                  />
                  {errors.lines?.[index]?.description && (
                    <p className="mt-0.5 text-xs" style={{ color: '#dc2626' }}>{errors.lines[index]!.description!.message}</p>
                  )}
                </div>

                {/* Qty */}
                <div>
                  <label className="text-xs font-medium sm:hidden mb-1 block" style={{ color: 'var(--color-muted)' }}>Qtd.</label>
                  <input
                    type="number"
                    step="0.01"
                    min="0.01"
                    {...register(`lines.${index}.quantity`, { valueAsNumber: true })}
                    className="form-input text-right"
                    style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)' }}
                  />
                </div>

                {/* Unit price */}
                <div>
                  <label className="text-xs font-medium sm:hidden mb-1 block" style={{ color: 'var(--color-muted)' }}>Preço unit.</label>
                  <input
                    type="number"
                    step="0.01"
                    min="0"
                    placeholder="0.00"
                    {...register(`lines.${index}.unitPrice`, { valueAsNumber: true })}
                    className="form-input text-right"
                    style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)' }}
                  />
                </div>

                {/* VAT */}
                <div>
                  <label className="text-xs font-medium sm:hidden mb-1 block" style={{ color: 'var(--color-muted)' }}>IVA</label>
                  <select
                    {...register(`lines.${index}.vatRate`, { valueAsNumber: true })}
                    className="form-input"
                    style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)' }}
                  >
                    <option value={0}>0%</option>
                    <option value={6}>6%</option>
                    <option value={13}>13%</option>
                    <option value={23}>23%</option>
                  </select>
                </div>

                {/* Line total */}
                <div className="text-right text-sm font-semibold" style={{ color: 'var(--color-ink)' }}>
                  <label className="text-xs font-medium sm:hidden mb-1 block text-left" style={{ color: 'var(--color-muted)' }}>Total</label>
                  {formatCurrency(lineTotal)}
                </div>

                {/* Remove */}
                <div className="flex justify-end sm:justify-center">
                  {fields.length > 1 ? (
                    <button
                      type="button"
                      onClick={() => remove(index)}
                      className="w-7 h-7 rounded-md flex items-center justify-center transition-colors duration-150"
                      style={{ color: 'var(--color-muted)' }}
                      onMouseEnter={e => { e.currentTarget.style.backgroundColor = '#fef2f2'; e.currentTarget.style.color = '#dc2626' }}
                      onMouseLeave={e => { e.currentTarget.style.backgroundColor = 'transparent'; e.currentTarget.style.color = 'var(--color-muted)' }}
                      title="Remover linha"
                    >
                      <svg width="12" height="12" viewBox="0 0 12 12" fill="none">
                        <path d="M1 1l10 10M11 1L1 11" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
                      </svg>
                    </button>
                  ) : <div className="w-7" />}
                </div>
              </div>
            )
          })}
        </div>

        {/* Totals */}
        <div
          className="px-5 py-4 space-y-2 border-t"
          style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}
        >
          <div className="flex justify-end gap-12 text-sm" style={{ color: 'var(--color-muted)' }}>
            <span>Subtotal</span>
            <span className="w-24 text-right">{formatCurrency(subTotal)}</span>
          </div>
          <div className="flex justify-end gap-12 text-sm" style={{ color: 'var(--color-muted)' }}>
            <span>IVA</span>
            <span className="w-24 text-right">{formatCurrency(vatTotal)}</span>
          </div>
          {(Number(discount) > 0) && (
            <div className="flex justify-end gap-12 text-sm" style={{ color: 'var(--color-muted)' }}>
              <span>Desconto</span>
              <span className="w-24 text-right">-{formatCurrency(Number(discount))}</span>
            </div>
          )}
          <div
            className="flex justify-end gap-12 text-base font-bold pt-2 border-t"
            style={{ color: 'var(--color-ink)', borderColor: 'var(--color-line)' }}
          >
            <span>Total</span>
            <span className="w-24 text-right" style={{ color: 'var(--color-brand-600)' }}>{formatCurrency(total)}</span>
          </div>
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
