'use client'

import { useState, Suspense } from 'react'
import { useSearchParams } from 'next/navigation'
import Link from 'next/link'
import { toast } from 'sonner'
import { useEquipmentList, useDeleteEquipment } from '@/hooks/useEquipment'
import { useDebouncedValue } from '@/hooks/useDebouncedValue'
import { formatDate } from '@/lib/utils/formatters'
import { getErrorMessage } from '@/lib/api/client'

function MaintenanceBadge({ date }: { date?: string }) {
  if (!date) return <span style={{ color: 'var(--color-subtle)' }}>—</span>
  const d = new Date(date)
  const now = new Date()
  const daysUntil = Math.ceil((d.getTime() - now.getTime()) / (1000 * 60 * 60 * 24))

  if (daysUntil < 0) {
    return (
      <span className="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium" style={{ backgroundColor: '#fef2f2', color: '#dc2626' }}>
        Vencida
      </span>
    )
  }
  if (daysUntil <= 30) {
    return (
      <span className="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium" style={{ backgroundColor: '#fef3c7', color: '#b45309' }}>
        {formatDate(date)}
      </span>
    )
  }
  return <span className="text-sm" style={{ color: 'var(--color-muted)' }}>{formatDate(date)}</span>
}

function EquipamentosContent() {
  const searchParams = useSearchParams()
  const clientIdFilter = searchParams.get('clientId') ?? undefined

  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search, 300)
  const [page, setPage] = useState(1)

  const { data, isLoading, isError, error } = useEquipmentList({
    search: debouncedSearch || undefined,
    clientId: clientIdFilter,
    page,
    pageSize: 20,
  })

  const deleteEquipment = useDeleteEquipment()

  const handleSearch = (value: string) => {
    setSearch(value)
    setPage(1)
  }

  const handleDelete = (id: string, type: string) => {
    if (!confirm(`Apagar o equipamento "${type}"?`)) return
    deleteEquipment.mutate(id, {
      onSuccess: () => toast.success('Equipamento apagado.'),
      onError: (err) => toast.error(getErrorMessage(err)),
    })
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Equipamentos</h1>
          <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
            {data ? `${data.totalCount} equipamento${data.totalCount !== 1 ? 's' : ''}` : '\u00a0'}
          </p>
        </div>
        <Link
          href="/dashboard/equipamentos/novo"
          className="rounded-lg px-4 py-2 text-sm font-semibold transition-all duration-150 hover:brightness-110 active:scale-[0.99]"
          style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
        >
          + Novo Equipamento
        </Link>
      </div>

      <div className="flex items-center gap-3">
        <input
          type="search"
          placeholder="Pesquisar por tipo, marca ou modelo..."
          value={search}
          onChange={e => handleSearch(e.target.value)}
          className="w-full max-w-sm rounded-lg px-3 py-2 text-sm outline-none transition-all duration-150"
          style={{ border: '1.5px solid var(--color-line-strong)', backgroundColor: 'var(--color-card)', color: 'var(--color-ink)' }}
          onFocus={e => { e.currentTarget.style.borderColor = 'var(--color-brand-500)'; e.currentTarget.style.boxShadow = '0 0 0 3px rgba(245,158,11,0.12)' }}
          onBlur={e => { e.currentTarget.style.borderColor = 'var(--color-line-strong)'; e.currentTarget.style.boxShadow = 'none' }}
        />
        {clientIdFilter && (
          <Link
            href="/dashboard/equipamentos"
            className="text-xs font-medium rounded-lg px-3 py-2 border transition-colors duration-150"
            style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-muted)', backgroundColor: 'var(--color-canvas)' }}
          >
            × Limpar filtro
          </Link>
        )}
      </div>

      {isError && (
        <p className="text-sm rounded-xl px-4 py-3" style={{ color: '#dc2626', backgroundColor: '#fef2f2' }}>
          {getErrorMessage(error)}
        </p>
      )}

      <div className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <table className="min-w-full">
          <thead>
            <tr style={{ borderBottom: '1px solid var(--color-line)', backgroundColor: 'var(--color-canvas)' }}>
              {['Tipo', 'Marca / Modelo', 'Cliente', 'N.º série', 'Próx. manutenção', ''].map(h => (
                <th key={h} className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {isLoading && Array.from({ length: 5 }).map((_, i) => (
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
                <td colSpan={6} className="px-5 py-14 text-center text-sm" style={{ color: 'var(--color-subtle)' }}>
                  {debouncedSearch || clientIdFilter
                    ? 'Nenhum equipamento encontrado.'
                    : <>Ainda não tens equipamentos.{' '}
                        <Link href="/dashboard/equipamentos/novo" className="font-medium underline underline-offset-2" style={{ color: 'var(--color-brand-600)' }}>
                          Regista o primeiro
                        </Link>
                      </>}
                </td>
              </tr>
            )}

            {data?.items.map(eq => (
              <tr
                key={eq.id}
                style={{ borderBottom: '1px solid var(--color-line)' }}
                onMouseEnter={e => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
                onMouseLeave={e => (e.currentTarget.style.backgroundColor = '')}
              >
                <td className="px-5 py-3.5">
                  <Link
                    href={`/dashboard/equipamentos/${eq.id}`}
                    className="text-sm font-medium transition-colors duration-150"
                    style={{ color: 'var(--color-ink)' }}
                    onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-brand-500)')}
                    onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-ink)')}
                  >
                    {eq.type}
                  </Link>
                </td>
                <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-muted)' }}>
                  {[eq.brand, eq.model].filter(Boolean).join(' ') || '—'}
                </td>
                <td className="px-5 py-3.5 text-sm">
                  <Link
                    href={`/dashboard/clientes/${eq.clientId}`}
                    style={{ color: 'var(--color-muted)' }}
                    onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-ink)')}
                    onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-muted)')}
                  >
                    {eq.clientName}
                  </Link>
                </td>
                <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-subtle)' }}>{eq.serialNumber ?? '—'}</td>
                <td className="px-5 py-3.5"><MaintenanceBadge date={eq.nextMaintenance} /></td>
                <td className="px-5 py-3.5">
                  <div className="flex justify-end gap-3">
                    <Link
                      href={`/dashboard/equipamentos/${eq.id}/editar`}
                      className="text-xs font-medium transition-colors duration-150"
                      style={{ color: 'var(--color-muted)' }}
                      onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-ink)')}
                      onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-muted)')}
                    >
                      Editar
                    </Link>
                    <button
                      onClick={() => handleDelete(eq.id, eq.type)}
                      className="text-xs font-medium transition-colors duration-150"
                      style={{ color: 'var(--color-subtle)' }}
                      onMouseEnter={e => (e.currentTarget.style.color = '#dc2626')}
                      onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-subtle)')}
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

      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between text-sm" style={{ color: 'var(--color-muted)' }}>
          <span>Página {data.page} de {data.totalPages}</span>
          <div className="flex gap-2">
            <button
              disabled={!data.hasPreviousPage}
              onClick={() => setPage(p => p - 1)}
              className="px-3 py-1.5 rounded-lg border text-sm font-medium disabled:opacity-40"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
            >
              ← Anterior
            </button>
            <button
              disabled={!data.hasNextPage}
              onClick={() => setPage(p => p + 1)}
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

export default function EquipamentosPage() {
  return (
    <Suspense fallback={<div className="h-8 w-48 rounded animate-pulse" style={{ backgroundColor: 'var(--color-line)' }} />}>
      <EquipamentosContent />
    </Suspense>
  )
}
