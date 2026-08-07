'use client'

import { use, useState } from 'react'
import Link from 'next/link'
import { toast } from 'sonner'
import { useInvoice, useUpdateInvoiceStatus, useInvoicePayLink, useSendInvoiceEmail } from '@/hooks/useInvoices'
import { useCanManage } from '@/hooks/useCanManage'
import { InvoiceStatusBadge } from '@/components/features/InvoiceStatusBadge'
import { formatDate, formatCurrency } from '@/lib/utils/formatters'
import { invoicesApi } from '@/lib/api/invoices'
import { getErrorMessage } from '@/lib/api/client'

export default function FaturaDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params)
  const canManage = useCanManage()
  const { data: invoice, isLoading } = useInvoice(id)
  const updateStatus = useUpdateInvoiceStatus()
  const payLink = useInvoicePayLink()
  const sendEmail = useSendInvoiceEmail()
  const [pdfLoading, setPdfLoading] = useState(false)

  const handleStatusChange = (status: 'Paid' | 'Cancelled') => {
    if (status === 'Cancelled' && !confirm(`Cancelar a fatura ${invoice?.number}?`)) return
    updateStatus.mutate({ id, status }, {
      onError: (err) => toast.error(getErrorMessage(err)),
    })
  }

  const handleDownloadPdf = async () => {
    if (!invoice) return
    setPdfLoading(true)
    try {
      await invoicesApi.downloadPdf(id, invoice.number)
    } catch (err) {
      toast.error(getErrorMessage(err))
    } finally {
      setPdfLoading(false)
    }
  }

  const handleCopyPayLink = () => {
    payLink.mutate(id, {
      onSuccess: async (url) => {
        try {
          await navigator.clipboard.writeText(url)
          toast.success('Link de pagamento copiado.')
        } catch {
          toast.error('Não foi possível copiar o link automaticamente.')
        }
      },
      onError: (err) => toast.error(getErrorMessage(err)),
    })
  }

  const handleSendEmail = () => {
    sendEmail.mutate(id, {
      onSuccess: () => toast.success('Fatura enviada por email.'),
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

  if (!invoice) {
    return (
      <div className="text-center py-16">
        <p style={{ color: 'var(--color-muted)' }}>Fatura não encontrada.</p>
        <Link href="/dashboard/faturas" className="text-sm mt-2 inline-block" style={{ color: 'var(--color-brand-500)' }}>
          Voltar à lista
        </Link>
      </div>
    )
  }

  const canChangeStatus = canManage && (invoice.status === 'Issued' || invoice.status === 'Overdue')

  return (
    <div className="max-w-3xl space-y-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm" style={{ color: 'var(--color-muted)' }}>
        <Link
          href="/dashboard/faturas"
          className="transition-colors duration-150"
          onMouseEnter={(e) => (e.currentTarget.style.color = 'var(--color-ink)')}
          onMouseLeave={(e) => (e.currentTarget.style.color = 'var(--color-muted)')}
        >
          Faturas
        </Link>
        <span style={{ color: 'var(--color-line-strong)' }}>/</span>
        <span style={{ color: 'var(--color-ink)', fontFamily: 'var(--font-jetbrains), monospace' }}>{invoice.number}</span>
      </div>

      {/* Header */}
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <div className="flex items-center gap-3 flex-wrap">
            <h1
              className="text-2xl font-bold"
              style={{ color: 'var(--color-ink)', fontFamily: 'var(--font-jetbrains), monospace' }}
            >
              {invoice.number}
            </h1>
            <InvoiceStatusBadge status={invoice.status} />
          </div>
          <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
            Cliente: <Link href={`/dashboard/clientes/${invoice.clientId}`} style={{ color: 'var(--color-brand-500)' }}>{invoice.clientName}</Link>
          </p>
          {invoice.quoteId && (
            <p className="text-sm mt-1" style={{ color: 'var(--color-subtle)' }}>
              Gerado a partir do orçamento{' '}
              <Link href={`/dashboard/orcamentos/${invoice.quoteId}`} style={{ color: 'var(--color-brand-500)' }}>
                {invoice.quoteNumber}
              </Link>
            </p>
          )}
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

          {/* Payment link + email (Owner/Admin only) */}
          {canManage && (
            <>
              <button
                onClick={handleCopyPayLink}
                disabled={payLink.isPending}
                className="rounded-lg border px-3 py-2 text-sm font-medium inline-flex items-center gap-1.5 transition-all duration-150 disabled:opacity-60"
                style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
                onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
                onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
              >
                {payLink.isPending ? 'A gerar...' : 'Copiar link de pagamento'}
              </button>
              <button
                onClick={handleSendEmail}
                disabled={sendEmail.isPending}
                className="rounded-lg border px-3 py-2 text-sm font-medium inline-flex items-center gap-1.5 transition-all duration-150 disabled:opacity-60"
                style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
                onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
                onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
              >
                {sendEmail.isPending ? 'A enviar...' : 'Enviar por email'}
              </button>
            </>
          )}

          {/* Status transitions */}
          {canChangeStatus && (
            <>
              <button
                onClick={() => handleStatusChange('Paid')}
                disabled={updateStatus.isPending}
                className="rounded-lg px-3 py-2 text-sm font-medium transition-all duration-150 disabled:opacity-60"
                style={{ backgroundColor: 'var(--color-success-600)', color: 'white' }}
              >
                Marcar como Paga
              </button>
              <button
                onClick={() => handleStatusChange('Cancelled')}
                disabled={updateStatus.isPending}
                className="rounded-lg px-3 py-2 text-sm font-medium transition-all duration-150 disabled:opacity-60"
                style={{ border: '1px solid var(--color-line-strong)', color: 'var(--color-danger-600)', backgroundColor: 'transparent' }}
              >
                Cancelar
              </button>
            </>
          )}
        </div>
      </div>

      {/* Meta */}
      <div className="rounded-xl border divide-y" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <InfoRow label="Emitida em" value={formatDate(invoice.issuedAt)} />
        <InfoRow label="Vencimento" value={formatDate(invoice.dueDate)} />
        {invoice.paidAt && <InfoRow label="Paga em" value={formatDate(invoice.paidAt)} />}
        {invoice.notes && <InfoRow label="Notas" value={invoice.notes} />}
      </div>

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
            {invoice.lines.map((line) => (
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
        </div>

        {/* Totals */}
        <div className="px-5 py-4 space-y-1.5 text-sm" style={{ borderTop: '1px solid var(--color-line)', backgroundColor: 'var(--color-canvas)' }}>
          <div className="flex justify-between" style={{ color: 'var(--color-muted)' }}>
            <span>Subtotal</span><span>{formatCurrency(invoice.subTotal)}</span>
          </div>
          <div className="flex justify-between" style={{ color: 'var(--color-muted)' }}>
            <span>IVA</span><span>{formatCurrency(invoice.vatTotal)}</span>
          </div>
          {invoice.discount != null && invoice.discount > 0 && (
            <div className="flex justify-between" style={{ color: 'var(--color-muted)' }}>
              <span>Desconto</span><span>-{formatCurrency(invoice.discount)}</span>
            </div>
          )}
          <div
            className="flex justify-between font-bold text-base pt-2"
            style={{ color: 'var(--color-ink)', borderTop: '1px solid var(--color-line)' }}
          >
            <span>Total</span><span style={{ color: 'var(--color-brand-600)' }}>{formatCurrency(invoice.total)}</span>
          </div>
        </div>
      </div>

      {/* Non-AT-certification notice */}
      <p className="text-xs text-center" style={{ color: 'var(--color-subtle)' }}>
        Este documento não é uma fatura certificada pela Autoridade Tributária — serve apenas para gestão interna.
      </p>
    </div>
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
