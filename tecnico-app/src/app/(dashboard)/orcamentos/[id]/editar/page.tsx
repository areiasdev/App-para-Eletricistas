'use client'

import { use } from 'react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { useQuote, useUpdateQuote } from '@/hooks/useQuotes'
import { QuoteForm, type QuoteFormValues } from '@/components/features/QuoteForm'
import { getErrorMessage } from '@/lib/api/client'

export default function EditarOrcamentoPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params)
  const router = useRouter()
  const { data: quote, isLoading } = useQuote(id)
  const updateQuote = useUpdateQuote(id)

  const handleSubmit = (values: QuoteFormValues) => {
    updateQuote.mutate(
      {
        clientId: values.clientId,
        discount: values.discount as number | undefined,
        notes: values.notes,
        validUntil: values.validUntil || undefined,
        lines: values.lines.map(l => ({
          description: l.description,
          quantity: Number(l.quantity),
          unitPrice: Number(l.unitPrice),
          vatRate: Number(l.vatRate),
        })),
      },
      { onSuccess: () => router.push(`/dashboard/orcamentos/${id}`) }
    )
  }

  if (isLoading) return <div className="animate-pulse h-8 bg-gray-100 rounded w-1/3" />

  return (
    <div className="max-w-3xl space-y-6">
      <div className="flex items-center gap-2 text-sm text-gray-500">
        <Link href="/dashboard/orcamentos" className="hover:text-gray-700">Orçamentos</Link>
        <span>/</span>
        <Link href={`/dashboard/orcamentos/${id}`} className="hover:text-gray-700 font-mono">{quote?.number}</Link>
        <span>/</span>
        <span className="text-gray-900">Editar</span>
      </div>

      <h1 className="text-2xl font-bold text-gray-900">Editar Orçamento</h1>

      {updateQuote.isError && (
        <p className="text-sm text-red-600 bg-red-50 rounded-md px-4 py-3">
          {getErrorMessage(updateQuote.error)}
        </p>
      )}

      {quote && (
        <QuoteForm
          defaultValues={{
            clientId: quote.clientId,
            discount: quote.discount ?? undefined,
            notes: quote.notes ?? '',
            validUntil: quote.validUntil
              ? new Date(quote.validUntil).toISOString().split('T')[0]
              : '',
            lines: quote.lines.map(l => ({
              description: l.description,
              quantity: l.quantity,
              unitPrice: l.unitPrice,
              vatRate: l.vatRate,
            })),
          }}
          onSubmit={handleSubmit}
          isLoading={updateQuote.isPending}
          submitLabel="Guardar alterações"
        />
      )}
    </div>
  )
}
