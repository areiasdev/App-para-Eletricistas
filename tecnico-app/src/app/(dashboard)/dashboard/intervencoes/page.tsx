'use client'

import { useState, useEffect, Suspense } from 'react'
import { useSearchParams } from 'next/navigation'
import Link from 'next/link'
import { toast } from 'sonner'
import { useInterventions, useDeleteIntervention } from '@/hooks/useInterventions'
import { useDebouncedValue } from '@/hooks/useDebouncedValue'
import { useCanManage } from '@/hooks/useCanManage'
import { InterventionStatusBadge } from '@/components/features/InterventionStatusBadge'
import { formatDate, formatDateTime } from '@/lib/utils/formatters'
import { getErrorMessage } from '@/lib/api/client'
import type { InterventionStatus } from '@/types'
import type { InterventionListItem } from '@/lib/api/interventions'

// ── Calendar helpers ───────────────────────────────────────────────────────────
const WEEKDAYS = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb']
const MONTHS_PT = ['Janeiro','Fevereiro','Março','Abril','Maio','Junho','Julho','Agosto','Setembro','Outubro','Novembro','Dezembro']

function buildCalendarDays(year: number, month: number) {
  const firstDay = new Date(year, month, 1).getDay()
  const daysInMonth = new Date(year, month + 1, 0).getDate()
  const cells: (number | null)[] = []
  for (let i = 0; i < firstDay; i++) cells.push(null)
  for (let d = 1; d <= daysInMonth; d++) cells.push(d)
  while (cells.length % 7 !== 0) cells.push(null)
  return cells
}

