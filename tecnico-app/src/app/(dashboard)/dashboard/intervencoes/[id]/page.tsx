'use client'

import { use } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useIntervention, useUpdateInterventionStatus, useDeleteIntervention } from '@/hooks/useInterventions'
import { InterventionStatusBadge } from '@/components/features/InterventionStatusBadge'
import { formatDate, formatDateTime } from '@/lib/utils/formatters'
import type { InterventionStatus } from '@/types'

const nextStatuses: Partial<Record<InterventionStatus, { status: InterventionStatus; label: string; bg: string; color: string }[]>> = {
  Scheduled: [
    { status: 'InProgress', label: 'Iniciar intervenção', bg: 'var(--color-brand-500)', color: 'var(--color-sidebar)' },
  ],
  InProgress: [
    { status: 'Completed', label: 'Marcar como concluída', bg: '#16a34a', color: 'white' },
    { status: 'Scheduled', label: 'Reagendar', bg: 'transparent', color: 'var(--color-muted)' },
  ],
}

export default function IntervencaoDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params)
  const router = useRouter()
  const { data: iv, isLoading } = useIntervention(id)
  const updateStatus = useUpdateInterventionStatus()
  const deleteIntervention = useDeleteIntervention()

  const handleDelete = () => {
    if (!confirm(`Apagar a intervenção "${iv?.title}"?`)) return
    deleteIntervention.mutate(id, { onSuccess: () => router.push('/dashboard/intervencoes') })
  }

  if (isLoading) {
    return (
      <div className="space-y-4 animate-pulse max-w-3xl">
        <div className="h-8 rounded-lg w-1/3" style={{ backgroundColor: 'var(--color-line)' }} />
        <div className="h-4 rounded-lg w-1/4" style={{ backgroundColor: 'var(--color-line)' }} />
        <div className="h-48 rounded-xl" style={{ backgroundColor: 'var(--color-line)' }} />
      </div>
    )
  }

  if (!iv) {
    return (
      <div className="text-center py-16">
        <p style={{ color: 'var(--color-muted)' }}>Intervenção não encontrada.</p>
        <Link
          href="/dashboard/intervencoes"
          className="text-sm mt-2 inline-block transition-colors duration-150"
          style={{ color: 'var(--color-brand-500)' }}
        >
          Voltar à lista
        </Link>
      </div>
    )
  }

  const actions = nextStatuses[iv.status] ?? []

  return (
    <div className="max-w-3xl space-y-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm" style={{ color: 'var(--color-muted)' }}>
        <Link
          href="/dashboard/intervencoes"
          className="transition-colors duration-150"
          onMouseEnter={(e) => (e.currentTarget.style.color = 'var(--color-ink)')}
          onMouseLeave={(e) => (e.currentTarget.style.color = 'var(--color-muted)')}
        >
          Intervenções
        </Link>
        <span style={{ color: 'var(--color-line-strong)' }}>/</span>
        <span style={{ color: 'var(--color-ink)' }}>{iv.title}</span>
      </div>

      {/* Header */}
      <div className="flex items-start justify-between gap-4">
        <div>
          <div className="flex items-center gap-3 flex-wrap">
            <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>{iv.title}</h1>
            <InterventionStatusBadge status={iv.status} />
          </div>
          <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
            Cliente:{' '}
            <Link
              href={`/dashboard/clientes/${iv.clientId}`}
              className="transition-colors duration-150"
              style={{ color: 'var(--color-brand-500)' }}
            >
              {iv.clientName}
            </Link>
          </p>
        </div>

        <div className="flex flex-wrap gap-2 justify-end shrink-0">
          {iv.status !== 'Completed' && (
            <Link
              href={`/dashboard/intervencoes/${id}/editar`}
              className="rounded-lg border px-4 py-2 text-sm font-medium transition-all duration-150"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'white' }}
              onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
              onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'white')}
            >
              Editar
            </Link>
          )}
          <button
            onClick={handleDelete}
            className="rounded-lg border px-4 py-2 text-sm font-medium transition-all duration-150"
            style={{ borderColor: '#fecaca', color: '#dc2626', backgroundColor: 'white' }}
            onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = '#fef2f2')}
            onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'white')}
          >
            Apagar
          </button>
          {actions.map((a) => (
            <button
              key={a.status}
              onClick={() => updateStatus.mutate({ id, status: a.status })}
              disabled={updateStatus.isPending}
              className="rounded-lg px-4 py-2 text-sm font-medium transition-all duration-150 disabled:opacity-60"
              style={{
                backgroundColor: a.bg,
                color: a.color,
                border: a.bg === 'transparent' ? '1px solid var(--color-line-strong)' : 'none',
              }}
            >
              {a.label}
            </button>
          ))}
        </div>
      </div>

      {/* Main info */}
      <div className="rounded-xl border divide-y" style={{ backgroundColor: 'white', borderColor: 'var(--color-line)' }}>
        {iv.description && (
          <InfoRow label="Descrição">
            <span className="text-sm whitespace-pre-wrap" style={{ color: 'var(--color-ink)' }}>{iv.description}</span>
          </InfoRow>
        )}
        {iv.scheduledAt && (
          <InfoRow label="Agendada para">
            <span className="text-sm" style={{ color: 'var(--color-ink)' }}>{formatDateTime(iv.scheduledAt)}</span>
          </InfoRow>
        )}
        {iv.completedAt && (
          <InfoRow label="Concluída em">
            <span className="text-sm" style={{ color: 'var(--color-ink)' }}>{formatDateTime(iv.completedAt)}</span>
          </InfoRow>
        )}
        {iv.quoteNumber && (
          <InfoRow label="Orçamento">
            <Link
              href={`/dashboard/orcamentos/${iv.quoteId}`}
              className="text-sm font-mono transition-colors duration-150"
              style={{ color: 'var(--color-brand-500)' }}
            >
              {iv.quoteNumber}
            </Link>
          </InfoRow>
        )}
        <InfoRow label="Criada em">
          <span className="text-sm" style={{ color: 'var(--color-ink)' }}>{formatDate(iv.createdAt)}</span>
        </InfoRow>
      </div>

      {/* Equipment */}
      {iv.equipment.length > 0 && (
        <div className="rounded-xl border p-6 space-y-4" style={{ backgroundColor: 'white', borderColor: 'var(--color-line)' }}>
          <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
            Equipamentos ({iv.equipment.length})
          </h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
            {iv.equipment.map((eq) => (
              <div
                key={eq.id}
                className="flex items-center gap-3 rounded-lg border px-4 py-3"
                style={{ borderColor: 'var(--color-line)', backgroundColor: 'var(--color-canvas)' }}
              >
                <span
                  className="w-2 h-2 rounded-full shrink-0"
                  style={{ backgroundColor: 'var(--color-brand-500)' }}
                />
                <div className="min-w-0">
                  <p className="text-sm font-medium truncate" style={{ color: 'var(--color-ink)' }}>{eq.type}</p>
                  {(eq.brand || eq.model) && (
                    <p className="text-xs truncate" style={{ color: 'var(--color-muted)' }}>
                      {[eq.brand, eq.model].filter(Boolean).join(' ')}
                    </p>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Technician notes */}
      {iv.technicianNotes && (
        <div className="rounded-xl border p-6 space-y-3" style={{ backgroundColor: 'white', borderColor: 'var(--color-line)' }}>
          <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
            Notas técnicas
          </h2>
          <p className="text-sm whitespace-pre-wrap" style={{ color: 'var(--color-ink)' }}>{iv.technicianNotes}</p>
        </div>
      )}
    </div>
  )
}

function InfoRow({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex px-6 py-4 gap-6" style={{ borderColor: 'var(--color-line)' }}>
      <span className="text-sm font-medium w-36 shrink-0" style={{ color: 'var(--color-muted)' }}>{label}</span>
      {children}
    </div>
  )
}
