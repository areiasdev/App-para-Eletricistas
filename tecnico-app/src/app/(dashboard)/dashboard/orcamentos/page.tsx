'use client'

import { useState } from 'react'
import Link from 'next/link'
import { useQuotes, useDeleteQuote } from '@/hooks/useQuotes'
import { QuoteStatusBadge } from '@/components/features/QuoteStatusBadge'
import { formatDate, formatCurrency } from '@/lib/utils/formatters'
import { getErrorMessage } from '@/lib/api/client'
import type { QuoteStatus } from '@/types'

const statusOptions: { value: QuoteStatus | ''; label: string }[] = [
  { value: '', label: 'Todos' },
  { value: 'Draft', label: 'Rascunho' },
  { value: 'Sent', label: 'Enviado' },
  { value: 'Accepted', label: 'Aceite' },
  { value: 'Rejected', label: 'Recusado' },
  { value: 'Invoiced', label: 'Faturado' },
]

export default function OrcamentosPage() {
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [status, setStatus] = useState<QuoteStatus | ''>('')
  const [page, setPage] = useState(1)

  const { data, isLoading, isError, error } = useQuotes({
    search: debouncedSearch || undefined,
    status: status || undefined,
    page,
    pageSize: 20,
  })

  const deleteQuote = useDeleteQuote()

  const handleSearch = (value: string) => {
    setSearch(value)
    setPage(1)
    const t = setTimeout(() => setDebouncedSearch(value), 300)
    return () => clearTimeout(t)
  }

  const handleDelete = (id: string, number: string) => {
    if (!confirm(`Apagar o orçamento ${number}?`)) return
    deleteQuote.mutate(id)
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Orçamentos</h1>
          <p className="text-sm text-gray-500 mt-1">
            {data ? `${data.totalCount} orçamento${data.totalCount !== 1 ? 's' : ''}` : ''}
          </p>
        </div>
        <Link
          href="/dashboard/orcamentos/novo"
          className="inline-flex items-center gap-2 rounded-md bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-500 transition-colors"
        >
          + Novo Orçamento
        </Link>
      </div>

      {/* Filters */}
      <div className="flex gap-3 flex-wrap">
        <input
          type="search"
          placeholder="Pesquisar por número ou cliente..."
          value={search}
          onChange={(e) => handleSearch(e.target.value)}
          className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 w-64"
        />
        <select
          value={status}
          onChange={(e) => { setStatus(e.target.value as QuoteStatus | ''); setPage(1) }}
          className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
        >
          {statusOptions.map(o => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>
      </div>

      {/* Error */}
      {isError && (
        <p className="text-sm text-red-600 bg-red-50 rounded-md px-4 py-3">
          {getErrorMessage(error)}
        </p>
      )}

      {/* Table */}
      <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              {['Número', 'Cliente', 'Estado', 'Total', 'Válido até', 'Data', ''].map((h) => (
                <th
                  key={h}
                  className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {isLoading &&
              Array.from({ length: 5 }).map((_, i) => (
                <tr key={i}>
                  {Array.from({ length: 7 }).map((_, j) => (
                    <td key={j} className="px-6 py-4">
                      <div className="h-4 bg-gray-100 rounded animate-pulse" />
                    </td>
                  ))}
                </tr>
              ))}

            {!isLoading && data?.items.length === 0 && (
              <tr>
                <td colSpan={7} className="px-6 py-12 text-center text-sm text-gray-400">
                  {debouncedSearch || status
                    ? 'Nenhum orçamento encontrado.'
                    : 'Ainda não tens orçamentos. Cria o primeiro!'}
                </td>
              </tr>
            )}

            {data?.items.map((quote) => (
              <tr key={quote.id} className="hover:bg-gray-50">
                <td className="px-6 py-4 text-sm font-medium text-gray-900">
                  <Link href={`/dashboard/orcamentos/${quote.id}`} className="hover:text-blue-600 font-mono">
                    {quote.number}
                  </Link>
                </td>
                <td className="px-6 py-4 text-sm text-gray-500">{quote.clientName}</td>
                <td className="px-6 py-4">
                  <QuoteStatusBadge status={quote.status} />
                </td>
                <td className="px-6 py-4 text-sm text-gray-900 font-medium">
                  {formatCurrency(quote.total)}
                </td>
                <td className="px-6 py-4 text-sm text-gray-500">
                  {quote.validUntil ? formatDate(quote.validUntil) : '—'}
                </td>
                <td className="px-6 py-4 text-sm text-gray-500">{formatDate(quote.createdAt)}</td>
                <td className="px-6 py-4 text-right text-sm">
                  <div className="flex justify-end gap-3">
                    <Link
                      href={`/dashboard/orcamentos/${quote.id}`}
                      className="text-blue-600 hover:text-blue-800"
                    >
                      Ver
                    </Link>
                    {quote.status === 'Draft' && (
                      <>
                        <Link
                          href={`/dashboard/orcamentos/${quote.id}/editar`}
                          className="text-gray-600 hover:text-gray-800"
                        >
                          Editar
                        </Link>
                        <button
                          onClick={() => handleDelete(quote.id, quote.number)}
                          className="text-red-500 hover:text-red-700"
                        >
                          Apagar
                        </button>
                      </>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-gray-500">
          <span>Página {data.page} de {data.totalPages}</span>
          <div className="flex gap-2">
            <button
              disabled={!data.hasPreviousPage}
              onClick={() => setPage((p) => p - 1)}
              className="px-3 py-1 rounded border border-gray-300 disabled:opacity-40 hover:bg-gray-50"
            >
              ← Anterior
            </button>
            <button
              disabled={!data.hasNextPage}
              onClick={() => setPage((p) => p + 1)}
              className="px-3 py-1 rounded border border-gray-300 disabled:opacity-40 hover:bg-gray-50"
            >
              Próxima →
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
