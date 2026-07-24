'use client'

import { useEffect } from 'react'
import { useRouter } from 'next/navigation'
import { useQuery } from '@tanstack/react-query'
import { portal, setPortalToken } from '@/lib/api/portal'
import { usePortalStore } from '@/stores/portalStore'
import { formatDate } from '@/lib/utils/formatters'

const STATUS_COLORS: Record<string, { bg: string; text: string }> = {
  Scheduled:   { bg: 'rgba(245,158,11,0.12)',  text: '#f59e0b' },
  InProgress:  { bg: 'rgba(59,130,246,0.12)',   text: '#60a5fa' },
  Completed:   { bg: 'rgba(16,185,129,0.12)',   text: '#34d399' },
  Cancelled:   { bg: 'rgba(239,68,68,0.12)',    text: '#f87171' },
  Draft:       { bg: 'rgba(255,255,255,0.06)',  text: 'rgba(255,255,255,0.4)' },
  Sent:        { bg: 'rgba(59,130,246,0.12)',   text: '#60a5fa' },
  Accepted:    { bg: 'rgba(16,185,129,0.12)',   text: '#34d399' },
  Rejected:    { bg: 'rgba(239,68,68,0.12)',    text: '#f87171' },
}

const STATUS_LABELS: Record<string, string> = {
  Scheduled: 'Agendada', InProgress: 'Em curso', Completed: 'Concluída', Cancelled: 'Cancelada',
  Draft: 'Rascunho', Sent: 'Enviado', Accepted: 'Aceite', Rejected: 'Rejeitado',
}

const INTERVENTION_TYPE_LABELS: Record<string, string> = {
  Maintenance: 'Manutenção', Repair: 'Reparação', Installation: 'Instalação', Inspection: 'Inspeção',
}

function fmt(n: number) {
  return n.toFixed(2).replace('.', ',') + ' €'
}

function Badge({ status }: { status: string }) {
  const c = STATUS_COLORS[status] ?? { bg: 'var(--color-canvas)', text: 'var(--color-muted)' }
  return (
    <span className="rounded-full px-2.5 py-0.5 text-xs font-semibold"
      style={{ backgroundColor: c.bg, color: c.text }}>
      {STATUS_LABELS[status] ?? status}
    </span>
  )
}

