'use client'

import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { auditLogsApi, type AuditLogDto } from '@/lib/api/auditLogs'
import { getErrorMessage } from '@/lib/api/client'
import { useCanManage } from '@/hooks/useCanManage'
import { AccessDenied } from '@/components/shared/AccessDenied'

const ENTITY_TYPES = ['Client', 'Equipment', 'Intervention', 'Quote', 'TeamMember']

const ACTION_COLORS: Record<string, { bg: string; text: string }> = {
  Created: { bg: 'rgba(16,185,129,0.12)', text: '#34d399' },
  Updated: { bg: 'rgba(245,158,11,0.12)', text: '#f59e0b' },
  Deleted: { bg: 'rgba(239,68,68,0.12)',  text: '#f87171' },
}

const ENTITY_LABELS: Record<string, string> = {
  Client: 'Cliente',
  Equipment: 'Equipamento',
  Intervention: 'Intervenção',
  Quote: 'Orçamento',
  TeamMember: 'Membro',
}

function formatChanges(raw: string | null): string {
  if (!raw) return '—'
  try {
    const obj = JSON.parse(raw)
    return Object.entries(obj)
      .map(([k, v]) => `${k}: ${v}`)
      .join(' · ')
  } catch {
    return raw
  }
}

export default function AuditLogsPage() {
  const canManage = useCanManage()
  const today = new Date()
  const sevenDaysAgo = new Date(today)
  sevenDaysAgo.setDate(today.getDate() - 7)

  const [from, setFrom] = useState(sevenDaysAgo.toISOString().slice(0, 10))
  const [to, setTo]     = useState(today.toISOString().slice(0, 10))
  const [entityType, setEntityType] = useState('')
  const [page, setPage] = useState(1)
  const [expanded, setExpanded] = useState<number | null>(null)

  const { data, isLoading, error } = useQuery({
    queryKey: ['audit-logs', page, entityType, from, to],
    queryFn: () => auditLogsApi.get({
      page,
      pageSize: 50,
      entityType: entityType || undefined,
      from: from || undefined,
      to: to || undefined,
    }),
    enabled: canManage,
  })

  const inputCls = 'rounded-lg border px-3 py-2 text-sm outline-none transition-all duration-150 bg-[var(--color-canvas)] text-[var(--color-ink)] border-[var(--color-line-strong)]'

  if (!canManage) return <AccessDenied />

  return (
    <div className="max-w-5xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Audit Log</h1>
        <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
          Registo completo de todas as alterações feitas na conta.
        </p>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-end gap-3">
        <div>
          <label className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
            De
          </label>
          <input type="date" value={from} onChange={e => { setFrom(e.target.value); setPage(1) }} className={inputCls} />
        </div>
        <div>
          <label className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
            Até
          </label>
          <input type="date" value={to} onChange={e => { setTo(e.target.value); setPage(1) }} className={inputCls} />
        </div>
        <div>
          <label className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
            Entidade
          </label>
          <select
            value={entityType}
            onChange={e => { setEntityType(e.target.value); setPage(1) }}
            className={inputCls}
          >
            <option value="">Todas</option>
            {ENTITY_TYPES.map(t => (
              <option key={t} value={t}>{ENTITY_LABELS[t] ?? t}</option>
            ))}
          </select>
        </div>
      </div>

      {/* Error */}
      {error && (
        <p className="text-sm rounded-lg px-4 py-3" style={{ color: '#dc2626', backgroundColor: '#fef2f2' }}>
          {getErrorMessage(error)}
        </p>
      )}

      {/* Table */}
      {isLoading ? (
        <div className="space-y-2">
          {[1, 2, 3, 4, 5].map(i => (
            <div key={i} className="h-12 rounded-lg animate-pulse" style={{ backgroundColor: 'var(--color-line)' }} />
          ))}
        </div>
      ) : data && data.items.length === 0 ? (
        <div className="text-center py-16 rounded-xl border" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
          <p style={{ color: 'var(--color-muted)' }}>Nenhum registo encontrado para os filtros selecionados.</p>
        </div>
      ) : data && (
        <>
          <div className="rounded-xl border overflow-hidden" style={{ borderColor: 'var(--color-line)' }}>
            <table className="w-full text-sm">
              <thead>
                <tr style={{ backgroundColor: 'var(--color-canvas)', borderBottom: '1px solid var(--color-line)' }}>
                  {['Data/Hora', 'Ação', 'Entidade', 'Utilizador', 'Alterações'].map(h => (
                    <th key={h} className="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody style={{ backgroundColor: 'var(--color-card)' }}>
                {data.items.map((row: AuditLogDto, i) => {
                  const actionColor = ACTION_COLORS[row.action] ?? { bg: 'var(--color-canvas)', text: 'var(--color-muted)' }
                  const isOpen = expanded === row.id
                  return (
                    <tr
                      key={row.id}
                      style={{ borderTop: i > 0 ? '1px solid var(--color-line)' : undefined, cursor: row.changes ? 'pointer' : 'default' }}
                      onClick={() => row.changes && setExpanded(isOpen ? null : row.id)}
                    >
                      <td className="px-4 py-3 font-mono text-xs whitespace-nowrap" style={{ color: 'var(--color-muted)' }}>
                        {new Date(row.occurredAt).toLocaleString('pt-PT', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })}
                      </td>
                      <td className="px-4 py-3">
                        <span className="rounded-full px-2.5 py-0.5 text-xs font-semibold"
                          style={{ backgroundColor: actionColor.bg, color: actionColor.text }}>
                          {row.action}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-xs" style={{ color: 'var(--color-ink)' }}>
                        <span className="font-medium">{ENTITY_LABELS[row.entityType] ?? row.entityType}</span>
                        <span className="font-mono ml-1.5 text-[10px]" style={{ color: 'var(--color-muted)' }}>
                          {row.entityId.slice(0, 8)}…
                        </span>
                      </td>
                      <td className="px-4 py-3 text-xs" style={{ color: 'var(--color-muted)' }}>
                        {row.userEmail ?? '—'}
                      </td>
                      <td className="px-4 py-3 text-xs max-w-xs truncate" style={{ color: isOpen ? 'var(--color-ink)' : 'var(--color-muted)' }}>
                        {isOpen
                          ? <pre className="whitespace-pre-wrap text-[10px] font-mono" style={{ color: 'var(--color-ink)' }}>{JSON.stringify(JSON.parse(row.changes!), null, 2)}</pre>
                          : formatChanges(row.changes)
                        }
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {data.totalPages > 1 && (
            <div className="flex items-center justify-between text-sm">
              <p style={{ color: 'var(--color-muted)' }}>
                {data.totalCount} registos · página {data.page} de {data.totalPages}
              </p>
              <div className="flex gap-2">
                <button
                  onClick={() => setPage(p => Math.max(1, p - 1))}
                  disabled={!data.hasPreviousPage}
                  className="rounded-lg border px-3 py-1.5 text-xs font-medium transition-all duration-150 disabled:opacity-40"
                  style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
                >
                  ← Anterior
                </button>
                <button
                  onClick={() => setPage(p => p + 1)}
                  disabled={!data.hasNextPage}
                  className="rounded-lg border px-3 py-1.5 text-xs font-medium transition-all duration-150 disabled:opacity-40"
                  style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
                >
                  Seguinte →
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}
