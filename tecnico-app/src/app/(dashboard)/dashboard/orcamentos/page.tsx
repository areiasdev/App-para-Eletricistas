'use client'

import { useState, Suspense } from 'react'
import { useSearchParams } from 'next/navigation'
import Link from 'next/link'
import { toast } from 'sonner'
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

function OrcamentosContent() {
  const searchParams = useSearchParams()
  const clientIdFilter = searchParams.get('clientId') ?? undefined

  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [status, setStatus] = useState<QuoteStatus | ''>('')
  const [page, setPage] = useState(1)

  const { data, isLoading, isError, error } = useQuotes({
    search: debouncedSearch || undefined,
    status: status || undefined,
    clientId: clientIdFilter,
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
    deleteQuote.mutate(id, {
      onSuccess: () => toast.success('Orçamento apagado.'),
      onError: (err) => toast.error(getErrorMessage(err)),
    })
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Orçamentos</h1>
          <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
            {data ? `${data.totalCount} orçamento${data.totalCount !== 1 ? 's' : ''}` : '\u00a0'}
          </p>
        </div>
        <Link
          href="/dashboard/orcamentos/novo"
          className="rounded-lg px-4 py-2 text-sm font-semibold transition-all duration-150 hover:brightness-110 active:scale-[0.99]"
          style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
        >
          + Novo Orçamento
        </Link>
      </div>

      {/* Filters */}
      <div className="flex gap-3 flex-wrap items-center">
        <input
          type="search"
          placeholder="Pesquisar por número ou cliente..."
          value={search}
          onChange={(e) => handleSearch(e.target.value)}
          className="rounded-lg px-3 py-2 text-sm outline-none transition-all duration-150 w-64"
          style={{ border: '1.5px solid var(--color-line-strong)', backgroundColor: 'var(--color-card)', color: 'var(--color-ink)' }}
          onFocus={e => { e.currentTarget.style.borderColor = 'var(--color-brand-500)'; e.currentTarget.style.boxShadow = '0 0 0 3px rgba(245,158,11,0.12)' }}
          onBlur={e => { e.currentTarget.style.borderColor = 'var(--color-line-strong)'; e.currentTarget.style.boxShadow = 'none' }}
        />
        <select
          value={status}
          onChange={(e) => { setStatus(e.target.value as QuoteStatus | ''); setPage(1) }}
          className="rounded-lg px-3 py-2 text-sm outline-none transition-all duration-150"
          style={{ border: '1.5px solid var(--color-line-strong)', backgroundColor: 'var(--color-card)', color: 'var(--color-ink)' }}
          onFocus={e => { e.currentTarget.style.borderColor = 'var(--color-brand-500)'; e.currentTarget.style.boxShadow = '0 0 0 3px rgba(245,158,11,0.12)' }}
          onBlur={e => { e.currentTarget.style.borderColor = 'var(--color-line-strong)'; e.currentTarget.style.boxShadow = 'none' }}
        >
          {statusOptions.map(o => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>
        {clientIdFilter && (
          <Link
            href="/dashboard/orcamentos"
            className="text-xs font-medium rounded-lg px-3 py-2 border transition-colors duration-150"
            style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-muted)', backgroundColor: 'var(--color-canvas)' }}
          >
            × Limpar filtro de cliente
          </Link>
        )}
      </div>

      {/* Error */}
      {isError && (
        <p className="text-sm rounded-xl px-4 py-3" style={{ color: '#dc2626', backgroundColor: '#fef2f2' }}>
          {getErrorMessage(error)}
        </p>
      )}

      {/* Table */}
      <div className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <table className="min-w-full">
          <thead>
            <tr style={{ borderBottom: '1px solid var(--color-line)', backgroundColor: 'var(--color-canvas)' }}>
              {['Número', 'Cliente', 'Estado', 'Total', 'Válido até', 'Data', ''].map((h) => (
                <th
                  key={h}
                  className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide"
                  style={{ color: 'var(--color-muted)' }}
                >
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {isLoading && Array.from({ length: 5 }).map((_, i) => (
              <tr key={i} style={{ borderBottom: '1px solid var(--color-line)' }}>
                {Array.from({ length: 7 }).map((_, j) => (
                  <td key={j} className="px-5 py-4">
                    <div className="h-4 rounded animate-pulse" style={{ backgroundColor: 'var(--color-line)' }} />
                  </td>
                ))}
              </tr>
            ))}

            {!isLoading && data?.items.length === 0 && (
              <tr>
                <td colSpan={7} className="px-5 py-14 text-center text-sm" style={{ color: 'var(--color-subtle)' }}>
                  {debouncedSearch || status || clientIdFilter
                    ? 'Nenhum orçamento encontrado.'
                    : <>Ainda não tens orçamentos.{' '}
                        <Link href="/dashboard/orcamentos/novo" className="font-medium underline underline-offset-2" style={{ color: 'var(--color-brand-600)' }}>
                          Cria o primeiro
                        </Link>
                      </>}
                </td>
              </tr>
            )}

            {data?.items.map((quote) => (
              <tr
                key={quote.id}
                style={{ borderBottom: '1px solid var(--color-line)' }}
                onMouseEnter={e => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
                onMouseLeave={e => (e.currentTarget.style.backgroundColor = '')}
              >
                <td className="px-5 py-3.5">
                  <Link
                    href={`/dashboard/orcamentos/${quote.id}`}
                    className="text-sm font-mono font-medium transition-colors duration-150"
                    style={{ color: 'var(--color-ink)' }}
                    onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-brand-500)')}
                    onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-ink)')}
                  >
                    {quote.number}
                  </Link>
                </td>
                <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-muted)' }}>{quote.clientName}</td>
                <td className="px-5 py-3.5">
                  <QuoteStatusBadge status={quote.status} />
                </td>
                <td className="px-5 py-3.5 text-sm font-medium" style={{ color: 'var(--color-ink)' }}>
                  {formatCurrency(quote.total)}
                </td>
                <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-muted)' }}>
                  {quote.validUntil ? formatDate(quote.validUntil) : '—'}
                </td>
                <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-subtle)' }}>{formatDate(quote.createdAt)}</td>
                <td className="px-5 py-3.5">
                  <div className="flex justify-end gap-3">
                    <Link
                      href={`/dashboard/orcamentos/${quote.id}`}
                      className="text-xs font-medium transition-colors duration-150"
                      style={{ color: 'var(--color-muted)' }}
                      onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-brand-500)')}
                      onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-muted)')}
                    >
                      Ver
                    </Link>
                    {quote.status === 'Draft' && (
                      <>
                        <Link
                          href={`/dashboard/orcamentos/${quote.id}/editar`}
                          className="text-xs font-medium transition-colors duration-150"
                          style={{ color: 'var(--color-muted)' }}
                          onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-ink)')}
                          onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-muted)')}
                        >
                          Editar
                        </Link>
                        <button
                          onClick={() => handleDelete(quote.id, quote.number)}
                          className="text-xs font-medium transition-colors duration-150"
                          style={{ color: 'var(--color-subtle)' }}
                          onMouseEnter={e => (e.currentTarget.style.color = '#dc2626')}
                          onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-subtle)')}
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
        <div className="flex items-center justify-between text-sm" style={{ color: 'var(--color-muted)' }}>
          <span>Página {data.page} de {data.totalPages}</span>
          <div className="flex gap-2">
            <button
              disabled={!data.hasPreviousPage}
              onClick={() => setPage((p) => p - 1)}
              className="px-3 py-1.5 rounded-lg border text-sm font-medium disabled:opacity-40"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
            >
              ← Anterior
            </button>
            <button
              disabled={!data.hasNextPage}
              onClick={() => setPage((p) => p + 1)}
              className="px-3 py-1.5 rounded-lg border text-sm font-medium disabled:opacity-40"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
            >
              Próxima →
            </button>
          </div>
        </div>
      )}
    </div>
  )
}

export default function OrcamentosPage() {
  return (
    <Suspense fallback={<div className="h-8 w-48 rounded animate-pulse" style={{ backgroundColor: 'var(--color-line)' }} />}>
      <OrcamentosContent />
    </Suspense>
  )
}
