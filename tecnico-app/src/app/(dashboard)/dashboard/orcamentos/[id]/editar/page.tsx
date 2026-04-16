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

  if (isLoading) {
    return (
      <div className="max-w-3xl space-y-4 animate-pulse">
        <div className="h-8 rounded-lg w-1/3" style={{ backgroundColor: 'var(--color-line)' }} />
        <div className="h-4 rounded-lg w-1/4" style={{ backgroundColor: 'var(--color-line)' }} />
      </div>
    )
  }

  return (
    <div className="max-w-3xl space-y-6">
      <div className="flex items-center gap-2 text-sm" style={{ color: 'var(--color-muted)' }}>
        <Link href="/dashboard/orcamentos"
          onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-ink)')}
          onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-muted)')}
        >Orçamentos</Link>
        <span style={{ color: 'var(--color-line-strong)' }}>/</span>
        <Link href={`/dashboard/orcamentos/${id}`}
          className="font-mono"
          onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-ink)')}
          onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-muted)')}
        >{quote?.number}</Link>
        <span style={{ color: 'var(--color-line-strong)' }}>/</span>
        <span style={{ color: 'var(--color-ink)' }}>Editar</span>
      </div>

      <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Editar Orçamento</h1>

      {updateQuote.isError && (
        <p className="text-sm rounded-xl px-4 py-3" style={{ color: '#dc2626', backgroundColor: '#fef2f2' }}>
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
