'use client'

import { use, useState } from 'react'
import Link from 'next/link'
import { toast } from 'sonner'
import { useRouter } from 'next/navigation'
import { useQuote, useUpdateQuoteStatus, useSignQuote, useDeleteQuote, useSendQuoteEmail, useDuplicateQuote } from '@/hooks/useQuotes'
import { useCreateInvoiceFromQuote } from '@/hooks/useInvoices'
import { useCanManage } from '@/hooks/useCanManage'
import { QuoteStatusBadge } from '@/components/features/QuoteStatusBadge'
import { SignatureModal } from '@/components/features/SignatureModal'
import { formatDate, formatDateTime, formatCurrency, formatQuantity, formatUnitPrice } from '@/lib/utils/formatters'
import { quotesApi } from '@/lib/api/quotes'
import { getErrorMessage } from '@/lib/api/client'
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
                  className="w-7 h-7 rounded-full flex items-center justify-center transition-colors duration-100"
                  style={{
                    backgroundColor: isPast || isCurrent
                      ? (isRejected && idx === 1 ? 'var(--color-danger-500)' : 'var(--color-brand-text)')
                      : 'var(--color-canvas)',
                    border: isFuture ? '2px solid var(--color-line-strong)' : 'none',
                    boxShadow: isCurrent ? '0 0 0 3px color-mix(in srgb, var(--color-brand-500) 20%, transparent)' : 'none',
                  }}
                >
                  {isPast ? (
                    <svg width="12" height="10" viewBox="0 0 12 10" fill="none">
                      <path d="M1 5l3.5 3.5L11 1" stroke="white" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"/>
                    </svg>
                  ) : (
                    <span
                      className="text-xs font-bold"
                      style={{ color: isCurrent ? 'var(--color-on-brand)' : 'var(--color-subtle)' }}
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
                  className="flex-1 h-0.5 mx-2 mb-5 transition-colors duration-100"
                  style={{ backgroundColor: isPast ? 'var(--color-brand-500)' : 'var(--color-line)' }}
                />
              )}
            </div>
          )
        })}

        {/* Rejected branch indicator */}
        {isRejected && (
          <div className="ml-4 flex items-center gap-1.5 shrink-0 mb-5">
            <div className="h-0.5 w-4" style={{ backgroundColor: 'var(--color-danger-solid)' }} />
            <span
              className="rounded-sm px-2 py-0.5 text-xs font-medium"
              style={{ backgroundColor: 'var(--color-danger-50)', color: 'var(--color-danger-600)' }}
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
  Draft:    [{ status: 'Sent',     label: 'Marcar como Enviado',    bg: 'var(--color-info-solid)', color: 'white' }],
  Sent:     [
    { status: 'Accepted', label: 'Aceite pelo cliente',   bg: 'var(--color-success-solid)', color: 'white' },
    { status: 'Rejected', label: 'Recusado pelo cliente', bg: 'transparent', color: 'var(--color-danger-600)' },
    { status: 'Draft',    label: 'Revogar envio',          bg: 'transparent', color: 'var(--color-muted)' },
  ],
}

export default function OrcamentoDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params)
  const router = useRouter()
  const canManage = useCanManage()
  const { data: quote, isLoading } = useQuote(id)
  const updateStatus = useUpdateQuoteStatus()
  const signQuote = useSignQuote()
  const deleteQuote = useDeleteQuote()
  const sendEmail = useSendQuoteEmail()
  const duplicateQuote = useDuplicateQuote()
  const createInvoice = useCreateInvoiceFromQuote()
  const [pdfLoading, setPdfLoading] = useState(false)
  const [showSignModal, setShowSignModal] = useState(false)
  // Server-derived — survives a page reload, unlike local component state.
  const emailSent = !!quote?.emailSentAt

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
          toast.error(getErrorMessage(err))
        },
      }
    )
  }

  const handleSendEmail = () => {
    // The backend marks a draft as Sent in the same request and includes the online
    // approval link in the email.
    sendEmail.mutate(id, {
      onSuccess: () => toast.success(emailSent ? 'Orçamento reenviado.' : 'Orçamento enviado ao cliente.'),
      onError: (err) => toast.error(getErrorMessage(err)),
    })
  }

  const handleDuplicate = () => {
    duplicateQuote.mutate(id, {
      onSuccess: (copy) => {
        toast.success(`Criado ${copy.number} a partir deste orçamento.`)
        router.push(`/dashboard/orcamentos/${copy.id}/editar`)
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

  const handleCreateInvoice = () => {
    // The backend sets quote.status = Invoiced as part of creating the invoice — no need
    // to also call updateStatus here, the quote's own pipeline reflects it on next load.
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
        <div className="h-20 rounded-xl" style={{ backgroundColor: 'var(--color-line)' }} />
        <div className="h-48 rounded-xl" style={{ backgroundColor: 'var(--color-line)' }} />
      </div>
    )
  }

  if (!quote) {
    return (
      <div className="text-center py-16">
        <p style={{ color: 'var(--color-muted)' }}>Orçamento não encontrado.</p>
        <Link href="/dashboard/orcamentos" className="text-sm mt-2 inline-block" style={{ color: 'var(--color-brand-text)' }}>
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
          subtitle={quote.number}
          onConfirm={handleSign}
          onClose={() => setShowSignModal(false)}
          isLoading={signQuote.isPending}
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
          <span style={{ color: 'var(--color-ink)', fontFamily: 'var(--font-code), monospace' }}>{quote.number}</span>
        </div>

        {/* Header */}
        <div className="flex items-start justify-between gap-4 flex-wrap">
          <div>
            <div className="flex items-center gap-3 flex-wrap">
              <h1
                className="text-2xl font-bold"
                style={{ color: 'var(--color-ink)', fontFamily: 'var(--font-code), monospace' }}
              >
                {quote.number}
              </h1>
              <QuoteStatusBadge status={quote.status} />
              {quote.signedAt && (
                <span
                  className="inline-flex items-center gap-1.5 rounded-sm px-2.5 py-0.5 text-xs font-medium"
                  style={{ backgroundColor: 'var(--color-success-50)', color: 'var(--color-success-700)' }}
                >
                  <svg width="10" height="10" viewBox="0 0 10 10" fill="none">
                    <path d="M1.5 5l2.5 2.5 4.5-4" stroke="var(--color-success-700)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                  </svg>
                  Assinado
                </span>
              )}
            </div>
            <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
              Cliente: <Link href={`/dashboard/clientes/${quote.clientId}`} style={{ color: 'var(--color-brand-text)' }}>{quote.clientName}</Link>
            </p>
          </div>

          <div className="flex flex-wrap gap-2 justify-end shrink-0">
            {/* PDF */}
            <button
              onClick={handleDownloadPdf}
              disabled={pdfLoading}
              className="rounded-lg border px-3 py-2 text-sm font-medium inline-flex items-center gap-1.5 transition-colors duration-100 disabled:opacity-60"
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
              disabled={sendEmail.isPending}
              title={emailSent ? 'Enviar novamente (o link de aprovação mantém-se válido)' : 'Enviar por email com link para aceitar online'}
              className="rounded-lg border px-3 py-2 text-sm font-medium inline-flex items-center gap-1.5 transition-colors duration-100 disabled:opacity-60"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
              onMouseEnter={(e) => { if (!sendEmail.isPending) e.currentTarget.style.backgroundColor = 'var(--color-canvas)' }}
              onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
            >
              <svg width="13" height="13" viewBox="0 0 14 14" fill="none">
                <path d="M1 2l12 5-12 5V9l8-2-8-2V2z" stroke="currentColor" strokeWidth="1.3" strokeLinejoin="round"/>
              </svg>
              {sendEmail.isPending ? 'A enviar...' : emailSent ? 'Reenviar' : 'Enviar'}
            </button>

            {/* Duplicate — reuse for a similar job or revise a sent/rejected quote */}
            <button
              onClick={handleDuplicate}
              disabled={duplicateQuote.isPending}
              className="rounded-lg border px-3 py-2 text-sm font-medium inline-flex items-center gap-1.5 transition-colors duration-100 disabled:opacity-60"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
              onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
              onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
            >
              <svg width="13" height="13" viewBox="0 0 14 14" fill="none">
                <rect x="4" y="4" width="8.5" height="8.5" rx="1.5" stroke="currentColor" strokeWidth="1.3"/>
                <path d="M9.5 4V2.5A1 1 0 0 0 8.5 1.5h-6a1 1 0 0 0-1 1v6a1 1 0 0 0 1 1H4" stroke="currentColor" strokeWidth="1.3"/>
              </svg>
              {duplicateQuote.isPending ? 'A duplicar...' : 'Duplicar'}
            </button>

            {/* Sign */}
            {canSign && (
              <button
                onClick={() => setShowSignModal(true)}
                className="rounded-lg px-3 py-2 text-sm font-medium inline-flex items-center gap-1.5 transition-colors duration-100"
                style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-on-brand)' }}
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
                  className="rounded-lg border px-3 py-2 text-sm font-medium transition-colors duration-100"
                  style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
                  onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
                  onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
                >
                  Editar
                </Link>
                {canManage && (
                  <button
                    onClick={handleDelete}
                    className="rounded-lg border px-3 py-2 text-sm font-medium transition-colors duration-100"
                    style={{ borderColor: 'var(--color-danger-200)', color: 'var(--color-danger-600)', backgroundColor: 'var(--color-card)' }}
                    onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-danger-50)')}
                    onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
                  >
                    Apagar
                  </button>
                )}
              </>
            )}

            {/* Faturar (Accepted only) — creates a real Invoice document, replacing the old
                plain status-flip to Invoiced */}
            {quote.status === 'Accepted' && canManage && (
              <button
                onClick={handleCreateInvoice}
                disabled={createInvoice.isPending}
                className="rounded-lg px-3 py-2 text-sm font-medium transition-colors duration-100 disabled:opacity-60"
                style={{ backgroundColor: 'var(--color-role-purple-solid)', color: 'white' }}
              >
                {createInvoice.isPending ? 'A faturar...' : 'Faturar'}
              </button>
            )}

            {/* Status transitions */}
            {actions.map((a) => (
              <button
                key={a.status}
                onClick={() => handleStatusChange(a.status)}
                disabled={updateStatus.isPending}
                className="rounded-lg px-3 py-2 text-sm font-medium transition-colors duration-100 disabled:opacity-60"
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
            style={{ backgroundColor: 'var(--color-success-50)', border: '1px solid var(--color-success-200)' }}
          >
            <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
              <path d="M1.5 7l3.5 3.5 7.5-7" stroke="var(--color-success-600)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
            <p className="text-sm font-medium" style={{ color: 'var(--color-success-700)' }}>
              {quote.status === 'Sent'
                ? `Enviado por email a ${formatDateTime(quote.emailSentAt!)} — o cliente pode aceitar e assinar online a partir do link no email.`
                : `Enviado por email a ${formatDateTime(quote.emailSentAt!)}.`}
            </p>
          </div>
        )}

        {/* Online answer from the client */}
        {quote.clientDecisionAt && (quote.status === 'Accepted' || quote.status === 'Invoiced') && (
          <div className="rounded-xl px-5 py-3.5" style={{ backgroundColor: 'var(--color-success-50)', border: '1px solid var(--color-success-200)' }}>
            <p className="text-sm font-medium" style={{ color: 'var(--color-success-700)' }}>
              Aceite online por {quote.acceptedByName} a {formatDateTime(quote.clientDecisionAt)}.
              {quote.status === 'Accepted' && canManage && ' Já podes agendar a intervenção e faturar.'}
            </p>
          </div>
        )}
        {quote.status === 'Rejected' && (
          <div className="rounded-xl px-5 py-3.5" style={{ backgroundColor: 'var(--color-danger-50)', border: '1px solid var(--color-danger-200)' }}>
            <p className="text-sm font-medium" style={{ color: 'var(--color-danger-700)' }}>
              Recusado pelo cliente{quote.clientDecisionAt ? ` a ${formatDateTime(quote.clientDecisionAt)}` : ''}.
              {quote.rejectionReason && <> Motivo: «{quote.rejectionReason}»</>}
            </p>
            <p className="text-xs mt-1" style={{ color: 'var(--color-danger-600)' }}>
              Usa <strong>Duplicar</strong> para preparar uma proposta revista.
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
              Carrega em <strong>Enviar</strong>: o cliente recebe o PDF e um link para aceitar e assinar online — o estado atualiza-se sozinho.
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
            <div className="rounded-lg border p-3 inline-block" style={{ borderColor: 'var(--color-line)', backgroundColor: 'var(--color-paper)' }}>
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={quote.signatureUrl} alt="Assinatura" style={{ maxHeight: 120, maxWidth: 300 }} />
            </div>
          </div>
        )}

        {/* Lines */}
        <div className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
          <div className="overflow-x-auto">
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
                  <td className="px-5 py-3.5 text-sm whitespace-nowrap" style={{ color: 'var(--color-muted)' }}>{formatQuantity(line.quantity, line.unit)}</td>
                  <td className="px-5 py-3.5 text-sm whitespace-nowrap" style={{ color: 'var(--color-muted)' }}>{formatUnitPrice(line.unitPrice)}</td>
                  <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-muted)' }}>{line.vatRate}%</td>
                  <td className="px-5 py-3.5 text-sm font-semibold" style={{ color: 'var(--color-ink)' }}>{formatCurrency(line.lineTotal)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>

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
              <span>Total</span><span style={{ color: 'var(--color-brand-text)' }}>{formatCurrency(quote.total)}</span>
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
