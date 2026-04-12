'use client'

import { useState } from 'react'
import Link from 'next/link'
import { useInterventions, useDeleteIntervention } from '@/hooks/useInterventions'
import { InterventionStatusBadge } from '@/components/features/InterventionStatusBadge'
import { formatDate, formatDateTime } from '@/lib/utils/formatters'
import { getErrorMessage } from '@/lib/api/client'
import type { InterventionStatus } from '@/types'

const statusOptions: { value: InterventionStatus | ''; label: string }[] = [
  { value: '', label: 'Todos' },
  { value: 'Scheduled', label: 'Agendadas' },
  { value: 'InProgress', label: 'Em curso' },
  { value: 'Completed', label: 'Concluídas' },
]

export default function IntervencoesPage() {
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [status, setStatus] = useState<InterventionStatus | ''>('')
  const [page, setPage] = useState(1)

  const { data, isLoading, isError, error } = useInterventions({
    search: debouncedSearch || undefined,
    status: status || undefined,
    page,
    pageSize: 20,
  })

  const deleteIntervention = useDeleteIntervention()

  const handleSearch = (value: string) => {
    setSearch(value)
    setPage(1)
    const t = setTimeout(() => setDebouncedSearch(value), 300)
    return () => clearTimeout(t)
  }

  const handleDelete = (id: string, title: string) => {
    if (!confirm(`Apagar a intervenção "${title}"?`)) return
    deleteIntervention.mutate(id)
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Intervenções</h1>
          <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
            {data ? `${data.totalCount} intervenção${data.totalCount !== 1 ? 'ões' : ''}` : '\u00a0'}
          </p>
        </div>
        <Link
          href="/dashboard/intervencoes/novo"
          className="inline-flex items-center gap-2 rounded-lg px-4 py-2.5 text-sm font-semibold transition-all duration-150 hover:brightness-110 active:scale-[0.99]"
          style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
        >
          + Nova Intervenção
        </Link>
      </div>

      {/* Filters */}
      <div className="flex gap-3 flex-wrap">
        <input
          type="search"
          placeholder="Pesquisar por título ou cliente..."
          value={search}
          onChange={(e) => handleSearch(e.target.value)}
          className="rounded-lg border px-3 py-2 text-sm outline-none transition-all duration-150 w-64"
          style={{
            borderColor: 'var(--color-line-strong)',
            backgroundColor: 'var(--color-canvas)',
            color: 'var(--color-ink)',
          }}
          onFocus={(e) => (e.currentTarget.style.borderColor = 'var(--color-brand-500)')}
          onBlur={(e) => (e.currentTarget.style.borderColor = 'var(--color-line-strong)')}
        />
        <div className="flex gap-1 rounded-lg border p-1" style={{ borderColor: 'var(--color-line)', backgroundColor: 'var(--color-canvas)' }}>
          {statusOptions.map((o) => (
            <button
              key={o.value}
              onClick={() => { setStatus(o.value as InterventionStatus | ''); setPage(1) }}
              className="rounded-md px-3 py-1 text-xs font-medium transition-all duration-150"
              style={{
                backgroundColor: status === o.value ? 'var(--color-brand-500)' : 'transparent',
                color: status === o.value ? 'var(--color-sidebar)' : 'var(--color-muted)',
              }}
            >
              {o.label}
            </button>
          ))}
        </div>
      </div>

      {/* Error */}
      {isError && (
        <p className="text-sm rounded-lg px-4 py-3" style={{ color: '#dc2626', backgroundColor: '#fef2f2' }}>
          {getErrorMessage(error)}
        </p>
      )}

      {/* Table */}
      <div className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'white', borderColor: 'var(--color-line)' }}>
        <table className="min-w-full">
          <thead>
            <tr style={{ borderBottom: '1px solid var(--color-line)', backgroundColor: 'var(--color-canvas)' }}>
              {['Título', 'Cliente', 'Estado', 'Agendada para', 'Equipamentos', ''].map((h) => (
                <th key={h} className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {isLoading &&
              Array.from({ length: 5 }).map((_, i) => (
                <tr key={i} style={{ borderBottom: '1px solid var(--color-line)' }}>
                  {Array.from({ length: 6 }).map((_, j) => (
                    <td key={j} className="px-5 py-4">
                      <div className="h-4 rounded animate-pulse" style={{ backgroundColor: 'var(--color-line)' }} />
                    </td>
                  ))}
                </tr>
              ))}

            {!isLoading && data?.items.length === 0 && (
              <tr>
                <td colSpan={6} className="px-5 py-12 text-center text-sm" style={{ color: 'var(--color-subtle)' }}>
                  {debouncedSearch || status
                    ? 'Nenhuma intervenção encontrada.'
                    : 'Ainda não tens intervenções registadas.'}
                </td>
              </tr>
            )}

            {data?.items.map((iv) => (
              <tr
                key={iv.id}
                className="transition-colors duration-100"
                style={{ borderBottom: '1px solid var(--color-line)' }}
                onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
                onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
              >
                <td className="px-5 py-4">
                  <Link
                    href={`/dashboard/intervencoes/${iv.id}`}
                    className="text-sm font-medium transition-colors duration-150"
                    style={{ color: 'var(--color-ink)' }}
                    onMouseEnter={(e) => (e.currentTarget.style.color = 'var(--color-brand-500)')}
                    onMouseLeave={(e) => (e.currentTarget.style.color = 'var(--color-ink)')}
                  >
                    {iv.title}
                  </Link>
                </td>
                <td className="px-5 py-4 text-sm" style={{ color: 'var(--color-muted)' }}>{iv.clientName}</td>
                <td className="px-5 py-4">
                  <InterventionStatusBadge status={iv.status} />
                </td>
                <td className="px-5 py-4 text-sm" style={{ color: 'var(--color-muted)' }}>
                  {iv.scheduledAt ? formatDateTime(iv.scheduledAt) : '—'}
                </td>
                <td className="px-5 py-4 text-sm" style={{ color: 'var(--color-muted)' }}>
                  {iv.equipmentCount > 0 ? `${iv.equipmentCount} equipamento${iv.equipmentCount !== 1 ? 's' : ''}` : '—'}
                </td>
                <td className="px-5 py-4 text-right">
                  <div className="flex justify-end gap-3">
                    <Link
                      href={`/dashboard/intervencoes/${iv.id}`}
                      className="text-xs font-medium transition-colors duration-150"
                      style={{ color: 'var(--color-brand-500)' }}
                    >
                      Ver
                    </Link>
                    <Link
                      href={`/dashboard/intervencoes/${iv.id}/editar`}
                      className="text-xs font-medium transition-colors duration-150"
                      style={{ color: 'var(--color-muted)' }}
                      onMouseEnter={(e) => (e.currentTarget.style.color = 'var(--color-ink)')}
                      onMouseLeave={(e) => (e.currentTarget.style.color = 'var(--color-muted)')}
                    >
                      Editar
                    </Link>
                    <button
                      onClick={() => handleDelete(iv.id, iv.title)}
                      className="text-xs font-medium transition-colors duration-150"
                      style={{ color: '#dc2626' }}
                    >
                      Apagar
                    </button>
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
              className="px-3 py-1.5 rounded-lg border text-xs font-medium disabled:opacity-40 transition-colors duration-150"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)' }}
            >
              ← Anterior
            </button>
            <button
              disabled={!data.hasNextPage}
              onClick={() => setPage((p) => p + 1)}
              className="px-3 py-1.5 rounded-lg border text-xs font-medium disabled:opacity-40 transition-colors duration-150"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)' }}
            >
              Próxima →
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
