'use client'

import { use, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import { publicQuotesApi, type PublicQuote } from '@/lib/api/publicQuotes'
import { SignatureModal } from '@/components/features/SignatureModal'
import { API_BASE_URL } from '@/lib/config'
import { formatCurrency, formatDate, formatDateTime, formatQuantity, formatUnitPrice } from '@/lib/utils/formatters'

// Client-facing page reached from the link in the quote email: the client reviews the quote on
// their phone and accepts it with a signature (or declines with a reason) — no account needed.

function errorDetail(error: unknown, fallback: string) {
  if (isAxiosError(error) && typeof error.response?.data?.detail === 'string') return error.response.data.detail as string
  return fallback
}

export default function PublicQuotePage({ params }: { params: Promise<{ token: string }> }) {
  const { token } = use(params)
  const qc = useQueryClient()
  const [signing, setSigning] = useState(false)
  const [rejecting, setRejecting] = useState(false)
  const [reason, setReason] = useState('')

  const { data: quote, isLoading, isError } = useQuery({
    queryKey: ['public-quote', token],
    queryFn: () => publicQuotesApi.get(token),
    retry: false,
  })

  const onAnswered = (updated: PublicQuote) => {
    qc.setQueryData(['public-quote', token], updated)
    setSigning(false)
    setRejecting(false)
  }

  const accept = useMutation({
    mutationFn: ({ name, signature }: { name: string; signature: string }) => publicQuotesApi.accept(token, name, signature),
    onSuccess: onAnswered,
  })
  const reject = useMutation({
    mutationFn: () => publicQuotesApi.reject(token, reason),
    onSuccess: onAnswered,
  })

  const brand = quote?.issuerBrandColor || 'var(--color-brand-500)'
  const awaiting = quote?.status === 'Sent' && !quote.isExpired

  return (
    <div className="min-h-screen px-4 py-8 sm:py-12" style={{ backgroundColor: 'var(--color-canvas)' }}>
      {signing && quote && (
        <SignatureModal
          title="Aceitar orçamento"
          subtitle={`${quote.number} · ${formatCurrency(quote.total)}`}
          requireName
          defaultName={quote.clientName}
          declaration={`Ao assinar, aceito o orçamento ${quote.number} de ${quote.issuerName} nas condições apresentadas.`}
          confirmLabel="Aceitar e assinar"
          onConfirm={(signature, name) => accept.mutate({ name, signature })}
          onClose={() => setSigning(false)}
          isLoading={accept.isPending}
        />
      )}

      <div className="mx-auto w-full max-w-2xl space-y-5">
        {isLoading && (
          <div className="py-24 text-center">
            <div className="w-8 h-8 rounded-full border-2 animate-spin mx-auto" style={{ borderColor: 'var(--color-brand-500)', borderTopColor: 'transparent' }} />
          </div>
        )}

        {!isLoading && (isError || !quote) && (
          <div className="rounded-xl border px-5 py-8 text-center space-y-2" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
            <p className="font-medium" style={{ color: 'var(--color-ink)' }}>Link inválido ou expirado.</p>
            <p className="text-sm" style={{ color: 'var(--color-muted)' }}>Contacta a empresa que te enviou o orçamento para receberes um novo link.</p>
          </div>
        )}

        {quote && (
          <>
            {/* Issuer */}
            <header className="flex items-center gap-3">
              {quote.issuerLogoUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={`${API_BASE_URL}${quote.issuerLogoUrl}`} alt="" className="w-12 h-12 object-contain rounded-lg bg-white" />
              ) : (
                <span className="w-12 h-12 rounded-lg flex items-center justify-center text-lg font-bold text-white" style={{ backgroundColor: brand }}>
                  {quote.issuerName.charAt(0).toUpperCase()}
                </span>
              )}
              <div>
                <p className="font-bold" style={{ color: 'var(--color-ink)' }}>{quote.issuerName}</p>
                <p className="text-xs" style={{ color: 'var(--color-muted)' }}>
                  {[quote.issuerPhone, quote.issuerEmail].filter(Boolean).join(' · ')}
                </p>
              </div>
            </header>

            {/* Decision state */}
            {quote.status === 'Accepted' || quote.status === 'Invoiced' ? (
              <div className="rounded-xl px-5 py-4" style={{ backgroundColor: 'var(--color-success-50)', border: '1px solid var(--color-success-200)' }}>
                <p className="font-semibold" style={{ color: 'var(--color-success-700)' }}>Orçamento aceite ✓</p>
                <p className="text-sm mt-1" style={{ color: 'var(--color-success-700)' }}>
                  {quote.acceptedByName ? `Assinado por ${quote.acceptedByName}` : 'Aceite'}
                  {quote.clientDecisionAt ? ` a ${formatDateTime(quote.clientDecisionAt)}` : ''}. {quote.issuerName} vai entrar em contacto para agendar.
                </p>
              </div>
            ) : quote.status === 'Rejected' ? (
              <div className="rounded-xl px-5 py-4" style={{ backgroundColor: 'var(--color-neutral-100)', border: '1px solid var(--color-line)' }}>
                <p className="font-semibold" style={{ color: 'var(--color-ink)' }}>Orçamento recusado</p>
                <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>A tua resposta foi enviada a {quote.issuerName}. Obrigado.</p>
              </div>
            ) : quote.isExpired ? (
              <div className="rounded-xl px-5 py-4" style={{ backgroundColor: 'var(--color-brand-50)', border: '1px solid var(--color-brand-200)' }}>
                <p className="text-sm" style={{ color: 'var(--color-brand-700)' }}>
                  Este orçamento expirou{quote.validUntil ? ` a ${formatDate(quote.validUntil)}` : ''}. Contacta {quote.issuerName} para uma versão atualizada.
                </p>
              </div>
            ) : null}

            {/* Quote */}
            <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
              <div className="px-5 py-4 flex flex-wrap items-baseline justify-between gap-2" style={{ borderBottom: '1px solid var(--color-line)' }}>
                <div>
                  <p className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>Orçamento</p>
                  <h1 className="text-xl font-bold" style={{ color: 'var(--color-ink)', fontFamily: 'var(--font-jetbrains), monospace' }}>{quote.number}</h1>
                  <p className="text-sm" style={{ color: 'var(--color-muted)' }}>Para {quote.clientName}</p>
                </div>
                <div className="text-right text-sm" style={{ color: 'var(--color-muted)' }}>
                  <p>Data: {formatDate(quote.createdAt)}</p>
                  {quote.validUntil && <p>Válido até: {formatDate(quote.validUntil)}</p>}
                </div>
              </div>

              <ul className="divide-y divide-[var(--color-line)]">
                {quote.lines.map((line) => (
                  <li key={line.id} className="px-5 py-3 flex justify-between gap-4">
                    <div className="min-w-0">
                      <p className="text-sm font-medium" style={{ color: 'var(--color-ink)' }}>{line.description}</p>
                      <p className="text-xs" style={{ color: 'var(--color-muted)' }}>
                        {formatQuantity(line.quantity, line.unit)} × {formatUnitPrice(line.unitPrice)} · IVA {line.vatRate}%
                      </p>
                    </div>
                    <p className="text-sm font-semibold whitespace-nowrap" style={{ color: 'var(--color-ink)' }}>{formatCurrency(line.lineTotal)}</p>
                  </li>
                ))}
              </ul>

              <div className="px-5 py-4 space-y-1 text-sm" style={{ backgroundColor: 'var(--color-canvas)', borderTop: '1px solid var(--color-line)' }}>
                <div className="flex justify-between" style={{ color: 'var(--color-muted)' }}><span>Subtotal</span><span>{formatCurrency(quote.subTotal)}</span></div>
                <div className="flex justify-between" style={{ color: 'var(--color-muted)' }}><span>IVA</span><span>{formatCurrency(quote.vatTotal)}</span></div>
                {!!quote.discount && (
                  <div className="flex justify-between" style={{ color: 'var(--color-muted)' }}><span>Desconto</span><span>-{formatCurrency(quote.discount)}</span></div>
                )}
                <div className="flex justify-between font-bold text-base pt-2" style={{ color: 'var(--color-ink)', borderTop: '1px solid var(--color-line)' }}>
                  <span>Total</span><span>{formatCurrency(quote.total)}</span>
                </div>
              </div>

              {quote.notes && (
                <p className="px-5 py-4 text-sm whitespace-pre-line" style={{ color: 'var(--color-muted)', borderTop: '1px solid var(--color-line)' }}>{quote.notes}</p>
              )}
            </section>

            <a href={publicQuotesApi.pdfUrl(token)} className="inline-block text-sm font-medium underline" style={{ color: 'var(--color-ink)' }}>
              Descarregar PDF
            </a>

            {/* Answer */}
            {awaiting && !rejecting && (
              <div className="sticky bottom-4 grid grid-cols-1 sm:grid-cols-[1fr_auto] gap-3 rounded-xl border p-4 shadow-lg"
                style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
                <button
                  onClick={() => setSigning(true)}
                  className="rounded-lg px-5 py-3 font-bold text-white"
                  style={{ backgroundColor: 'var(--color-success-600)' }}
                >
                  Aceitar orçamento
                </button>
                <button
                  onClick={() => setRejecting(true)}
                  className="rounded-lg border px-5 py-3 text-sm font-medium"
                  style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-muted)' }}
                >
                  Recusar
                </button>
                {accept.isError && (
                  <p className="sm:col-span-2 text-sm" style={{ color: 'var(--color-danger-600)' }}>
                    {errorDetail(accept.error, 'Não foi possível registar a aceitação. Tenta novamente.')}
                  </p>
                )}
              </div>
            )}

            {awaiting && rejecting && (
              <div className="rounded-xl border p-4 space-y-3" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
                <label htmlFor="reject-reason" className="text-sm font-medium block" style={{ color: 'var(--color-ink)' }}>
                  Queres dizer porquê? (opcional)
                </label>
                <textarea
                  id="reject-reason"
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  maxLength={1000}
                  rows={3}
                  placeholder="Ex.: preço acima do previsto, prazo, já contratei outra empresa…"
                  className="form-input resize-none"
                  style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)' }}
                />
                {reject.isError && (
                  <p className="text-sm" style={{ color: 'var(--color-danger-600)' }}>{errorDetail(reject.error, 'Não foi possível enviar a resposta.')}</p>
                )}
                <div className="flex gap-3 justify-end">
                  <button onClick={() => setRejecting(false)} className="rounded-lg border px-4 py-2 text-sm" style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)' }}>
                    Voltar
                  </button>
                  <button
                    onClick={() => reject.mutate()}
                    disabled={reject.isPending}
                    className="rounded-lg px-4 py-2 text-sm font-semibold text-white disabled:opacity-60"
                    style={{ backgroundColor: 'var(--color-danger-600)' }}
                  >
                    {reject.isPending ? 'A enviar…' : 'Recusar orçamento'}
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}