export default function PortalDashboardPage() {
  const router = useRouter()
  const { accessToken, clientName, clearPortal } = usePortalStore()

  useEffect(() => {
    if (!accessToken) { router.replace('/portal/login'); return }
    setPortalToken(accessToken)
  }, [accessToken, router])

  const { data: client } = useQuery({
    queryKey: ['portal-me'],
    queryFn: portal.me,
    enabled: !!accessToken,
  })

  const { data: equipment = [] } = useQuery({
    queryKey: ['portal-equipment'],
    queryFn: portal.equipment,
    enabled: !!accessToken,
  })

  const { data: interventions = [] } = useQuery({
    queryKey: ['portal-interventions'],
    queryFn: portal.interventions,
    enabled: !!accessToken,
  })

  const { data: quotes = [] } = useQuery({
    queryKey: ['portal-quotes'],
    queryFn: portal.quotes,
    enabled: !!accessToken,
  })

  if (!accessToken) return null

  const handleLogout = () => {
    portal.logout().catch(() => {}).finally(() => {
      setPortalToken(null)
      clearPortal()
      router.push('/portal/login')
    })
  }

  return (
    <div className="min-h-screen" style={{ backgroundColor: 'var(--color-canvas)' }}>
      {/* Header */}
      <header className="border-b px-6 py-4 flex items-center justify-between"
        style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <div className="flex items-center gap-2.5">
          <span className="flex items-center justify-center w-7 h-7 rounded-md text-sm font-bold"
            style={{ backgroundColor: 'var(--color-brand-500)', color: '#1c1917' }}>
            T
          </span>
          <span className="text-sm font-semibold" style={{ color: 'var(--color-ink)' }}>Portal do cliente</span>
        </div>
        <div className="flex items-center gap-4">
          <span className="text-sm" style={{ color: 'var(--color-muted)' }}>{clientName}</span>
          <button onClick={handleLogout} className="text-xs" style={{ color: 'var(--color-muted)' }}>
            Terminar sessão →
          </button>
        </div>
      </header>

      <main className="max-w-4xl mx-auto px-6 py-8 space-y-10">

        {/* Client info */}
        {client && (
          <section className="space-y-4">
            <h2 className="text-lg font-bold" style={{ color: 'var(--color-ink)' }}>Os seus dados</h2>
            <div className="rounded-xl border divide-y overflow-hidden"
              style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
              {[
                { label: 'Nome', value: client.name },
                ...(client.email ? [{ label: 'Email', value: client.email }] : []),
                ...(client.phone ? [{ label: 'Telefone', value: client.phone }] : []),
                ...(client.address ? [{ label: 'Morada', value: client.address }] : []),
              ].map(({ label, value }) => (
                <div key={label} className="flex px-5 py-3 gap-6" style={{ borderColor: 'var(--color-line)' }}>
                  <span className="text-sm font-medium w-24 shrink-0" style={{ color: 'var(--color-muted)' }}>{label}</span>
                  <span className="text-sm" style={{ color: 'var(--color-ink)' }}>{value}</span>
                </div>
              ))}
            </div>
          </section>
        )}

        {/* Equipment */}
        {equipment.length > 0 && (
          <section className="space-y-4">
            <h2 className="text-lg font-bold" style={{ color: 'var(--color-ink)' }}>Equipamentos</h2>
            <div className="grid sm:grid-cols-2 gap-4">
              {equipment.map((e) => (
                <div key={e.id} className="rounded-xl border p-5 space-y-3"
                  style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
                  <p className="font-semibold" style={{ color: 'var(--color-ink)' }}>{e.name}</p>
                  {(e.brand || e.model) && (
                    <p className="text-sm" style={{ color: 'var(--color-muted)' }}>
                      {[e.brand, e.model].filter(Boolean).join(' · ')}
                    </p>
                  )}
                  {e.serialNumber && (
                    <p className="text-xs font-mono" style={{ color: 'var(--color-muted)' }}>S/N: {e.serialNumber}</p>
                  )}
                  {e.nextMaintenanceDate && (
                    <div className="flex items-center gap-1.5 text-xs"
                      style={{ color: new Date(e.nextMaintenanceDate) < new Date() ? '#f87171' : '#f59e0b' }}>
                      <svg width="12" height="12" viewBox="0 0 12 12" fill="none">
                        <circle cx="6" cy="6" r="5" stroke="currentColor" strokeWidth="1.2"/>
                        <path d="M6 3v3l2 1.5" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round"/>
                      </svg>
                      Próxima manutenção: {formatDate(e.nextMaintenanceDate)}
                    </div>
                  )}
                </div>
              ))}
            </div>
          </section>
        )}

        {/* Interventions */}
        {interventions.length > 0 && (
          <section className="space-y-4">
            <h2 className="text-lg font-bold" style={{ color: 'var(--color-ink)' }}>Intervenções</h2>
            <div className="rounded-xl border overflow-hidden" style={{ borderColor: 'var(--color-line)' }}>
              <table className="w-full text-sm">
                <thead>
                  <tr style={{ backgroundColor: 'var(--color-canvas)', borderBottom: '1px solid var(--color-line)' }}>
                    {['Data', 'Tipo', 'Estado', 'Técnico'].map(h => (
                      <th key={h} className="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wide"
                        style={{ color: 'var(--color-muted)' }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody style={{ backgroundColor: 'var(--color-card)' }}>
                  {interventions.map((row, i) => (
                    <tr key={row.id} style={{ borderTop: i > 0 ? '1px solid var(--color-line)' : undefined }}>
                      <td className="px-4 py-3 text-xs" style={{ color: 'var(--color-muted)' }}>
                        {row.scheduledAt ? formatDate(row.scheduledAt) : '—'}
                      </td>
                      <td className="px-4 py-3 text-xs" style={{ color: 'var(--color-ink)' }}>
                        {INTERVENTION_TYPE_LABELS[row.type] ?? row.type}
                      </td>
                      <td className="px-4 py-3"><Badge status={row.status} /></td>
                      <td className="px-4 py-3 text-xs" style={{ color: 'var(--color-muted)' }}>
                        {row.technicianName ?? '—'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        )}

        {/* Quotes */}
        {quotes.length > 0 && (
          <section className="space-y-4">
            <h2 className="text-lg font-bold" style={{ color: 'var(--color-ink)' }}>Orçamentos</h2>
            <div className="rounded-xl border overflow-hidden" style={{ borderColor: 'var(--color-line)' }}>
              <table className="w-full text-sm">
                <thead>
                  <tr style={{ backgroundColor: 'var(--color-canvas)', borderBottom: '1px solid var(--color-line)' }}>
                    {['Nº', 'Data', 'Estado', 'Total'].map(h => (
                      <th key={h} className="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wide"
                        style={{ color: 'var(--color-muted)' }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody style={{ backgroundColor: 'var(--color-card)' }}>
                  {quotes.map((row, i) => (
                    <tr key={row.id} style={{ borderTop: i > 0 ? '1px solid var(--color-line)' : undefined }}>
                      <td className="px-4 py-3 font-mono text-xs font-medium" style={{ color: 'var(--color-ink)' }}>
                        {row.number}
                      </td>
                      <td className="px-4 py-3 text-xs" style={{ color: 'var(--color-muted)' }}>
                        {formatDate(row.createdAt)}
                      </td>
                      <td className="px-4 py-3"><Badge status={row.status} /></td>
                      <td className="px-4 py-3 font-mono text-xs font-semibold" style={{ color: 'var(--color-ink)' }}>
                        {fmt(row.total)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        )}

        <footer className="text-center pt-4">
          <p className="text-xs" style={{ color: 'var(--color-muted)' }}>
            Portal de cliente · TécnicoApp
          </p>
        </footer>
      </main>
    </div>
  )
}
