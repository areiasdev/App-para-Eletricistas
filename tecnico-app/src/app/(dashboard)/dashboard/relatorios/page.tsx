'use client'

import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { reportsApi, type ProfitabilityByTechnician, type ProfitabilityByClient } from '@/lib/api/reports'
import { getErrorMessage } from '@/lib/api/client'

function fmt(n: number) {
  return n.toFixed(2).replace('.', ',') + ' €'
}

function margin(revenue: number, cost: number) {
  if (revenue === 0) return '—'
  const m = ((revenue - cost) / revenue) * 100
  return m.toFixed(1) + '%'
}

export default function RelatoriosPage() {
  const today = new Date()
  const threeMonthsAgo = new Date(today)
  threeMonthsAgo.setMonth(today.getMonth() - 3)

  const [from, setFrom] = useState(threeMonthsAgo.toISOString().slice(0, 10))
  const [to, setTo] = useState(today.toISOString().slice(0, 10))
  const [tab, setTab] = useState<'tech' | 'client'>('tech')

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['profitability', from, to],
    queryFn: () => reportsApi.getProfitability({ from, to }),
  })

  const inputCls = 'rounded-lg border px-3 py-2 text-sm outline-none transition-all duration-150 bg-[var(--color-canvas)] text-[var(--color-ink)] border-[var(--color-line-strong)]'

  return (
    <div className="max-w-5xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Relatórios de rentabilidade</h1>
        <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
          Analisa o desempenho por técnico e por cliente.
        </p>
      </div>

      {/* Date range filter */}
      <div className="flex flex-wrap items-end gap-3">
        <div>
          <label className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
            De
          </label>
          <input type="date" value={from} onChange={e => setFrom(e.target.value)} className={inputCls} />
        </div>
        <div>
          <label className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
            Até
          </label>
          <input type="date" value={to} onChange={e => setTo(e.target.value)} className={inputCls} />
        </div>
        <button
          onClick={() => refetch()}
          className="rounded-lg px-5 py-2 text-sm font-semibold transition-all duration-150 hover:brightness-110"
          style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
        >
          Atualizar
        </button>
      </div>

      {/* Summary cards */}
      {data && (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
          {[
            { label: 'Custo materiais', value: fmt(data.totalMaterialsCost) },
            { label: 'Receita orçamentada', value: fmt(data.totalQuotedRevenue) },
            { label: 'Margem bruta', value: margin(data.totalQuotedRevenue, data.totalMaterialsCost) },
            {
              label: 'Total intervenções',
              value: String(data.byTechnician.reduce((s, t) => s + t.totalInterventions, 0))
            },
          ].map((card) => (
            <div key={card.label} className="rounded-xl border p-4 space-y-1"
              style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
              <p className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
                {card.label}
              </p>
              <p className="text-lg font-bold font-mono" style={{ color: 'var(--color-ink)' }}>{card.value}</p>
            </div>
          ))}
        </div>
      )}

      {/* Tab selector */}
      <div className="flex gap-1 rounded-lg p-1 w-fit" style={{ backgroundColor: 'var(--color-canvas)', border: '1px solid var(--color-line)' }}>
        {(['tech', 'client'] as const).map((t) => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className="rounded-md px-4 py-1.5 text-xs font-semibold transition-all duration-150"
            style={{
              backgroundColor: tab === t ? 'var(--color-brand-500)' : 'transparent',
              color: tab === t ? 'var(--color-sidebar)' : 'var(--color-muted)',
            }}
          >
            {t === 'tech' ? 'Por técnico' : 'Por cliente'}
          </button>
        ))}
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
          {[1, 2, 3, 4].map(i => (
            <div key={i} className="h-12 rounded-lg animate-pulse" style={{ backgroundColor: 'var(--color-line)' }} />
          ))}
        </div>
      ) : data && (
        <div className="rounded-xl border overflow-hidden" style={{ borderColor: 'var(--color-line)' }}>
          <table className="w-full text-sm">
            <thead>
              <tr style={{ backgroundColor: 'var(--color-canvas)', borderBottom: '1px solid var(--color-line)' }}>
                <th className="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
                  {tab === 'tech' ? 'Técnico' : 'Cliente'}
                </th>
                <th className="text-right px-4 py-3 text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>Total</th>
                <th className="text-right px-4 py-3 text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>Concluídas</th>
                <th className="text-right px-4 py-3 text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>Materiais</th>
                <th className="text-right px-4 py-3 text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>Receita</th>
                <th className="text-right px-4 py-3 text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>Margem</th>
              </tr>
            </thead>
            <tbody style={{ backgroundColor: 'var(--color-card)' }}>
              {tab === 'tech'
                ? data.byTechnician.map((row: ProfitabilityByTechnician, i) => (
                  <tr key={row.userId} style={{ borderTop: i > 0 ? '1px solid var(--color-line)' : undefined }}>
                    <td className="px-4 py-3 font-medium" style={{ color: 'var(--color-ink)' }}>{row.technicianName}</td>
                    <td className="px-4 py-3 text-right font-mono text-xs" style={{ color: 'var(--color-muted)' }}>{row.totalInterventions}</td>
                    <td className="px-4 py-3 text-right font-mono text-xs" style={{ color: 'var(--color-muted)' }}>{row.completedInterventions}</td>
                    <td className="px-4 py-3 text-right font-mono text-xs" style={{ color: '#f59e0b' }}>{fmt(row.materialsCost)}</td>
                    <td className="px-4 py-3 text-right font-mono text-xs font-semibold" style={{ color: 'var(--color-ink)' }}>{fmt(row.quotedRevenue)}</td>
                    <td className="px-4 py-3 text-right font-mono text-xs font-semibold" style={{ color: '#34d399' }}>{margin(row.quotedRevenue, row.materialsCost)}</td>
                  </tr>
                ))
                : data.byClient.map((row: ProfitabilityByClient, i) => (
                  <tr key={row.clientId} style={{ borderTop: i > 0 ? '1px solid var(--color-line)' : undefined }}>
                    <td className="px-4 py-3 font-medium" style={{ color: 'var(--color-ink)' }}>{row.clientName}</td>
                    <td className="px-4 py-3 text-right font-mono text-xs" style={{ color: 'var(--color-muted)' }}>{row.totalInterventions}</td>
                    <td className="px-4 py-3 text-right font-mono text-xs" style={{ color: 'var(--color-muted)' }}>{row.completedInterventions}</td>
                    <td className="px-4 py-3 text-right font-mono text-xs" style={{ color: '#f59e0b' }}>{fmt(row.materialsCost)}</td>
                    <td className="px-4 py-3 text-right font-mono text-xs font-semibold" style={{ color: 'var(--color-ink)' }}>{fmt(row.quotedRevenue)}</td>
                    <td className="px-4 py-3 text-right font-mono text-xs font-semibold" style={{ color: '#34d399' }}>{margin(row.quotedRevenue, row.materialsCost)}</td>
                  </tr>
                ))
              }
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
