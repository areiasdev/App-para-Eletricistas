'use client'

import { use, useState, Suspense } from 'react'
import { useSearchParams } from 'next/navigation'
import { useQuery } from '@tanstack/react-query'
import { publicApi } from '@/lib/api/public'
import { formatDate, formatCurrency } from '@/lib/utils/formatters'
import { InvoiceStatusBadge } from '@/components/features/InvoiceStatusBadge'
import { APP_INITIAL, APP_NAME } from '@/lib/config'

function PayPageInner({ token }: { token: string }) {
  const searchParams = useSearchParams()
  const success = searchParams.get('success') === 'true'
  const cancelled = searchParams.get('cancelled') === 'true'

  const [checkoutLoading, setCheckoutLoading] = useState(false)
  const [checkoutError, setCheckoutError] = useState<string | null>(null)

  const { data: invoice, isLoading, isError } = useQuery({
    queryKey: ['public-invoice', token],
    queryFn: () => publicApi.getInvoiceByToken(token),
    retry: false,
    enabled: !!token,
  })

  const handlePay = async () => {
    setCheckoutLoading(true)
    setCheckoutError(null)
    try {
      const url = await publicApi.createInvoiceCheckout(token)
      window.location.href = url
    } catch {
      setCheckoutError('Não foi possível iniciar o pagamento. Tenta novamente.')
      setCheckoutLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center px-4 py-10" style={{ backgroundColor: 'var(--color-canvas)' }}>
      <div className="w-full max-w-sm space-y-6">
        <div className="flex items-center justify-center gap-2">
          <span className="flex items-center justify-center w-8 h-8 rounded-md text-base font-bold"
            style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}>
            {APP_INITIAL}
          </span>
          <span className="text-lg font-bold" style={{ color: 'var(--color-ink)' }}>{APP_NAME}</span>
        </div>

        {isLoading && (
          <div className="space-y-3 text-center">
            <div className="w-8 h-8 rounded-full border-2 animate-spin mx-auto"
              style={{ borderColor: 'var(--color-brand-500)', borderTopColor: 'transparent' }} />
            <p className="text-sm" style={{ color: 'var(--color-muted)' }}>A carregar a fatura…</p>
          </div>
        )}

        {!isLoading && (isError || !invoice) && (
          <div className="rounded-xl border px-5 py-6 space-y-3 text-center"
            style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
            <p className="text-sm font-medium" style={{ color: 'var(--color-ink)' }}>
              Link de pagamento inválido ou expirado.
            </p>
            <p className="text-xs" style={{ color: 'var(--color-muted)' }}>
              Contacta a empresa que te enviou esta fatura para receber um novo link.
            </p>
          </div>
        )}

        {!isLoading && invoice && (
          <div className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
            <div className="px-6 py-5 space-y-1" style={{ borderBottom: '1px solid var(--color-line)' }}>
              <p className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
                {invoice.issuerCompanyName}
              </p>
              <div className="flex items-center gap-2 flex-wrap">
                <p className="text-xl font-bold" style={{ color: 'var(--color-ink)', fontFamily: 'var(--font-jetbrains), monospace' }}>
                  {invoice.number}
                </p>
                <InvoiceStatusBadge status={invoice.status} />
              </div>
              <p className="text-sm" style={{ color: 'var(--color-muted)' }}>Cliente: {invoice.clientName}</p>
            </div>

            <div className="px-6 py-5 space-y-2">
              <div className="flex justify-between text-sm" style={{ color: 'var(--color-muted)' }}>
                <span>Emitida em</span><span>{formatDate(invoice.issuedAt)}</span>
              </div>
              <div className="flex justify-between text-sm" style={{ color: 'var(--color-muted)' }}>
                <span>Vencimento</span><span>{formatDate(invoice.dueDate)}</span>
              </div>
              <div className="flex justify-between text-base font-bold pt-2" style={{ borderTop: '1px solid var(--color-line)', color: 'var(--color-ink)' }}>
                <span>Total</span><span style={{ color: 'var(--color-brand-600)' }}>{formatCurrency(invoice.total)}</span>
              </div>
            </div>

            <div className="px-6 pb-6">
              {invoice.status === 'Paid' && (
                <div className="rounded-lg px-4 py-3 text-center text-sm font-medium" style={{ backgroundColor: 'var(--color-success-50)', color: 'var(--color-success-700)' }}>
                  Esta fatura já foi paga. Obrigado!
                </div>
              )}

              {invoice.status === 'Cancelled' && (
                <div className="rounded-lg px-4 py-3 text-center text-sm font-medium" style={{ backgroundColor: 'var(--color-neutral-100)', color: 'var(--color-neutral-600)' }}>
                  Esta fatura foi cancelada e já não pode ser paga.
                </div>
              )}

              {invoice.status !== 'Paid' && invoice.status !== 'Cancelled' && (
                <div className="space-y-3">
                  {success && (
                    <div className="rounded-lg px-4 py-3 text-center text-sm" style={{ backgroundColor: 'var(--color-info-50)', color: 'var(--color-info-700)' }}>
                      Pagamento em processamento. Isto pode demorar um momento a confirmar — não é preciso pagar novamente.
                    </div>
                  )}
                  {cancelled && (
                    <div className="rounded-lg px-4 py-3 text-center text-sm" style={{ backgroundColor: 'var(--color-brand-50)', color: 'var(--color-brand-700)' }}>
                      Pagamento cancelado. Podes tentar novamente quando quiseres.
                    </div>
                  )}
                  {checkoutError && (
                    <div className="rounded-lg px-4 py-3 text-center text-sm" style={{ backgroundColor: 'var(--color-danger-50)', color: 'var(--color-danger-600)' }}>
                      {checkoutError}
                    </div>
                  )}
                  <button
                    onClick={handlePay}
                    disabled={checkoutLoading}
                    className="w-full rounded-lg px-4 py-3 text-sm font-bold transition-all duration-150 disabled:opacity-60"
                    style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
                  >
                    {checkoutLoading ? 'A abrir pagamento…' : 'Pagar agora →'}
                  </button>
                  <p className="text-xs text-center" style={{ color: 'var(--color-subtle)' }}>
                    Pagamento seguro processado pela Stripe · Cartão ou MB WAY
                  </p>
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

export default function PayPage({ params }: { params: Promise<{ token: string }> }) {
  const { token } = use(params)
  return (
    <Suspense>
      <PayPageInner token={token} />
    </Suspense>
  )
}