function CalendarView({ items }: { items: InterventionListItem[] }) {
  const today = new Date()
  const [calYear, setCalYear] = useState(today.getFullYear())
  const [calMonth, setCalMonth] = useState(today.getMonth())

  const cells = buildCalendarDays(calYear, calMonth)

  const byDay: Record<number, InterventionListItem[]> = {}
  items.forEach(iv => {
    if (!iv.scheduledAt) return
    const d = new Date(iv.scheduledAt)
    if (d.getFullYear() === calYear && d.getMonth() === calMonth) {
      const day = d.getDate()
      if (!byDay[day]) byDay[day] = []
      byDay[day].push(iv)
    }
  })

  const prevMonth = () => {
    if (calMonth === 0) { setCalMonth(11); setCalYear(y => y - 1) }
    else setCalMonth(m => m - 1)
  }
  const nextMonth = () => {
    if (calMonth === 11) { setCalMonth(0); setCalYear(y => y + 1) }
    else setCalMonth(m => m + 1)
  }

  const statusColors: Record<InterventionStatus, string> = {
    Scheduled: 'var(--color-brand-500)',
    InProgress: '#2563eb',
    Completed: '#16a34a',
  }

  return (
    <div className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
      {/* Calendar header */}
      <div className="flex items-center justify-between px-5 py-4 border-b" style={{ borderColor: 'var(--color-line)' }}>
        <button onClick={prevMonth} className="rounded-lg border px-3 py-1.5 text-sm transition-all duration-150"
          style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-muted)', backgroundColor: 'var(--color-canvas)' }}>
          ←
        </button>
        <h2 className="text-sm font-semibold" style={{ color: 'var(--color-ink)' }}>
          {MONTHS_PT[calMonth]} {calYear}
        </h2>
        <button onClick={nextMonth} className="rounded-lg border px-3 py-1.5 text-sm transition-all duration-150"
          style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-muted)', backgroundColor: 'var(--color-canvas)' }}>
          →
        </button>
      </div>
      {/* Weekday headers */}
      <div className="grid grid-cols-7 border-b" style={{ borderColor: 'var(--color-line)', backgroundColor: 'var(--color-canvas)' }}>
        {WEEKDAYS.map(d => (
          <div key={d} className="py-2 text-center text-xs font-semibold uppercase tracking-wide"
            style={{ color: 'var(--color-muted)' }}>
            {d}
          </div>
        ))}
      </div>
      {/* Days grid */}
      <div className="grid grid-cols-7">
        {cells.map((day, i) => {
          const isToday = day !== null && calYear === today.getFullYear() && calMonth === today.getMonth() && day === today.getDate()
          const dayItems = day ? (byDay[day] ?? []) : []
          return (
            <div
              key={i}
              className="min-h-[80px] p-2 border-b border-r"
              style={{
                borderColor: 'var(--color-line)',
                backgroundColor: day === null ? 'var(--color-canvas)' : 'transparent',
              }}
            >
              {day !== null && (
                <>
                  <span
                    className="text-xs font-medium inline-flex w-6 h-6 items-center justify-center rounded-full mb-1"
                    style={{
                      backgroundColor: isToday ? 'var(--color-brand-500)' : 'transparent',
                      color: isToday ? 'var(--color-sidebar)' : 'var(--color-muted)',
                    }}
                  >
                    {day}
                  </span>
                  <div className="space-y-0.5">
                    {dayItems.slice(0, 3).map(iv => (
                      <Link
                        key={iv.id}
                        href={`/dashboard/intervencoes/${iv.id}`}
                        className="block rounded px-1.5 py-0.5 text-xs truncate leading-tight transition-opacity duration-150 hover:opacity-80"
                        style={{ backgroundColor: statusColors[iv.status] + '22', color: statusColors[iv.status], border: `1px solid ${statusColors[iv.status]}44` }}
                        title={iv.title}
                      >
                        {iv.title}
                      </Link>
                    ))}
                    {dayItems.length > 3 && (
                      <span className="text-xs" style={{ color: 'var(--color-subtle)' }}>+{dayItems.length - 3} mais</span>
                    )}
                  </div>
                </>
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}

const statusOptions: { value: InterventionStatus | ''; label: string }[] = [
  { value: '', label: 'Todos' },
  { value: 'Scheduled', label: 'Agendadas' },
  { value: 'InProgress', label: 'Em curso' },
  { value: 'Completed', label: 'Concluídas' },
]

function IntervencoesContent() {
  const canManage = useCanManage()
  const searchParams = useSearchParams()
  const clientIdFilter = searchParams.get('clientId') ?? undefined
  const statusParam = (searchParams.get('status') ?? '') as InterventionStatus | ''

  const [view, setView] = useState<'list' | 'calendar'>('list')
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search, 300)
  const [status, setStatus] = useState<InterventionStatus | ''>(statusParam)
  const [page, setPage] = useState(1)

  // Re-sync when the URL's ?status= changes (e.g. clicking a status-filtered dashboard
  // link while already on this page) — a plain useState initializer only runs once on mount.
  useEffect(() => {
    setStatus(statusParam)
  }, [statusParam])

  const { data, isLoading, isError, error } = useInterventions({
    search: debouncedSearch || undefined,
    status: status || undefined,
    clientId: clientIdFilter,
    page,
    pageSize: view === 'calendar' ? 200 : 20,
  })

  const deleteIntervention = useDeleteIntervention()

  const handleSearch = (value: string) => {
    setSearch(value)
    setPage(1)
  }

  const handleDelete = (id: string, title: string) => {
    if (!confirm(`Apagar a intervenção "${title}"?`)) return
    deleteIntervention.mutate(id, {
      onSuccess: () => toast.success('Intervenção apagada.'),
      onError: (err) => toast.error(getErrorMessage(err)),
    })
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <div>
          <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Intervenções</h1>
          <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
            {data ? `${data.totalCount} intervenção${data.totalCount !== 1 ? 'ões' : ''}` : '\u00a0'}
          </p>
        </div>
        <div className="flex items-center gap-2">
          {/* View toggle */}
          <div className="flex rounded-lg border overflow-hidden" style={{ borderColor: 'var(--color-line-strong)' }}>
            <button
              onClick={() => setView('list')}
              className="px-3 py-2 text-xs font-medium transition-all duration-150"
              style={{
                backgroundColor: view === 'list' ? 'var(--color-brand-500)' : 'var(--color-canvas)',
                color: view === 'list' ? 'var(--color-sidebar)' : 'var(--color-muted)',
              }}
            >
              Lista
            </button>
            <button
              onClick={() => setView('calendar')}
              className="px-3 py-2 text-xs font-medium transition-all duration-150 border-l"
              style={{
                borderColor: 'var(--color-line-strong)',
                backgroundColor: view === 'calendar' ? 'var(--color-brand-500)' : 'var(--color-canvas)',
                color: view === 'calendar' ? 'var(--color-sidebar)' : 'var(--color-muted)',
              }}
            >
              Calendário
            </button>
          </div>
          <Link
            href="/dashboard/intervencoes/novo"
            className="inline-flex items-center gap-2 rounded-lg px-4 py-2.5 text-sm font-semibold transition-all duration-150 hover:brightness-110 active:scale-[0.99]"
            style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
          >
            + Nova Intervenção
          </Link>
        </div>
      </div>

      {/* Filters */}
      <div className="flex gap-3 flex-wrap items-center">
        <input
          type="search"
          placeholder="Pesquisar por título ou cliente..."
          value={search}
          onChange={(e) => handleSearch(e.target.value)}
          className="rounded-lg border px-3 py-2 text-sm outline-none transition-all duration-150 w-64"
          style={{ borderColor: 'var(--color-line-strong)', backgroundColor: 'var(--color-card)', color: 'var(--color-ink)' }}
          onFocus={(e) => { e.currentTarget.style.borderColor = 'var(--color-brand-500)'; e.currentTarget.style.boxShadow = '0 0 0 3px rgba(245,158,11,0.12)' }}
          onBlur={(e) => { e.currentTarget.style.borderColor = 'var(--color-line-strong)'; e.currentTarget.style.boxShadow = 'none' }}
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
        {clientIdFilter && (
          <Link
            href="/dashboard/intervencoes"
            className="text-xs font-medium rounded-lg px-3 py-2 border transition-colors duration-150"
            style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-muted)', backgroundColor: 'var(--color-canvas)' }}
          >
            × Limpar filtro de cliente
          </Link>
        )}
      </div>

      {/* Error */}
      {isError && (
        <p className="text-sm rounded-lg px-4 py-3" style={{ color: '#dc2626', backgroundColor: '#fef2f2' }}>
          {getErrorMessage(error)}
        </p>
      )}

      {/* Calendar view */}
      {view === 'calendar' && (
        <CalendarView items={data?.items ?? []} />
      )}

      {/* Table */}
      {view === 'list' && (<>
      <div className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
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
                  {debouncedSearch || status || clientIdFilter
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
                  <div className="flex items-center gap-2 flex-wrap">
                    <InterventionStatusBadge status={iv.status} />
                    {iv.status === 'Scheduled' && iv.scheduledAt && new Date(iv.scheduledAt) < new Date() && (
                      <span
                        className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium"
                        style={{ backgroundColor: '#fef3c7', color: '#92400e', border: '1px solid #fcd34d' }}
                        title="Data agendada já passou"
                      >
                        ⚠ Em atraso
                      </span>
                    )}
                  </div>
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
                    {canManage && (
                      <button
                        onClick={() => handleDelete(iv.id, iv.title)}
                        className="text-xs font-medium transition-colors duration-150"
                        style={{ color: 'var(--color-subtle)' }}
                        onMouseEnter={(e) => (e.currentTarget.style.color = '#dc2626')}
                        onMouseLeave={(e) => (e.currentTarget.style.color = 'var(--color-subtle)')}
                      >
                        Apagar
                      </button>
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
              className="px-3 py-1.5 rounded-lg border text-xs font-medium disabled:opacity-40 transition-colors duration-150"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
            >
              ← Anterior
            </button>
            <button
              disabled={!data.hasNextPage}
              onClick={() => setPage((p) => p + 1)}
              className="px-3 py-1.5 rounded-lg border text-xs font-medium disabled:opacity-40 transition-colors duration-150"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
            >
              Próxima →
            </button>
          </div>
        </div>
      )}
      </>)}
    </div>
  )
}

export default function IntervencoesPage() {
  return (
    <Suspense fallback={<div className="h-8 w-48 rounded animate-pulse" style={{ backgroundColor: 'var(--color-line)' }} />}>
      <IntervencoesContent />
    </Suspense>
  )
}
