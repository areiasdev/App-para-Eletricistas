'use client'

import { use, useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { toast } from 'sonner'
import { useIntervention, useUpdateIntervention, useUpdateInterventionStatus, useDeleteIntervention, useSignIntervention } from '@/hooks/useInterventions'
import { useCreateInvoiceFromIntervention } from '@/hooks/useInvoices'
import { SignatureModal } from '@/components/features/SignatureModal'
import { PhotoUploader } from '@/components/features/PhotoUploader'
import { interventionsApi, type UpdateInterventionRequest } from '@/lib/api/interventions'
import type { Intervention } from '@/types'
import { useCanManage } from '@/hooks/useCanManage'
import { InterventionStatusBadge } from '@/components/features/InterventionStatusBadge'
import { formatDate, formatDateTime, formatCurrency, formatUnitPrice } from '@/lib/utils/formatters'
import { getErrorMessage } from '@/lib/api/client'
import type { InterventionStatus } from '@/types'

const nextStatuses: Partial<Record<InterventionStatus, { status: InterventionStatus; label: string; bg: string; color: string }[]>> = {
  Scheduled: [
    { status: 'InProgress', label: 'Iniciar intervenção', bg: 'var(--color-brand-500)', color: 'var(--color-on-brand)' },
  ],
  InProgress: [
    { status: 'Completed', label: 'Marcar como concluída', bg: 'var(--color-success-solid)', color: 'white' },
    { status: 'Scheduled', label: 'Reagendar', bg: 'transparent', color: 'var(--color-muted)' },
  ],
}

export default function IntervencaoDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params)
  const router = useRouter()
  const canManage = useCanManage()
  const { data: iv, isLoading } = useIntervention(id)
  const updateStatus = useUpdateInterventionStatus()
  const updateIntervention = useUpdateIntervention(id)
  const deleteIntervention = useDeleteIntervention()

  const signIntervention = useSignIntervention(id)
  const createInvoice = useCreateInvoiceFromIntervention()
  const [signing, setSigning] = useState(false)
  const [reportLoading, setReportLoading] = useState(false)
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
      toUpdateRequest(iv, { technicianNotes: notesValue || undefined }),
      {
        onSuccess: () => {
          setEditingNotes(false)
          toast.success('Notas guardadas.')
        },
        onError: (err) => toast.error(getErrorMessage(err)),
      }
    )
  }

  const handleSavePhotos = (photos: string[]) => {
    if (!iv) return
    updateIntervention.mutate(toUpdateRequest(iv, { photos }), {
      onError: (err) => toast.error(getErrorMessage(err)),
    })
  }

  const handleSign = (signatureDataUrl: string, signerName: string) => {
    signIntervention.mutate({ signedByName: signerName, signatureDataUrl }, {
      onSuccess: () => {
        setSigning(false)
        toast.success('Folha de obra assinada e intervenção concluída.')
      },
      onError: (err) => toast.error(getErrorMessage(err)),
    })
  }

  const handleDownloadReport = async () => {
    setReportLoading(true)
    try {
      await interventionsApi.downloadReport(id)
    } catch (err) {
      toast.error(getErrorMessage(err))
    } finally {
      setReportLoading(false)
    }
  }

  const handleInvoice = () => {
    createInvoice.mutate(id, {
      onSuccess: (invoice) => router.push(`/dashboard/faturas/${invoice.id}`),
      onError: (err) => toast.error(getErrorMessage(err)),
    })
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
          style={{ color: 'var(--color-brand-text)' }}
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

  const canInvoice = canManage && iv.status === 'Completed' && !iv.invoiceId

  return (
    <div className="max-w-3xl space-y-6">
      {signing && (
        <SignatureModal
          title="Assinatura do cliente"
          subtitle={`Folha de obra · ${iv.title}`}
          requireName
          defaultName={iv.clientName}
          declaration="Declaro que os trabalhos descritos foram executados. Ao assinar, a intervenção fica concluída."
          confirmLabel="Assinar e concluir"
          onConfirm={handleSign}
          onClose={() => setSigning(false)}
          isLoading={signIntervention.isPending}
        />
      )}

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

      {/* Header — stacks on phones so the title keeps full width and the actions wrap */}
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
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
              style={{ color: 'var(--color-brand-text)' }}
            >
              {iv.clientName}
            </Link>
          </p>
        </div>

        <div className="flex flex-wrap gap-2 sm:justify-end sm:max-w-[60%]">
          {iv.status !== 'Completed' && (
            <Link
              href={`/dashboard/intervencoes/${id}/editar`}
              className="rounded-lg border px-4 py-2 text-sm font-medium transition-colors duration-100"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
              onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
              onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
            >
              Editar
            </Link>
          )}
          <button
            onClick={handleDownloadReport}
            disabled={reportLoading}
            className="rounded-lg border px-4 py-2 text-sm font-medium transition-colors duration-100 disabled:opacity-60"
            style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
          >
            {reportLoading ? 'A gerar...' : 'Folha de obra (PDF)'}
          </button>
          {!iv.signedAt && (
            <button
              onClick={() => setSigning(true)}
              className="rounded-lg px-4 py-2 text-sm font-semibold transition-colors duration-100"
              style={{ backgroundColor: 'var(--color-success-solid)', color: 'white' }}
            >
              Cliente assina
            </button>
          )}
          {canInvoice && (
            <button
              onClick={handleInvoice}
              disabled={createInvoice.isPending}
              className="rounded-lg px-4 py-2 text-sm font-semibold transition-colors duration-100 disabled:opacity-60"
              style={{ backgroundColor: 'var(--color-role-purple-solid)', color: 'white' }}
              title="Cria a fatura com as horas (preço/hora do Perfil) e os materiais aplicados"
            >
              {createInvoice.isPending ? 'A faturar...' : 'Faturar'}
            </button>
          )}
          {canManage && (
            <button
              onClick={handleDelete}
              className="rounded-lg border px-4 py-2 text-sm font-medium transition-colors duration-100"
              style={{ borderColor: 'var(--color-danger-200)', color: 'var(--color-danger-600)', backgroundColor: 'var(--color-card)' }}
              onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-danger-50)')}
              onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
            >
              Apagar
            </button>
          )}
          {actions.map((a) => (
            <button
              key={a.status}
              onClick={() => updateStatus.mutate(
                { id, status: a.status },
                { onError: (err) => toast.error(getErrorMessage(err)) }
              )}
              disabled={updateStatus.isPending}
              className="rounded-lg px-4 py-2 text-sm font-medium transition-colors duration-100 disabled:opacity-60"
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
          style={{ backgroundColor: 'var(--color-brand-50)', borderColor: 'var(--color-brand-300)' }}
        >
          <svg className="shrink-0 mt-0.5" width="18" height="18" viewBox="0 0 20 20" fill="none">
            <path d="M10 2L1.5 17h17L10 2z" stroke="var(--color-brand-600)" strokeWidth="1.5" strokeLinejoin="round" fill="var(--color-brand-100)"/>
            <path d="M10 8v4" stroke="var(--color-brand-600)" strokeWidth="1.5" strokeLinecap="round"/>
            <circle cx="10" cy="14.5" r="0.75" fill="var(--color-brand-600)"/>
          </svg>
          <div>
            <p className="text-sm font-semibold" style={{ color: 'var(--color-brand-800)' }}>
              Intervenção em atraso
            </p>
            <p className="text-sm mt-0.5" style={{ color: 'var(--color-brand-700)' }}>
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
        {iv.assignedToName && (
          <InfoRow label="Técnico">
            <span className="text-sm" style={{ color: 'var(--color-ink)' }}>{iv.assignedToName}</span>
          </InfoRow>
        )}
        {iv.laborHours != null && iv.laborHours > 0 && (
          <InfoRow label="Horas de trabalho">
            <span className="text-sm" style={{ color: 'var(--color-ink)' }}>{iv.laborHours.toLocaleString('pt-PT')} h</span>
          </InfoRow>
        )}
        {iv.invoiceId && (
          <InfoRow label="Fatura">
            <Link href={`/dashboard/faturas/${iv.invoiceId}`} className="text-sm font-mono" style={{ color: 'var(--color-brand-text)' }}>
              {iv.invoiceNumber}
            </Link>
          </InfoRow>
        )}
        {iv.quoteNumber && (
          <InfoRow label="Orçamento">
            <Link
              href={`/dashboard/orcamentos/${iv.quoteId}`}
              className="text-sm font-mono transition-colors duration-150"
              style={{ color: 'var(--color-brand-text)' }}
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
            <span className="text-xs font-semibold font-mono" style={{ color: 'var(--color-brand-text)' }}>
              Total: {formatCurrency(iv.materials.reduce((s, m) => s + m.quantity * m.unitCost, 0))}
            </span>
          </div>
          <div className="rounded-lg border overflow-hidden" style={{ borderColor: 'var(--color-line)' }}>
            <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr style={{ backgroundColor: 'var(--color-canvas)', borderBottom: '1px solid var(--color-line)' }}>
                  <th className="text-left px-4 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>Material</th>
                  <th className="text-right px-4 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>Qtd.</th>
                  <th className="text-right px-4 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>Custo/un.</th>
                  <th className="text-right px-4 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>Venda/un.</th>
                  <th className="text-right px-4 py-2 text-xs font-semibold" style={{ color: 'var(--color-muted)' }}>Custo total</th>
                </tr>
              </thead>
              <tbody>
                {iv.materials.map((m, i) => (
                  <tr key={i} style={{ borderTop: i > 0 ? '1px solid var(--color-line)' : undefined }}>
                    <td className="px-4 py-2.5" style={{ color: 'var(--color-ink)' }}>{m.name}</td>
                    <td className="px-4 py-2.5 text-right font-mono text-xs" style={{ color: 'var(--color-muted)' }}>{m.quantity}</td>
                    <td className="px-4 py-2.5 text-right font-mono text-xs" style={{ color: 'var(--color-muted)' }}>{formatUnitPrice(m.unitCost)}</td>
                    <td className="px-4 py-2.5 text-right font-mono text-xs" style={{ color: 'var(--color-muted)' }}>{m.unitPrice != null ? formatUnitPrice(m.unitPrice) : '—'}</td>
                    <td className="px-4 py-2.5 text-right font-mono text-xs font-semibold" style={{ color: 'var(--color-ink)' }}>
                      {formatCurrency((m.quantity * m.unitCost))}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            </div>
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
              className="text-xs px-3 py-1 rounded-md border transition-colors duration-100"
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
              className="w-full rounded-lg border px-3 py-2.5 text-sm outline-none resize-none transition-colors duration-100"
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
                className="text-sm px-4 py-2 rounded-lg border transition-colors duration-100"
                style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-muted)', backgroundColor: 'var(--color-canvas)' }}
              >
                Cancelar
              </button>
              <button
                onClick={handleSaveNotes}
                disabled={updateIntervention.isPending}
                className="text-sm px-4 py-2 rounded-lg font-medium transition-colors duration-100 disabled:opacity-60"
                style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-on-brand)' }}
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

      {/* Signature */}
      {iv.clientSignatureUrl && (
        <div className="rounded-xl border p-6 space-y-3" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
          <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
            Assinatura do cliente
          </h2>
          <div className="rounded-lg border p-3 inline-block" style={{ borderColor: 'var(--color-line)', backgroundColor: 'var(--color-paper)' }}>
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src={iv.clientSignatureUrl} alt="Assinatura do cliente" style={{ maxHeight: 110, maxWidth: 300 }} />
          </div>
          <p className="text-sm" style={{ color: 'var(--color-muted)' }}>
            {iv.signedByName}{iv.signedAt ? ` — ${formatDateTime(iv.signedAt)}` : ''}
          </p>
        </div>
      )}

      {/* Photos — added straight from the phone camera, saved immediately */}
      <div className="rounded-xl border p-6 space-y-4" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
          Fotos ({iv.photos.length})
        </h2>
        <PhotoUploader value={iv.photos} onChange={handleSavePhotos} />
      </div>
    </div>
  )
}

/** Full update payload from the loaded job, so a partial edit never clears other fields. */
function toUpdateRequest(iv: Intervention, changes: Partial<UpdateInterventionRequest>): UpdateInterventionRequest {
  return {
    title: iv.title,
    description: iv.description,
    scheduledAt: iv.scheduledAt,
    technicianNotes: iv.technicianNotes,
    quoteId: iv.quoteId,
    equipmentIds: iv.equipment.map((e) => e.id),
    photos: iv.photos,
    materials: iv.materials,
    assignedToUserId: iv.assignedToUserId,
    laborHours: iv.laborHours,
    ...changes,
  }
}

function InfoRow({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex px-6 py-4 gap-6" style={{ borderColor: 'var(--color-line)' }}>
      <span className="text-sm font-medium w-36 shrink-0" style={{ color: 'var(--color-muted)' }}>{label}</span>
      {children}
    </div>
  )
}
