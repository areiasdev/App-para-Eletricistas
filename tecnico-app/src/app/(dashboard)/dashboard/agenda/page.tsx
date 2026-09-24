'use client'

import { useMemo, useState } from 'react'
import Link from 'next/link'
import { useInterventions } from '@/hooks/useInterventions'
import { useTeam } from '@/hooks/useTeam'
import { InterventionStatusBadge } from '@/components/features/InterventionStatusBadge'
import type { InterventionListItem } from '@/lib/api/interventions'

// Week agenda: what each technician has on which day. Scheduled times are wall-clock values
// (no time zone), so dates are built and compared as local "YYYY-MM-DD" strings.

const DAY_MS = 24 * 60 * 60 * 1000

function startOfWeek(date: Date) {
  const d = new Date(date.getFullYear(), date.getMonth(), date.getDate())
  const weekday = (d.getDay() + 6) % 7 // Monday = 0
  return new Date(d.getTime() - weekday * DAY_MS)
}

const isoDay = (d: Date) =>
  `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`

const dayLabel = new Intl.DateTimeFormat('pt-PT', { weekday: 'long', day: 'numeric', month: 'long' })
const rangeLabel = new Intl.DateTimeFormat('pt-PT', { day: 'numeric', month: 'short' })

export default function AgendaPage() {
  const [weekStart, setWeekStart] = useState(() => startOfWeek(new Date()))
  const [technician, setTechnician] = useState('')
  const { data: team = [] } = useTeam()

  const days = useMemo(() => Array.from({ length: 7 }, (_, i) => new Date(weekStart.getTime() + i * DAY_MS)), [weekStart])
  const weekEnd = days[6]

  const { data, isLoading, isError } = useInterventions({
    from: `${isoDay(weekStart)}T00:00:00`,
    to: `${isoDay(new Date(weekStart.getTime() + 7 * DAY_MS))}T00:00:00`,
    assignedToUserId: technician || undefined,
    pageSize: 100,
  })

  const byDay = useMemo(() => {
    const map = new Map<string, InterventionListItem[]>()
    for (const item of data?.items ?? []) {
      if (!item.scheduledAt) continue
      const key = item.scheduledAt.slice(0, 10)
      map.set(key, [...(map.get(key) ?? []), item])
    }
    return map
  }, [data])

  const today = isoDay(new Date())
  const shiftWeek = (weeks: number) => setWeekStart((w) => new Date(w.getTime() + weeks * 7 * DAY_MS))

  return (
    <div className="space-y-6 max-w-4xl">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Agenda</h1>
          <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
            {rangeLabel.format(weekStart)} – {rangeLabel.format(weekEnd)}
            {data ? ` · ${data.totalCount} intervenç${data.totalCount === 1 ? 'ão' : 'ões'}` : ''}
          </p>
        </div>
        <Link
          href="/dashboard/intervencoes/novo"
          className="rounded-lg px-4 py-2 text-sm font-semibold"
          style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-on-brand)' }}
        >
          + Nova intervenção
        </Link>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <div className="inline-flex rounded-lg border overflow-hidden" style={{ borderColor: 'var(--color-line-strong)' }}>
          <button onClick={() => shiftWeek(-1)} aria-label="Semana anterior" className="px-3 py-2 text-sm" style={{ color: 'var(--color-ink)' }}>←</button>
          <button onClick={() => setWeekStart(startOfWeek(new Date()))} className="px-3 py-2 text-sm font-medium border-x"
            style={{ color: 'var(--color-ink)', borderColor: 'var(--color-line-strong)' }}>
            Esta semana
          </button>
          <button onClick={() => shiftWeek(1)} aria-label="Semana seguinte" className="px-3 py-2 text-sm" style={{ color: 'var(--color-ink)' }}>→</button>
        </div>
        {team.length > 0 && (
          <select
            value={technician}
            onChange={(e) => setTechnician(e.target.value)}
            aria-label="Filtrar por técnico"
            className="form-input w-auto"
            style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)' }}
          >
            <option value="">Todos os técnicos</option>
            {team.filter((m) => m.isAccepted).map((m) => (
              <option key={m.memberId} value={m.memberId}>{m.fullName || m.email}</option>
            ))}
          </select>
        )}
      </div>

      {isError && (
        <p className="text-sm rounded-xl px-4 py-3" style={{ color: 'var(--color-danger-600)', backgroundColor: 'var(--color-danger-50)' }}>
          Erro ao carregar a agenda.
        </p>
      )}

      <div className="space-y-3">
        {days.map((day) => {
          const key = isoDay(day)
          const items = byDay.get(key) ?? []
          const isToday = key === today
          const isPast = key < today
          return (
            <section key={key} className="rounded-xl border overflow-hidden"
              style={{
                backgroundColor: 'var(--color-card)',
                borderColor: isToday ? 'var(--color-brand-500)' : 'var(--color-line)',
                opacity: isPast && items.length === 0 ? 0.6 : 1,
              }}>
              <header className="px-4 py-2.5 flex items-center justify-between"
                style={{ backgroundColor: isToday ? 'var(--color-brand-50)' : 'var(--color-canvas)', borderBottom: items.length ? '1px solid var(--color-line)' : undefined }}>
                <h2 className="text-sm font-semibold first-letter:uppercase" style={{ color: 'var(--color-ink)' }}>
                  {dayLabel.format(day)}{isToday && ' · hoje'}
                </h2>
                <span className="text-xs" style={{ color: 'var(--color-subtle)' }}>
                  {isLoading ? '…' : items.length === 0 ? 'livre' : `${items.length}`}
                </span>
              </header>
              {items.length > 0 && (
                <ul className="divide-y divide-[var(--color-line)]">
                  {items.map((item) => (
                    <li key={item.id}>
                      <Link href={`/dashboard/intervencoes/${item.id}`} className="flex items-center gap-4 px-4 py-3 hover:bg-[var(--color-canvas)]">
                        <span className="w-12 shrink-0 text-sm font-mono font-semibold" style={{ color: 'var(--color-ink)' }}>
                          {item.scheduledAt!.slice(11, 16)}
                        </span>
                        <span className="min-w-0 flex-1">
                          <span className="block text-sm font-medium truncate" style={{ color: 'var(--color-ink)' }}>{item.title}</span>
                          <span className="block text-xs truncate" style={{ color: 'var(--color-muted)' }}>
                            {item.clientName}{item.assignedToName ? ` · ${item.assignedToName}` : ''}
                          </span>
                        </span>
                        <InterventionStatusBadge status={item.status} />
                      </Link>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          )
        })}
      </div>
    </div>
  )
}
