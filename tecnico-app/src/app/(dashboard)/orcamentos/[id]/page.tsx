'use client'

import { use } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useQuote, useUpdateQuoteStatus, useDeleteQuote } from '@/hooks/useQuotes'
import { QuoteStatusBadge } from '@/components/features/QuoteStatusBadge'
import { formatDate, formatCurrency } from '@/lib/utils/formatters'
import type { QuoteStatus } from '@/types'

const nextStatuses: Partial<Record<QuoteStatus, { status: QuoteStatus; label: string; cls: string }[]>> = {
  Draft:    [{ status: 'Sent', label: 'Marcar como Enviado', cls: 'bg-blue-600 text-white hover:bg-blue-500' }],
  Sent:     [
    { status: 'Accepted', label: 'Aceite pelo cliente', cls: 'bg-green-600 text-white hover:bg-green-500' },
    { status: 'Rejected', label: 'Recusado pelo cliente', cls: 'border border-red-300 text-red-600 hover:bg-red-50' },
    { status: 'Draft',    label: 'Revogar envio',         cls: 'border border-gray-300 text-gray-600 hover:bg-gray-50' },
  ],
  Accepted: [{ status: 'Invoiced', label: 'Marcar como Faturado', cls: 'bg-purple-600 text-white hover:bg-purple-500' }],
}

export default function OrcamentoDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params)
  const router = useRouter()
  const { data: quote, isLoading } = useQuote(id)
  const updateStatus = useUpdateQuoteStatus()
  const deleteQuote = useDeleteQuote()

  const handleStatusChange = (status: QuoteStatus) => {
    updateStatus.mutate({ id, status })
  }

  const handleDelete = () => {
    if (!confirm(`Apagar o orçamento ${quote?.number}?`)) return
    deleteQuote.mutate(id, { onSuccess: () => router.push('/dashboard/orcamentos') })
  }

  if (isLoading) {
    return (
      <div className="space-y-4 animate-pulse">
        <div className="h-8 bg-gray-100 rounded w-1/3" />
        <div className="h-4 bg-gray-100 rounded w-1/4" />
      </div>
    )
  }

  if (!quote) {
    return (
      <div className="text-center py-12">
        <p className="text-gray-500">Orçamento não encontrado.</p>
        <Link href="/dashboard/orcamentos" className="text-blue-600 text-sm hover:underline mt-2 inline-block">
          Voltar à lista
        </Link>
      </div>
    )
  }

  const actions = nextStatuses[quote.status] ?? []

  return (
    <div className="max-w-3xl space-y-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-gray-500">
        <Link href="/dashboard/orcamentos" className="hover:text-gray-700">Orçamentos</Link>
        <span>/</span>
        <span className="text-gray-900 font-mono">{quote.number}</span>
      </div>

      {/* Header */}
      <div className="flex items-start justify-between gap-4">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold text-gray-900 font-mono">{quote.number}</h1>
            <QuoteStatusBadge status={quote.status} />
          </div>
          <p className="text-sm text-gray-500 mt-1">Cliente: {quote.clientName}</p>
        </div>
        <div className="flex flex-wrap gap-2 justify-end">
          {quote.status === 'Draft' && (
            <>
              <Link
                href={`/dashboard/orcamentos/${id}/editar`}
                className="rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors"
              >
                Editar
              </Link>
              <button
                onClick={handleDelete}
                className="rounded-md border border-red-200 bg-white px-4 py-2 text-sm font-medium text-red-600 hover:bg-red-50 transition-colors"
              >
                Apagar
              </button>
            </>
          )}
          {actions.map(a => (
            <button
              key={a.status}
              onClick={() => handleStatusChange(a.status)}
              disabled={updateStatus.isPending}
              className={`rounded-md px-4 py-2 text-sm font-medium transition-colors disabled:opacity-60 ${a.cls}`}
            >
              {a.label}
            </button>
          ))}
        </div>
      </div>

      {/* Meta */}
      <div className="bg-white rounded-lg border border-gray-200 divide-y divide-gray-100">
        {quote.validUntil && (
          <InfoRow label="Válido até" value={formatDate(quote.validUntil)} />
        )}
        {quote.notes && <InfoRow label="Notas" value={quote.notes} />}
        <InfoRow label="Criado em" value={formatDate(quote.createdAt)} />
        {quote.signedAt && <InfoRow label="Assinado em" value={formatDate(quote.signedAt)} />}
      </div>

      {/* Lines */}
      <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              {['Descrição', 'Qtd', 'Preço unit.', 'IVA', 'Total linha'].map(h => (
                <th key={h} className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {quote.lines.map(line => (
              <tr key={line.id}>
                <td className="px-6 py-4 text-sm text-gray-900">{line.description}</td>
                <td className="px-6 py-4 text-sm text-gray-500">{line.quantity}</td>
                <td className="px-6 py-4 text-sm text-gray-500">{formatCurrency(line.unitPrice)}</td>
                <td className="px-6 py-4 text-sm text-gray-500">{line.vatRate}%</td>
                <td className="px-6 py-4 text-sm text-gray-900 font-medium">{formatCurrency(line.lineTotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        {/* Totals */}
        <div className="px-6 py-4 border-t border-gray-200 space-y-1 text-sm bg-gray-50">
          <div className="flex justify-between text-gray-500">
            <span>Subtotal</span>
            <span>{formatCurrency(quote.subTotal)}</span>
          </div>
          <div className="flex justify-between text-gray-500">
            <span>IVA</span>
            <span>{formatCurrency(quote.vatTotal)}</span>
          </div>
          {quote.discount != null && quote.discount > 0 && (
            <div className="flex justify-between text-gray-500">
              <span>Desconto</span>
              <span>-{formatCurrency(quote.discount)}</span>
            </div>
          )}
          <div className="flex justify-between font-semibold text-gray-900 text-base pt-1 border-t border-gray-200">
            <span>Total</span>
            <span>{formatCurrency(quote.total)}</span>
          </div>
        </div>
      </div>
    </div>
  )
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex px-6 py-4 gap-6">
      <span className="text-sm font-medium text-gray-500 w-28 shrink-0">{label}</span>
      <span className="text-sm text-gray-900">{value}</span>
    </div>
  )
}
