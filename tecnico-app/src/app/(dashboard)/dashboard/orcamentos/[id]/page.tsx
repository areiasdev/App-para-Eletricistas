'use client'

import { use, useState } from 'react'
import Link from 'next/link'
import { toast } from 'sonner'
import { useRouter } from 'next/navigation'
import { useQuote, useUpdateQuoteStatus, useSignQuote, useDeleteQuote, useSendQuoteEmail } from '@/hooks/useQuotes'
import { QuoteStatusBadge } from '@/components/features/QuoteStatusBadge'
import { SignatureModal } from '@/components/features/SignatureModal'
import { UpgradeModal } from '@/components/features/UpgradeModal'
import { formatDate, formatDateTime, formatCurrency } from '@/lib/utils/formatters'
import { quotesApi } from '@/lib/api/quotes'
import { getErrorMessage, isPlanLimitError } from '@/lib/api/client'
import type { QuoteStatus } from '@/types'

// ── Status pipeline ──────────────────────────────────────────────────────────
const PIPELINE: { status: QuoteStatus; label: string }[] = [
  { status: 'Draft',    label: 'Rascunho' },
  { status: 'Sent',     label: 'Enviado' },
  { status: 'Accepted', label: 'Aceite' },
  { status: 'Invoiced', label: 'Faturado' },
]

function QuotePipeline({ current }: { current: QuoteStatus }) {
  const isRejected = current === 'Rejected'
  // which step index is current? (Rejected maps visually to Sent)
  const currentIdx = isRejected
    ? 1
    : PIPELINE.findIndex((s) => s.status === current)

  return (
    <div className="rounded-xl border px-6 py-5" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
      <div className="flex items-center gap-0">
        {PIPELINE.map((step, idx) => {
          const isPast    = idx < currentIdx
          const isCurrent = idx === currentIdx && !isRejected
          const isFuture  = idx > currentIdx

          return (
            <div key={step.status} className="flex items-center" style={{ flex: idx < PIPELINE.length - 1 ? '1' : undefined }}>
              {/* Circle */}
              <div className="flex flex-col items-center gap-1.5 shrink-0">
                <div
                  className="w-7 h-7 rounded-full flex items-center justify-center transition-all duration-300"
                  style={{
                    backgroundColor: isPast || isCurrent
                      ? (isRejected && idx === 1 ? '#ef4444' : 'var(--color-brand-500)')
                      : 'var(--color-canvas)',
                    border: isFuture ? '2px solid var(--color-line-strong)' : 'none',
                    boxShadow: isCurrent ? '0 0 0 3px rgba(245,158,11,0.2)' : 'none',
                  }}
                >
                  {isPast ? (
                    <svg width="12" height="10" viewBox="0 0 12 10" fill="none">
                      <path d="M1 5l3.5 3.5L11 1" stroke="white" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"/>
                    </svg>
                  ) : (
                    <span
                      className="text-xs font-bold"
                      style={{ color: isCurrent ? 'var(--color-sidebar)' : 'var(--color-subtle)' }}
                    >
                      {idx + 1}
                    </span>
                  )}
                </div>
                <span
                  className="text-xs font-medium whitespace-nowrap"
                  style={{ color: isCurrent || isPast ? 'var(--color-ink)' : 'var(--color-subtle)' }}
                >
                  {step.label}
                </span>
              </div>
              {/* Connector */}
              {idx < PIPELINE.length - 1 && (
                <div
                  className="flex-1 h-0.5 mx-2 mb-5 transition-all duration-300"
                  style={{ backgroundColor: isPast ? 'var(--color-brand-500)' : 'var(--color-line)' }}
                />
              )}
            </div>
          )
        })}

        {/* Rejected branch indicator */}
        {isRejected && (
          <div className="ml-4 flex items-center gap-1.5 shrink-0 mb-5">
            <div className="h-0.5 w-4" style={{ backgroundColor: '#ef4444' }} />
            <span
              className="rounded-full px-2 py-0.5 text-xs font-medium"
              style={{ backgroundColor: '#fef2f2', color: '#dc2626' }}
            >
              Recusado
            </span>
          </div>
        )}
      </div>
    </div>
  )
}

