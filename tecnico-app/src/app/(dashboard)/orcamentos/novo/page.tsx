'use client'

import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { QuoteForm, type QuoteFormValues } from '@/components/features/QuoteForm'
import { useCreateQuote } from '@/hooks/useQuotes'
import { getErrorMessage } from '@/lib/api/client'

export default function NovoOrcamentoPage() {
  const router = useRouter()
  const createQuote = useCreateQuote()

  const handleSubmit = (values: QuoteFormValues) => {
    createQuote.mutate(
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
      {
        onSuccess: (quote) => router.push(`/dashboard/orcamentos/${quote.id}`),
      }
    )
  }

  return (
    <div className="max-w-3xl space-y-6">
      <div className="flex items-center gap-2 text-sm text-gray-500">
        <Link href="/dashboard/orcamentos" className="hover:text-gray-700">Orçamentos</Link>
        <span>/</span>
        <span className="text-gray-900">Novo orçamento</span>
      </div>

      <h1 className="text-2xl font-bold text-gray-900">Novo Orçamento</h1>

      {createQuote.isError && (
        <p className="text-sm text-red-600 bg-red-50 rounded-md px-4 py-3">
          {getErrorMessage(createQuote.error)}
        </p>
      )}

      <QuoteForm
        onSubmit={handleSubmit}
        isLoading={createQuote.isPending}
        submitLabel="Criar Orçamento"
      />
    </div>
  )
}
