'use client'

import Link from 'next/link'
import { useQuery } from '@tanstack/react-query'
import { dashboardApi } from '@/lib/api/dashboard'
import { QuoteStatusBadge } from '@/components/features/QuoteStatusBadge'
import { formatCurrency, formatDate } from '@/lib/utils/formatters'
import { useAuthStore } from '@/stores/authStore'

function StatCard({ label, value, sub, href }: { label: string; value: string | number; sub?: string; href?: string }) {
  const inner = (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <p className="text-sm font-medium text-gray-500">{label}</p>
      <p className="text-3xl font-bold text-gray-900 mt-1">{value}</p>
      {sub && <p className="text-xs text-gray-400 mt-1">{sub}</p>}
    </div>
  )
  if (href) return <Link href={href} className="hover:shadow-md transition-shadow">{inner}</Link>
  return inner
}

export default function DashboardPage() {
  const { user } = useAuthStore()
  const { data, isLoading } = useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: dashboardApi.getStats,
  })

  return (
    <div className="space-y-8">
      {/* Welcome */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">
          Olá, {user?.fullName?.split(' ')[0]} 👋
        </h1>
        <p className="text-sm text-gray-500 mt-1">Aqui está o resumo da tua atividade.</p>
      </div>

      {/* Stat cards */}
      {isLoading ? (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="bg-white rounded-lg border border-gray-200 p-6 animate-pulse">
              <div className="h-4 bg-gray-100 rounded w-2/3 mb-3" />
              <div className="h-8 bg-gray-100 rounded w-1/2" />
            </div>
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
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
            label="Receita faturada"
            value={formatCurrency(data?.totalRevenue ?? 0)}
            sub="orçamentos faturados"
          />
          <StatCard
            label="A aguardar faturação"
            value={formatCurrency(data?.pendingRevenue ?? 0)}
            sub={`${data?.acceptedQuotes ?? 0} orçamento${(data?.acceptedQuotes ?? 0) !== 1 ? 's' : ''} aceite${(data?.acceptedQuotes ?? 0) !== 1 ? 's' : ''}`}
          />
        </div>
      )}

      {/* Recent quotes */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-base font-semibold text-gray-900">Orçamentos recentes</h2>
          <Link href="/dashboard/orcamentos" className="text-sm text-blue-600 hover:text-blue-800">
            Ver todos →
          </Link>
        </div>

        <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
          {isLoading ? (
            <div className="p-6 space-y-3">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="h-4 bg-gray-100 rounded animate-pulse" />
              ))}
            </div>
          ) : !data?.recentQuotes.length ? (
            <div className="px-6 py-12 text-center text-sm text-gray-400">
              Ainda não tens orçamentos.{' '}
              <Link href="/dashboard/orcamentos/novo" className="text-blue-600 hover:underline">
                Cria o primeiro!
              </Link>
            </div>
          ) : (
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  {['Número', 'Cliente', 'Estado', 'Total', 'Data'].map(h => (
                    <th key={h} className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {data.recentQuotes.map(q => (
                  <tr key={q.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 text-sm font-medium text-gray-900">
                      <Link href={`/dashboard/orcamentos/${q.id}`} className="hover:text-blue-600 font-mono">
                        {q.number}
                      </Link>
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-500">{q.clientName}</td>
                    <td className="px-6 py-4">
                      <QuoteStatusBadge status={q.status} />
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-900 font-medium">{formatCurrency(q.total)}</td>
                    <td className="px-6 py-4 text-sm text-gray-500">{formatDate(q.createdAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {/* Quick actions */}
      <div>
        <h2 className="text-base font-semibold text-gray-900 mb-4">Ações rápidas</h2>
        <div className="flex flex-wrap gap-3">
          <Link
            href="/dashboard/orcamentos/novo"
            className="rounded-md bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-500 transition-colors"
          >
            + Novo Orçamento
          </Link>
          <Link
            href="/dashboard/clientes/novo"
            className="rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-semibold text-gray-700 hover:bg-gray-50 transition-colors"
          >
            + Novo Cliente
          </Link>
        </div>
      </div>
    </div>
  )
}
