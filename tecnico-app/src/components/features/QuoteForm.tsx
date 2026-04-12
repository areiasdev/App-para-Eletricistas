'use client'

import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { cn } from '@/lib/utils'
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

  const subTotal = lines?.reduce((sum, l) => sum + (Number(l.quantity) * Number(l.unitPrice)), 0) ?? 0
  const vatTotal = lines?.reduce((sum, l) => sum + (Number(l.quantity) * Number(l.unitPrice) * (Number(l.vatRate) / 100)), 0) ?? 0
  const total = subTotal + vatTotal - (Number(discount) || 0)

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      {/* Cliente + meta */}
      <div className="bg-white rounded-lg border border-gray-200 p-6 space-y-4">
        <h2 className="text-sm font-semibold text-gray-900 uppercase tracking-wide">Dados do orçamento</h2>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Field label="Cliente *" error={errors.clientId?.message}>
            <select {...register('clientId')} className={inputCls(!!errors.clientId)}>
              <option value="">Selecionar cliente...</option>
              {clientsData?.items.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </Field>

          <Field label="Válido até" error={errors.validUntil?.message}>
            <input type="date" {...register('validUntil')} className={inputCls(false)} />
          </Field>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Field label="Desconto (€)" error={errors.discount?.message}>
            <input type="number" step="0.01" min="0" {...register('discount', { valueAsNumber: true })} className={inputCls(!!errors.discount)} />
          </Field>
        </div>

        <Field label="Notas" error={errors.notes?.message}>
          <textarea {...register('notes')} rows={2} className={inputCls(false) + ' resize-none'} />
        </Field>
      </div>

      {/* Lines */}
      <div className="bg-white rounded-lg border border-gray-200 p-6 space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-sm font-semibold text-gray-900 uppercase tracking-wide">Linhas</h2>
          <button
            type="button"
            onClick={() => append({ description: '', quantity: 1, unitPrice: 0, vatRate: 23 })}
            className="text-sm text-blue-600 hover:text-blue-800 font-medium"
          >
            + Adicionar linha
          </button>
        </div>

        {errors.lines?.root && (
          <p className="text-xs text-red-600">{errors.lines.root.message}</p>
        )}

        <div className="space-y-3">
          {fields.map((field, index) => (
            <div key={field.id} className="grid grid-cols-12 gap-2 items-start">
              <div className="col-span-5">
                <input
                  {...register(`lines.${index}.description`)}
                  placeholder="Descrição"
                  className={inputCls(!!errors.lines?.[index]?.description)}
                />
                {errors.lines?.[index]?.description && (
                  <p className="mt-0.5 text-xs text-red-600">{errors.lines[index]!.description!.message}</p>
                )}
              </div>
              <div className="col-span-2">
                <input
                  type="number"
                  step="0.01"
                  {...register(`lines.${index}.quantity`, { valueAsNumber: true })}
                  placeholder="Qtd"
                  className={inputCls(!!errors.lines?.[index]?.quantity)}
                />
              </div>
              <div className="col-span-2">
                <input
                  type="number"
                  step="0.01"
                  {...register(`lines.${index}.unitPrice`, { valueAsNumber: true })}
                  placeholder="Preço"
                  className={inputCls(!!errors.lines?.[index]?.unitPrice)}
                />
              </div>
              <div className="col-span-2">
                <select
                  {...register(`lines.${index}.vatRate`, { valueAsNumber: true })}
                  className={inputCls(false)}
                >
                  <option value={0}>0%</option>
                  <option value={6}>6%</option>
                  <option value={13}>13%</option>
                  <option value={23}>23%</option>
                </select>
              </div>
              <div className="col-span-1 flex items-center justify-center pt-2">
                {fields.length > 1 && (
                  <button
                    type="button"
                    onClick={() => remove(index)}
                    className="text-red-400 hover:text-red-600 text-lg leading-none"
                    title="Remover linha"
                  >
                    ×
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>

        {/* Totals */}
        <div className="border-t border-gray-100 pt-4 space-y-1 text-sm">
          <div className="flex justify-between text-gray-500">
            <span>Subtotal</span>
            <span>{formatCurrency(subTotal)}</span>
          </div>
          <div className="flex justify-between text-gray-500">
            <span>IVA</span>
            <span>{formatCurrency(vatTotal)}</span>
          </div>
          {(Number(discount) > 0) && (
            <div className="flex justify-between text-gray-500">
              <span>Desconto</span>
              <span>-{formatCurrency(Number(discount))}</span>
            </div>
          )}
          <div className="flex justify-between font-semibold text-gray-900 text-base pt-1 border-t border-gray-200">
            <span>Total</span>
            <span>{formatCurrency(total)}</span>
          </div>
        </div>
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
