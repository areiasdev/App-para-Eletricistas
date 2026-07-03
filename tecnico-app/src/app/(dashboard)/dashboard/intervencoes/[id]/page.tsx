'use client'

import { use, useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { toast } from 'sonner'
import { useIntervention, useUpdateIntervention, useUpdateInterventionStatus, useDeleteIntervention } from '@/hooks/useInterventions'
import { InterventionStatusBadge } from '@/components/features/InterventionStatusBadge'
import { formatDate, formatDateTime } from '@/lib/utils/formatters'
import { getErrorMessage } from '@/lib/api/client'
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
  const updateIntervention = useUpdateIntervention(id)
  const deleteIntervention = useDeleteIntervention()

  const [editingNotes, setEditingNotes] = useState(false)
  const [notesValue, setNotesValue] = useState('')

  const handleDelete = () => {
    if (!confirm(`Apagar a intervenção "${iv?.title}"?`)) return
    deleteIntervention.mutate(id, {
      onSuccess: () => router.push('/dashboard/intervencoes'),
      onError: (err) => toast.error(getErrorMessage(err)),
    })
  }

  const handleStartEditNotes = () => {
    setNotesValue(iv?.technicianNotes ?? '')
    setEditingNotes(true)
  }

  const handleSaveNotes = () => {
    if (!iv) return
    updateIntervention.mutate(
      {
        title: iv.title,
        description: iv.description,
        scheduledAt: iv.scheduledAt,
        technicianNotes: notesValue || undefined,
        quoteId: iv.quoteId,
        equipmentIds: iv.equipment.map((e) => e.id),
        photos: iv.photos,
        materials: iv.materials,
      },
      {
        onSuccess: () => {
          setEditingNotes(false)
          toast.success('Notas guardadas.')
        },
        onError: (err) => toast.error(getErrorMessage(err)),
      }
    )
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

  const isOverdue =
    iv.status === 'Scheduled' &&
    !!iv.scheduledAt &&
    new Date(iv.scheduledAt) < new Date()

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
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
              onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
              onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
            >
              Editar
            </Link>
          )}
          <button
            onClick={handleDelete}
            className="rounded-lg border px-4 py-2 text-sm font-medium transition-all duration-150"
            style={{ borderColor: '#fecaca', color: '#dc2626', backgroundColor: 'var(--color-card)' }}
            onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = '#fef2f2')}
            onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
          >
            Apagar
          </button>
          {actions.map((a) => (
            <button
              key={a.status}
              onClick={() => updateStatus.mutate(
                { id, status: a.status },
                { onError: (err) => toast.error(getErrorMessage(err)) }
              )}
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

      {/* Overdue warning */}
      {isOverdue && (
        <div
          className="flex items-start gap-3 rounded-xl border px-4 py-3"
          style={{ backgroundColor: '#fffbeb', borderColor: '#fcd34d' }}
        >
          <svg className="shrink-0 mt-0.5" width="18" height="18" viewBox="0 0 20 20" fill="none">
            <path d="M10 2L1.5 17h17L10 2z" stroke="#d97706" strokeWidth="1.5" strokeLinejoin="round" fill="#fef3c7"/>
            <path d="M10 8v4" stroke="#d97706" strokeWidth="1.5" strokeLinecap="round"/>
            <circle cx="10" cy="14.5" r="0.75" fill="#d97706"/>
          </svg>
          <div>
            <p className="text-sm font-semibold" style={{ color: '#92400e' }}>
              Intervenção em atraso
            </p>
            <p className="text-sm mt-0.5" style={{ color: '#b45309' }}>
              A data agendada ({formatDateTime(iv.scheduledAt!)}) já passou e a intervenção ainda está como{' '}
              <strong>Agendada</strong>. Inicia a intervenção ou reagenda para uma nova data.
            </p>
          </div>
        </div>
      )}

      {/* Main info */}
      <div className="rounded-xl border divide-y" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
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
        <div className="rounded-xl border p-6 space-y-4" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
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

      {/* Materials */}
      {iv.materials && iv.materials.length > 0 && (
        <div className="rounded-xl border p-6 space-y-4" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
          <div className="flex items-center justify-between">
            <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
              Materiais utilizados ({iv.materials.length})
            </h2>
            <span className="text-xs font-semibold font-mono" style={{ color: 'var(--color-brand-500)' }}>
              Total: {iv.materials.reduce((s, m) => s + m.quantity * m.unitCost, 0).toFixed(2)} €
            </span>
          </div>
          <div className="rounded-lg border overflow-hidden" style={{ borderColor: 'var(--color-line)' }}>
            <table className="w-full text-sm">
              <thead>
                <tr style={{ backgroundColor: 'var(--color-canvas)', borderBottom: '1px solid var(--color-line)' }}>
                  <th className="text-left px-4 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>Material</th>
                  <th className="text-right px-4 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>Qtd.</th>
                  <th className="text-right px-4 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>€/un.</th>
                  <th className="text-right px-4 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>Total</th>
                </tr>
              </thead>
              <tbody>
                {iv.materials.map((m, i) => (
                  <tr key={i} style={{ borderTop: i > 0 ? '1px solid var(--color-line)' : undefined }}>
                    <td className="px-4 py-2.5" style={{ color: 'var(--color-ink)' }}>{m.name}</td>
                    <td className="px-4 py-2.5 text-right font-mono text-xs" style={{ color: 'var(--color-muted)' }}>{m.quantity}</td>
                    <td className="px-4 py-2.5 text-right font-mono text-xs" style={{ color: 'var(--color-muted)' }}>{m.unitCost.toFixed(2)}</td>
                    <td className="px-4 py-2.5 text-right font-mono text-xs font-semibold" style={{ color: 'var(--color-ink)' }}>
                      {(m.quantity * m.unitCost).toFixed(2)} €
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Technician notes — inline editable */}
      <div className="rounded-xl border p-6 space-y-3" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <div className="flex items-center justify-between">
          <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
            Notas técnicas
          </h2>
          {!editingNotes && iv.status !== 'Completed' && (
            <button
              onClick={handleStartEditNotes}
              className="text-xs px-3 py-1 rounded-md border transition-all duration-150"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-muted)', backgroundColor: 'var(--color-canvas)' }}
              onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-ink)')}
              onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-muted)')}
            >
              {iv.technicianNotes ? 'Editar' : 'Adicionar notas'}
            </button>
          )}
        </div>

        {editingNotes ? (
          <div className="space-y-3">
            <textarea
              value={notesValue}
              onChange={e => setNotesValue(e.target.value)}
              rows={5}
              placeholder="Observações, materiais usados, próximas ações..."
              className="w-full rounded-lg border px-3 py-2.5 text-sm outline-none resize-none transition-all duration-150"
              style={{
                borderColor: 'var(--color-line-strong)',
                backgroundColor: 'var(--color-canvas)',
                color: 'var(--color-ink)',
              }}
              autoFocus
            />
            <div className="flex gap-2 justify-end">
              <button
                onClick={() => setEditingNotes(false)}
                className="text-sm px-4 py-2 rounded-lg border transition-all duration-150"
                style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-muted)', backgroundColor: 'var(--color-canvas)' }}
              >
                Cancelar
              </button>
              <button
                onClick={handleSaveNotes}
                disabled={updateIntervention.isPending}
                className="text-sm px-4 py-2 rounded-lg font-medium transition-all duration-150 disabled:opacity-60"
                style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
              >
                {updateIntervention.isPending ? 'A guardar...' : 'Guardar'}
              </button>
            </div>
          </div>
        ) : iv.technicianNotes ? (
          <p className="text-sm whitespace-pre-wrap" style={{ color: 'var(--color-ink)' }}>{iv.technicianNotes}</p>
        ) : (
          <p className="text-sm" style={{ color: 'var(--color-subtle)' }}>Sem notas técnicas.</p>
        )}
      </div>

      {/* Photo gallery */}
      {iv.photos.length > 0 && (
        <div className="rounded-xl border p-6 space-y-4" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
          <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
            Fotos ({iv.photos.length})
          </h2>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            {iv.photos.map((url, i) => (
              <a
                key={i}
                href={url}
                target="_blank"
                rel="noopener noreferrer"
                className="block rounded-lg overflow-hidden border aspect-video relative group"
                style={{ borderColor: 'var(--color-line)' }}
              >
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={url}
                  alt={`Foto ${i + 1}`}
                  className="w-full h-full object-cover transition-transform duration-200 group-hover:scale-105"
                  onError={e => {
                    const parent = e.currentTarget.parentElement
                    if (parent) {
                      e.currentTarget.style.display = 'none'
                      parent.innerHTML = `<div class="w-full h-full flex items-center justify-center text-xs" style="background:var(--color-canvas);color:var(--color-muted)">Sem pré-visualização</div>`
                    }
                  }}
                />
                <div
                  className="absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity duration-150 flex items-center justify-center"
                  style={{ backgroundColor: 'rgba(0,0,0,0.35)' }}
                >
                  <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
                    <path d="M10 3H3v14h14v-7M13 3h4v4M20 0l-8 8" stroke="white" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                  </svg>
                </div>
              </a>
            ))}
          </div>
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