// ── Status actions ────────────────────────────────────────────────────────────
const nextStatuses: Partial<Record<QuoteStatus, { status: QuoteStatus; label: string; bg: string; color: string }[]>> = {
  Draft:    [{ status: 'Sent',     label: 'Marcar como Enviado',    bg: '#2563eb', color: 'white' }],
  Sent:     [
    { status: 'Accepted', label: 'Aceite pelo cliente',   bg: '#16a34a', color: 'white' },
    { status: 'Rejected', label: 'Recusado pelo cliente', bg: 'transparent', color: '#dc2626' },
    { status: 'Draft',    label: 'Revogar envio',          bg: 'transparent', color: 'var(--color-muted)' },
  ],
  Accepted: [{ status: 'Invoiced', label: 'Marcar como Faturado',   bg: '#7c3aed', color: 'white' }],
}

export default function OrcamentoDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params)
  const router = useRouter()
  const { data: quote, isLoading } = useQuote(id)
  const updateStatus = useUpdateQuoteStatus()
  const signQuote = useSignQuote()
  const deleteQuote = useDeleteQuote()
  const sendEmail = useSendQuoteEmail()
  const [pdfLoading, setPdfLoading] = useState(false)
  const [showSignModal, setShowSignModal] = useState(false)
  const [upgradeMessage, setUpgradeMessage] = useState<string | null>(null)
  const [emailSent, setEmailSent] = useState(false)

  const handleStatusChange = (status: QuoteStatus) => {
    updateStatus.mutate({ id, status }, {
      onError: (err) => toast.error(getErrorMessage(err)),
    })
  }

  const handleDownloadPdf = async () => {
    if (!quote) return
    setPdfLoading(true)
    try {
      await quotesApi.downloadPdf(id, quote.number)
    } catch (err) {
      toast.error(getErrorMessage(err))
    } finally {
      setPdfLoading(false)
    }
  }

  const handleSign = (dataUrl: string) => {
    signQuote.mutate(
      { id, signatureDataUrl: dataUrl },
      {
        onSuccess: () => setShowSignModal(false),
        onError: (err) => {
          setShowSignModal(false)
          if (isPlanLimitError(err)) setUpgradeMessage(getErrorMessage(err))
        },
      }
    )
  }

  const handleSendEmail = () => {
    sendEmail.mutate(id, {
      onSuccess: () => {
        setEmailSent(true)
        // Auto-advance status Draft → Sent — the client received the quote
        if (quote?.status === 'Draft') {
          updateStatus.mutate({ id, status: 'Sent' })
        }
      },
      onError: (err) => toast.error(getErrorMessage(err)),
    })
  }

  const handleDelete = () => {
    if (!confirm(`Apagar o orçamento ${quote?.number}?`)) return
    deleteQuote.mutate(id, {
      onSuccess: () => router.push('/dashboard/orcamentos'),
      onError: (err) => toast.error(getErrorMessage(err)),
    })
  }

  if (isLoading) {
    return (
      <div className="space-y-4 animate-pulse max-w-3xl">
        <div className="h-8 rounded-lg w-1/3" style={{ backgroundColor: 'var(--color-line)' }} />
        <div className="h-4 rounded-lg w-1/4" style={{ backgroundColor: 'var(--color-line)' }} />
        <div className="h-20 rounded-xl" style={{ backgroundColor: 'var(--color-line)' }} />
        <div className="h-48 rounded-xl" style={{ backgroundColor: 'var(--color-line)' }} />
      </div>
    )
  }

  if (!quote) {
    return (
      <div className="text-center py-16">
        <p style={{ color: 'var(--color-muted)' }}>Orçamento não encontrado.</p>
        <Link href="/dashboard/orcamentos" className="text-sm mt-2 inline-block" style={{ color: 'var(--color-brand-500)' }}>
          Voltar à lista
        </Link>
      </div>
    )
  }

  const actions = nextStatuses[quote.status] ?? []
  const canSign = (quote.status === 'Sent' || quote.status === 'Accepted') && !quote.signedAt

  return (
    <>
      {showSignModal && (
        <SignatureModal
          quoteNumber={quote.number}
          onConfirm={handleSign}
          onClose={() => setShowSignModal(false)}
          isLoading={signQuote.isPending}
        />
      )}
      {upgradeMessage && (
        <UpgradeModal
          message={upgradeMessage}
          onClose={() => setUpgradeMessage(null)}
        />
      )}

      <div className="max-w-3xl space-y-6">
        {/* Breadcrumb */}
        <div className="flex items-center gap-2 text-sm" style={{ color: 'var(--color-muted)' }}>
          <Link
            href="/dashboard/orcamentos"
            className="transition-colors duration-150"
            onMouseEnter={(e) => (e.currentTarget.style.color = 'var(--color-ink)')}
            onMouseLeave={(e) => (e.currentTarget.style.color = 'var(--color-muted)')}
          >
            Orçamentos
          </Link>
          <span style={{ color: 'var(--color-line-strong)' }}>/</span>
          <span style={{ color: 'var(--color-ink)', fontFamily: 'var(--font-jetbrains), monospace' }}>{quote.number}</span>
        </div>

        {/* Header */}
        <div className="flex items-start justify-between gap-4 flex-wrap">
          <div>
            <div className="flex items-center gap-3 flex-wrap">
              <h1
                className="text-2xl font-bold"
                style={{ color: 'var(--color-ink)', fontFamily: 'var(--font-jetbrains), monospace' }}
              >
                {quote.number}
              </h1>
              <QuoteStatusBadge status={quote.status} />
              {quote.signedAt && (
                <span
                  className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium"
                  style={{ backgroundColor: '#f0fdf4', color: '#15803d' }}
                >
                  <svg width="10" height="10" viewBox="0 0 10 10" fill="none">
                    <path d="M1.5 5l2.5 2.5 4.5-4" stroke="#15803d" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                  </svg>
                  Assinado
                </span>
              )}
            </div>
            <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
              Cliente: <Link href={`/dashboard/clientes/${quote.clientId}`} style={{ color: 'var(--color-brand-500)' }}>{quote.clientName}</Link>
            </p>
          </div>

          <div className="flex flex-wrap gap-2 justify-end shrink-0">
            {/* PDF */}
            <button
              onClick={handleDownloadPdf}
              disabled={pdfLoading}
              className="rounded-lg border px-3 py-2 text-sm font-medium inline-flex items-center gap-1.5 transition-all duration-150 disabled:opacity-60"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
              onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
              onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
            >
              <svg width="13" height="13" viewBox="0 0 14 14" fill="none">
                <path d="M7 1v8M4 6l3 3 3-3M2 11h10" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
              {pdfLoading ? 'A gerar...' : 'PDF'}
            </button>

            {/* Send email */}
            <button
              onClick={handleSendEmail}
              disabled={sendEmail.isPending || emailSent}
              className="rounded-lg border px-3 py-2 text-sm font-medium inline-flex items-center gap-1.5 transition-all duration-150 disabled:opacity-60"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
              onMouseEnter={(e) => { if (!sendEmail.isPending && !emailSent) e.currentTarget.style.backgroundColor = 'var(--color-canvas)' }}
              onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
            >
              <svg width="13" height="13" viewBox="0 0 14 14" fill="none">
                <path d="M1 2l12 5-12 5V9l8-2-8-2V2z" stroke="currentColor" strokeWidth="1.3" strokeLinejoin="round"/>
              </svg>
              {sendEmail.isPending ? 'A enviar...' : emailSent ? 'Enviado ✓' : 'Enviar'}
            </button>

            {/* Sign */}
            {canSign && (
              <button
                onClick={() => setShowSignModal(true)}
                className="rounded-lg px-3 py-2 text-sm font-medium inline-flex items-center gap-1.5 transition-all duration-150"
                style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
              >
                <svg width="13" height="13" viewBox="0 0 14 14" fill="none">
                  <path d="M2 11c2-2 3-4 4-6M6 5c1-1 2-1 3 0s1 2 0 3l-4 3" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
                </svg>
                Assinar
              </button>
            )}

            {/* Edit / Delete (Draft only) */}
            {quote.status === 'Draft' && (
              <>
                <Link
                  href={`/dashboard/orcamentos/${id}/editar`}
                  className="rounded-lg border px-3 py-2 text-sm font-medium transition-all duration-150"
                  style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
                  onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
                  onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
                >
                  Editar
                </Link>
                <button
                  onClick={handleDelete}
                  className="rounded-lg border px-3 py-2 text-sm font-medium transition-all duration-150"
                  style={{ borderColor: '#fecaca', color: '#dc2626', backgroundColor: 'var(--color-card)' }}
                  onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = '#fef2f2')}
                  onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
                >
                  Apagar
                </button>
              </>
            )}

            {/* Status transitions */}
            {actions.map((a) => (
              <button
                key={a.status}
                onClick={() => handleStatusChange(a.status)}
                disabled={updateStatus.isPending}
                className="rounded-lg px-3 py-2 text-sm font-medium transition-all duration-150 disabled:opacity-60"
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

        {/* Status pipeline */}
        <QuotePipeline current={quote.status} />

        {/* Email sent banner */}
        {emailSent && (
          <div
            className="rounded-xl px-5 py-3.5 flex items-center gap-3"
            style={{ backgroundColor: '#f0fdf4', border: '1px solid #bbf7d0' }}
          >
            <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
              <path d="M1.5 7l3.5 3.5 7.5-7" stroke="#16a34a" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
            <p className="text-sm font-medium" style={{ color: '#15803d' }}>
              Orçamento enviado com sucesso por email para o cliente.
            </p>
          </div>
        )}

        {/* Next action hint */}
        {quote.status === 'Draft' && (
          <div
            className="rounded-xl px-5 py-3.5 flex items-center gap-3"
            style={{ backgroundColor: 'var(--color-brand-50)', border: '1px solid var(--color-brand-200)' }}
          >
            <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
              <circle cx="7" cy="7" r="6" stroke="var(--color-brand-600)" strokeWidth="1.4"/>
              <path d="M7 4v3M7 9.5v.5" stroke="var(--color-brand-600)" strokeWidth="1.4" strokeLinecap="round"/>
            </svg>
            <p className="text-sm" style={{ color: 'var(--color-brand-700)' }}>
              Descarrega o PDF e envia ao cliente. Depois marca como <strong>Enviado</strong>.
            </p>
          </div>
        )}

        {/* Meta */}
        <div className="rounded-xl border divide-y" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
          {quote.validUntil && <InfoRow label="Válido até" value={formatDate(quote.validUntil)} />}
          {quote.notes && <InfoRow label="Notas" value={quote.notes} />}
          <InfoRow label="Criado em" value={formatDate(quote.createdAt)} />
          {quote.signedAt && <InfoRow label="Assinado em" value={formatDateTime(quote.signedAt)} />}
        </div>

        {/* Signature image */}
        {quote.signatureUrl && (
          <div className="rounded-xl border p-5" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
            <p className="text-xs font-semibold uppercase tracking-wide mb-3" style={{ color: 'var(--color-muted)' }}>
              Assinatura do cliente
            </p>
            <div className="rounded-lg border p-3 inline-block" style={{ borderColor: 'var(--color-line)', backgroundColor: '#fafafa' }}>
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={quote.signatureUrl} alt="Assinatura" style={{ maxHeight: 120, maxWidth: 300 }} />
            </div>
          </div>
        )}

        {/* Lines */}
        <div className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
          <table className="min-w-full">
            <thead>
              <tr style={{ borderBottom: '1px solid var(--color-line)', backgroundColor: 'var(--color-canvas)' }}>
                {['Descrição', 'Qtd', 'Preço unit.', 'IVA', 'Total linha'].map((h) => (
                  <th key={h} className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {quote.lines.map((line) => (
                <tr key={line.id} style={{ borderBottom: '1px solid var(--color-line)' }}>
                  <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-ink)' }}>{line.description}</td>
                  <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-muted)' }}>{line.quantity}</td>
                  <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-muted)' }}>{formatCurrency(line.unitPrice)}</td>
                  <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-muted)' }}>{line.vatRate}%</td>
                  <td className="px-5 py-3.5 text-sm font-semibold" style={{ color: 'var(--color-ink)' }}>{formatCurrency(line.lineTotal)}</td>
                </tr>
              ))}
            </tbody>
          </table>

          {/* Totals */}
          <div className="px-5 py-4 space-y-1.5 text-sm" style={{ borderTop: '1px solid var(--color-line)', backgroundColor: 'var(--color-canvas)' }}>
            <div className="flex justify-between" style={{ color: 'var(--color-muted)' }}>
              <span>Subtotal</span><span>{formatCurrency(quote.subTotal)}</span>
            </div>
            <div className="flex justify-between" style={{ color: 'var(--color-muted)' }}>
              <span>IVA</span><span>{formatCurrency(quote.vatTotal)}</span>
            </div>
            {quote.discount != null && quote.discount > 0 && (
              <div className="flex justify-between" style={{ color: 'var(--color-muted)' }}>
                <span>Desconto</span><span>-{formatCurrency(quote.discount)}</span>
              </div>
            )}
            <div
              className="flex justify-between font-bold text-base pt-2"
              style={{ color: 'var(--color-ink)', borderTop: '1px solid var(--color-line)' }}
            >
              <span>Total</span><span style={{ color: 'var(--color-brand-600)' }}>{formatCurrency(quote.total)}</span>
            </div>
          </div>
        </div>
      </div>
    </>
  )
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex px-6 py-4 gap-6" style={{ borderColor: 'var(--color-line)' }}>
      <span className="text-sm font-medium w-28 shrink-0" style={{ color: 'var(--color-muted)' }}>{label}</span>
      <span className="text-sm" style={{ color: 'var(--color-ink)' }}>{value}</span>
    </div>
  )
}
