'use client'

import Link from 'next/link'
import { useQuery } from '@tanstack/react-query'
import { dashboardApi } from '@/lib/api/dashboard'
import { QuoteStatusBadge } from '@/components/features/QuoteStatusBadge'
import { formatCurrency, formatDate } from '@/lib/utils/formatters'
import { useAuthStore } from '@/stores/authStore'

function StatCard({
  label, value, sub, href, accent = false,
}: {
  label: string
  value: string | number
  sub?: string
  href?: string
  accent?: boolean
}) {
  const inner = (
    <div
      className="rounded-xl border p-5 transition-all duration-150 hover:shadow-sm group"
      style={{
        backgroundColor: accent ? 'var(--color-brand-500)' : 'white',
        borderColor: accent ? 'var(--color-brand-600)' : 'var(--color-line)',
      }}
    >
      <p className="text-xs font-semibold uppercase tracking-wide" style={{ color: accent ? 'rgba(23,23,26,0.65)' : 'var(--color-muted)' }}>
        {label}
      </p>
      <p className="text-3xl font-bold mt-1.5 leading-none" style={{ color: accent ? 'var(--color-sidebar)' : 'var(--color-ink)' }}>
        {value}
      </p>
      {sub && (
        <p className="text-xs mt-2" style={{ color: accent ? 'rgba(23,23,26,0.55)' : 'var(--color-subtle)' }}>
          {sub}
        </p>
      )}
    </div>
  )

  if (href) {
    return (
      <Link href={href} className="block group">
        {inner}
      </Link>
    )
  }
  return inner
}

export default function DashboardPage() {
  const { user } = useAuthStore()
  const { data, isLoading } = useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: dashboardApi.getStats,
  })

  const hour = new Date().getHours()
  const greeting = hour < 12 ? 'Bom dia' : hour < 18 ? 'Boa tarde' : 'Boa noite'

  return (
    <div className="space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>
          {greeting}, {user?.fullName?.split(' ')[0]} 👋
        </h1>
        <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
          {formatDate(new Date().toISOString())} · Aqui está o resumo da tua atividade
        </p>
      </div>

      {/* Stats */}
      {isLoading ? (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 stagger">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="rounded-xl border p-5 animate-pulse" style={{ backgroundColor: 'white', borderColor: 'var(--color-line)' }}>
              <div className="h-3 rounded w-2/3 mb-4" style={{ backgroundColor: 'var(--color-line)' }} />
              <div className="h-8 rounded w-1/2" style={{ backgroundColor: 'var(--color-line)' }} />
            </div>
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 stagger">
          <StatCard
            label="Clientes"
            value={data?.totalClients ?? 0}
            href="/dashboard/clientes"
          />
          <StatCard
            label="Orçamentos"
            value={data?.totalQuotes ?? 0}
            sub={`${data?.draftQuotes ?? 0} rascunhos · ${data?.sentQuotes ?? 0} enviados`}
            href="/dashboard/orcamentos"
          />
          <StatCard
            label="A receber"
            value={formatCurrency(data?.pendingRevenue ?? 0)}
            sub={`${data?.acceptedQuotes ?? 0} aceite${(data?.acceptedQuotes ?? 0) !== 1 ? 's' : ''}`}
            accent
          />
          <StatCard
            label="Receita total"
            value={formatCurrency(data?.totalRevenue ?? 0)}
            sub="orçamentos faturados"
          />
        </div>
      )}

      {/* Recent quotes */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-sm font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
            Orçamentos recentes
          </h2>
          <Link href="/dashboard/orcamentos" className="text-xs font-medium transition-colors" style={{ color: 'var(--color-brand-600)' }}>
            Ver todos →
          </Link>
        </div>

        <div className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'white', borderColor: 'var(--color-line)' }}>
          {isLoading ? (
            <div className="p-6 space-y-4">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="h-4 rounded animate-pulse" style={{ backgroundColor: 'var(--color-line)' }} />
              ))}
            </div>
          ) : !data?.recentQuotes.length ? (
            <div className="px-6 py-14 text-center">
              <p className="text-sm" style={{ color: 'var(--color-subtle)' }}>
                Ainda não tens orçamentos.{' '}
                <Link href="/dashboard/orcamentos/novo" className="font-medium underline underline-offset-2" style={{ color: 'var(--color-brand-600)' }}>
                  Cria o primeiro
                </Link>
              </p>
            </div>
          ) : (
            <table className="min-w-full">
              <thead>
                <tr style={{ borderBottom: '1px solid var(--color-line)' }}>
                  {['Número', 'Cliente', 'Estado', 'Total', 'Data'].map(h => (
                    <th
                      key={h}
                      className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide"
                      style={{ color: 'var(--color-subtle)' }}
                    >
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="stagger">
                {data.recentQuotes.map(q => (
                  <tr
                    key={q.id}
                    className="transition-colors duration-100"
                    style={{ borderBottom: '1px solid var(--color-line)' }}
                    onMouseEnter={e => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
                    onMouseLeave={e => (e.currentTarget.style.backgroundColor = '')}
                  >
                    <td className="px-5 py-3.5">
                      <Link
                        href={`/dashboard/orcamentos/${q.id}`}
                        className="text-sm font-medium transition-colors"
                        style={{ fontFamily: 'var(--font-jetbrains), monospace', color: 'var(--color-ink)' }}
                      >
                        {q.number}
                      </Link>
                    </td>
                    <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-muted)' }}>{q.clientName}</td>
                    <td className="px-5 py-3.5"><QuoteStatusBadge status={q.status} /></td>
                    <td className="px-5 py-3.5 text-sm font-semibold" style={{ color: 'var(--color-ink)' }}>{formatCurrency(q.total)}</td>
                    <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-subtle)' }}>{formatDate(q.createdAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {/* Quick actions */}
      <div>
        <h2 className="text-sm font-semibold uppercase tracking-wide mb-4" style={{ color: 'var(--color-muted)' }}>
          Ações rápidas
        </h2>
        <div className="flex flex-wrap gap-3">
          <Link
            href="/dashboard/orcamentos/novo"
            className="rounded-lg px-4 py-2 text-sm font-semibold transition-all duration-150 hover:brightness-110 active:scale-[0.99]"
            style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
          >
            + Novo Orçamento
          </Link>
          <Link
            href="/dashboard/clientes/novo"
            className="rounded-lg border px-4 py-2 text-sm font-semibold transition-all duration-150 hover:bg-white"
            style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'transparent' }}
          >
            + Novo Cliente
          </Link>
          <Link
            href="/dashboard/equipamentos/novo"
            className="rounded-lg border px-4 py-2 text-sm font-semibold transition-all duration-150 hover:bg-white"
            style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'transparent' }}
          >
            + Novo Equipamento
          </Link>
        </div>
      </div>
    </div>
  )
}
