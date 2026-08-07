'use client'

import { useState, Suspense } from 'react'
import { useSearchParams } from 'next/navigation'
import Link from 'next/link'
import { useInvoices } from '@/hooks/useInvoices'
import { useDebouncedValue } from '@/hooks/useDebouncedValue'
import { InvoiceStatusBadge } from '@/components/features/InvoiceStatusBadge'
import { formatDate, formatCurrency } from '@/lib/utils/formatters'
import { getErrorMessage } from '@/lib/api/client'
import type { InvoiceStatus } from '@/types'

const statusOptions: { value: InvoiceStatus | ''; label: string }[] = [
  { value: '', label: 'Todos' },
  { value: 'Issued', label: 'Emitida' },
  { value: 'Paid', label: 'Paga' },
  { value: 'Overdue', label: 'Em atraso' },
  { value: 'Cancelled', label: 'Cancelada' },
]

function FaturasContent() {
  const searchParams = useSearchParams()
  const clientIdFilter = searchParams.get('clientId') ?? undefined

  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search, 300)
  const [status, setStatus] = useState<InvoiceStatus | ''>('')
  const [page, setPage] = useState(1)

  const { data, isLoading, isError, error } = useInvoices({
    search: debouncedSearch || undefined,
    status: status || undefined,
    clientId: clientIdFilter,
    page,
    pageSize: 20,
  })

  const handleSearch = (value: string) => {
    setSearch(value)
    setPage(1)
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Faturas</h1>
          <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
            {data ? `${data.totalCount} fatura${data.totalCount !== 1 ? 's' : ''}` : ' '}
          </p>
        </div>
      </div>

      {/* Filters */}
      <div className="flex gap-3 flex-wrap items-center">
        <input
          type="search"
          placeholder="Pesquisar por número ou cliente..."
          value={search}
          onChange={(e) => handleSearch(e.target.value)}
          className="rounded-lg px-3 py-2 text-sm outline-none transition-all duration-150 w-full sm:w-64"
          style={{ border: '1.5px solid var(--color-line-strong)', backgroundColor: 'var(--color-card)', color: 'var(--color-ink)' }}
          onFocus={e => { e.currentTarget.style.borderColor = 'var(--color-brand-500)'; e.currentTarget.style.boxShadow = '0 0 0 3px color-mix(in srgb, var(--color-brand-500) 12%, transparent)' }}
          onBlur={e => { e.currentTarget.style.borderColor = 'var(--color-line-strong)'; e.currentTarget.style.boxShadow = 'none' }}
        />
        <select
          value={status}
          onChange={(e) => { setStatus(e.target.value as InvoiceStatus | ''); setPage(1) }}
          className="rounded-lg px-3 py-2 text-sm outline-none transition-all duration-150"
          style={{ border: '1.5px solid var(--color-line-strong)', backgroundColor: 'var(--color-card)', color: 'var(--color-ink)' }}
          onFocus={e => { e.currentTarget.style.borderColor = 'var(--color-brand-500)'; e.currentTarget.style.boxShadow = '0 0 0 3px color-mix(in srgb, var(--color-brand-500) 12%, transparent)' }}
          onBlur={e => { e.currentTarget.style.borderColor = 'var(--color-line-strong)'; e.currentTarget.style.boxShadow = 'none' }}
        >
          {statusOptions.map(o => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>
        {clientIdFilter && (
          <Link
            href="/dashboard/faturas"
            className="text-xs font-medium rounded-lg px-3 py-2 border transition-colors duration-150"
            style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-muted)', backgroundColor: 'var(--color-canvas)' }}
          >
            × Limpar filtro de cliente
          </Link>
        )}
      </div>

      {/* Error */}
      {isError && (
        <p className="text-sm rounded-xl px-4 py-3" style={{ color: 'var(--color-danger-600)', backgroundColor: 'var(--color-danger-50)' }}>
          {getErrorMessage(error)}
        </p>
      )}

      {/* Table */}
      <div className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <div className="overflow-x-auto">
        <table className="min-w-full">
          <thead>
            <tr style={{ borderBottom: '1px solid var(--color-line)', backgroundColor: 'var(--color-canvas)' }}>
              {['Número', 'Cliente', 'Estado', 'Total', 'Vencimento', 'Data', ''].map((h) => (
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
                    ? 'Nenhuma fatura encontrada.'
                    : 'Ainda não tens faturas. Fatura um orçamento aceite para começares.'}
                </td>
              </tr>
            )}

            {data?.items.map((invoice) => (
              <tr
                key={invoice.id}
                style={{ borderBottom: '1px solid var(--color-line)' }}
                onMouseEnter={e => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
                onMouseLeave={e => (e.currentTarget.style.backgroundColor = '')}
              >
                <td className="px-5 py-3.5">
                  <Link
                    href={`/dashboard/faturas/${invoice.id}`}
                    className="text-sm font-mono font-medium transition-colors duration-150"
                    style={{ color: 'var(--color-ink)' }}
                    onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-brand-500)')}
                    onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-ink)')}
                  >
                    {invoice.number}
                  </Link>
                </td>
                <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-muted)' }}>{invoice.clientName}</td>
                <td className="px-5 py-3.5">
                  <InvoiceStatusBadge status={invoice.status} />
                </td>
                <td className="px-5 py-3.5 text-sm font-medium" style={{ color: 'var(--color-ink)' }}>
                  {formatCurrency(invoice.total)}
                </td>
                <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-muted)' }}>
                  {formatDate(invoice.dueDate)}
                </td>
                <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-subtle)' }}>{formatDate(invoice.createdAt)}</td>
                <td className="px-5 py-3.5">
                  <div className="flex justify-end gap-3">
                    <Link
                      href={`/dashboard/faturas/${invoice.id}`}
                      className="text-xs font-medium transition-colors duration-150"
                      style={{ color: 'var(--color-muted)' }}
                      onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-brand-500)')}
                      onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-muted)')}
                    >
                      Ver
                    </Link>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        </div>
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

export default function FaturasPage() {
  return (
    <Suspense fallback={<div className="h-8 w-48 rounded animate-pulse" style={{ backgroundColor: 'var(--color-line)' }} />}>
      <FaturasContent />
    </Suspense>
  )
}
