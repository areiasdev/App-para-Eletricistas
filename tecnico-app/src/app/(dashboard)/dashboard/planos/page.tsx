'use client'

import { useState, useEffect, Suspense } from 'react'
import { useSearchParams } from 'next/navigation'
import { useQuery } from '@tanstack/react-query'
import { billingApi } from '@/lib/api/billing'
import { getErrorMessage } from '@/lib/api/client'

const plans = [
  {
    id: 'Free' as const,
    name: 'Free',
    price: '€0',
    period: '/mês',
    description: 'Para começar a testar a plataforma.',
    features: [
      'Até 5 clientes',
      'Até 10 orçamentos/mês',
      'PDF de orçamentos',
      'Gestão de equipamentos',
      'Dashboard básico',
    ],
    cta: 'Plano atual',
    highlight: false,
  },
  {
    id: 'Pro' as const,
    name: 'Pro',
    price: '€29',
    period: '/mês',
    description: 'Para técnicos que trabalham a sério.',
    features: [
      'Clientes ilimitados',
      'Orçamentos ilimitados',
      'PDF + assinatura digital',
      'Intervenções ilimitadas',
      'Alertas de manutenção',
      'Dashboard avançado',
      'Suporte prioritário',
    ],
    cta: 'Subscrever Pro',
    highlight: true,
  },
  {
    id: 'Team' as const,
    name: 'Team',
    price: '€59',
    period: '/mês',
    description: 'Para equipas de 2 a 10 técnicos.',
    features: [
      'Tudo do Pro',
      'Até 10 utilizadores',
      'Gestão de equipa',
      'Relatórios de equipa',
      'Suporte dedicado',
    ],
    cta: 'Subscrever Team',
    highlight: false,
  },
]

function PlanosPageInner() {
  const searchParams = useSearchParams()
  const successParam = searchParams.get('success')
  const [loadingPlan, setLoadingPlan] = useState<'Pro' | 'Team' | null>(null)
  const [portalLoading, setPortalLoading] = useState(false)
  const [showSuccess, setShowSuccess] = useState(successParam === 'true')
  const [checkoutError, setCheckoutError] = useState<string | null>(null)

  const { data: billing, isLoading } = useQuery({
    queryKey: ['billing-me'],
    queryFn: billingApi.getMe,
  })

  useEffect(() => {
    if (showSuccess) {
      const t = setTimeout(() => setShowSuccess(false), 6000)
      return () => clearTimeout(t)
    }
  }, [showSuccess])

  const handleSubscribe = async (planId: 'Pro' | 'Team') => {
    setLoadingPlan(planId)
    setCheckoutError(null)
    try {
      await billingApi.createCheckout(planId)
    } catch (err) {
      setLoadingPlan(null)
      setCheckoutError(getErrorMessage(err))
    }
  }

  const handlePortal = async () => {
    setPortalLoading(true)
    setCheckoutError(null)
    try {
      await billingApi.createPortal()
    } catch (err) {
      setPortalLoading(false)
      setCheckoutError(getErrorMessage(err))
    }
  }

  const currentPlan = billing?.plan ?? 'Free'

  return (
    <div className="max-w-5xl space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Planos</h1>
        <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
          {isLoading
            ? 'A carregar...'
            : `Plano atual: `}
          {!isLoading && (
            <span className="font-semibold" style={{ color: 'var(--color-ink)' }}>{currentPlan}</span>
          )}
        </p>
      </div>

      {/* Error banner */}
      {checkoutError && (
        <div
          className="rounded-xl px-5 py-4 flex items-center gap-3"
          style={{ backgroundColor: '#fef2f2', border: '1px solid #fecaca' }}
        >
          <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
            <circle cx="7" cy="7" r="6" stroke="#dc2626" strokeWidth="1.5"/>
            <path d="M7 4v3M7 9.5v.5" stroke="#dc2626" strokeWidth="1.5" strokeLinecap="round"/>
          </svg>
          <p className="text-sm font-medium" style={{ color: '#dc2626' }}>{checkoutError}</p>
          <button onClick={() => setCheckoutError(null)} className="ml-auto text-xs" style={{ color: '#dc2626' }}>✕</button>
        </div>
      )}

      {/* Success banner */}
      {showSuccess && (
        <div
          className="rounded-xl px-5 py-4 flex items-center gap-3"
          style={{ backgroundColor: '#f0fdf4', border: '1px solid #bbf7d0' }}
        >
          <span style={{ color: '#16a34a' }}>✓</span>
          <p className="text-sm font-medium" style={{ color: '#15803d' }}>
            Subscrição activada com sucesso! O teu plano foi atualizado.
          </p>
        </div>
      )}

      {/* Manage subscription */}
      {billing?.hasActiveSubscription && (
        <div
          className="rounded-xl border px-5 py-4 flex items-center justify-between"
          style={{ backgroundColor: 'white', borderColor: 'var(--color-line)' }}
        >
          <div>
            <p className="text-sm font-medium" style={{ color: 'var(--color-ink)' }}>Gerir subscrição</p>
            <p className="text-xs mt-0.5" style={{ color: 'var(--color-muted)' }}>
              Altera o plano, método de pagamento ou cancela no painel Stripe.
            </p>
          </div>
          <button
            onClick={handlePortal}
            disabled={portalLoading}
            className="rounded-lg border px-4 py-2 text-sm font-medium transition-all duration-150 disabled:opacity-60"
            style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'white' }}
          >
            {portalLoading ? 'A abrir...' : 'Painel de faturação →'}
          </button>
        </div>
      )}

      {/* Plan cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {plans.map((plan) => {
          const isCurrent = currentPlan === plan.id
          const isPaid = plan.id !== 'Free'

          return (
            <div
              key={plan.id}
              className="rounded-2xl border flex flex-col"
              style={{
                backgroundColor: plan.highlight ? 'var(--color-sidebar)' : 'white',
                borderColor: plan.highlight ? 'var(--color-brand-500)' : 'var(--color-line)',
                boxShadow: plan.highlight ? '0 0 0 3px rgba(245,158,11,0.15)' : 'none',
              }}
            >
              {plan.highlight && (
                <div
                  className="rounded-t-2xl px-5 py-1.5 text-center"
                  style={{ backgroundColor: 'var(--color-brand-500)' }}
                >
                  <span className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-sidebar)' }}>
                    Mais popular
                  </span>
                </div>
              )}

              <div className="flex-1 p-7 flex flex-col">
                <p
                  className="text-xs font-bold uppercase tracking-widest mb-3"
                  style={{ color: plan.highlight ? 'rgba(245,158,11,0.8)' : 'var(--color-muted)' }}
                >
                  {plan.name}
                </p>

                <div className="flex items-end gap-1 mb-2">
                  <span
                    className="text-4xl font-bold"
                    style={{ color: plan.highlight ? 'white' : 'var(--color-ink)' }}
                  >
                    {plan.price}
                  </span>
                  <span
                    className="text-sm pb-1"
                    style={{ color: plan.highlight ? 'rgba(255,255,255,0.5)' : 'var(--color-muted)' }}
                  >
                    {plan.period}
                  </span>
                </div>

                <p
                  className="text-sm mb-6"
                  style={{ color: plan.highlight ? 'rgba(255,255,255,0.6)' : 'var(--color-muted)' }}
                >
                  {plan.description}
                </p>

                <ul className="space-y-2.5 flex-1">
                  {plan.features.map((f) => (
                    <li key={f} className="flex items-center gap-2.5 text-sm">
                      <span
                        className="w-4 h-4 rounded-full flex items-center justify-center shrink-0"
                        style={{
                          backgroundColor: plan.highlight ? 'rgba(245,158,11,0.2)' : 'var(--color-canvas)',
                          color: plan.highlight ? 'var(--color-brand-400)' : 'var(--color-brand-500)',
                        }}
                      >
                        <svg width="8" height="6" viewBox="0 0 8 6" fill="none">
                          <path d="M1 3l2 2 4-4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                        </svg>
                      </span>
                      <span style={{ color: plan.highlight ? 'rgba(255,255,255,0.8)' : 'var(--color-ink)' }}>
                        {f}
                      </span>
                    </li>
                  ))}
                </ul>

                <div className="mt-8">
                  {isCurrent ? (
                    <div
                      className="w-full rounded-lg px-4 py-2.5 text-sm font-semibold text-center"
                      style={{
                        backgroundColor: plan.highlight ? 'rgba(245,158,11,0.15)' : 'var(--color-canvas)',
                        color: plan.highlight ? 'var(--color-brand-400)' : 'var(--color-muted)',
                      }}
                    >
                      Plano atual ✓
                    </div>
                  ) : isPaid ? (
                    <button
                      onClick={() => handleSubscribe(plan.id as 'Pro' | 'Team')}
                      disabled={loadingPlan !== null}
                      className="w-full rounded-lg px-4 py-2.5 text-sm font-semibold transition-all duration-150 disabled:opacity-60 hover:brightness-110 active:scale-[0.99]"
                      style={{
                        backgroundColor: plan.highlight ? 'var(--color-brand-500)' : 'var(--color-ink)',
                        color: plan.highlight ? 'var(--color-sidebar)' : 'white',
                      }}
                    >
                      {loadingPlan === plan.id ? 'A redirecionar...' : plan.cta}
                    </button>
                  ) : (
                    <div
                      className="w-full rounded-lg px-4 py-2.5 text-sm font-semibold text-center"
                      style={{ backgroundColor: 'var(--color-canvas)', color: 'var(--color-muted)' }}
                    >
                      Grátis para sempre
                    </div>
                  )}
                </div>
              </div>
            </div>
          )
        })}
      </div>

      <p className="text-xs text-center" style={{ color: 'var(--color-subtle)' }}>
        Preços em EUR, IVA incluído. Pode cancelar a qualquer momento pelo painel de faturação Stripe.
      </p>
    </div>
  )
}

export default function PlanosPage() {
  return (
    <Suspense>
      <PlanosPageInner />
    </Suspense>
  )
}
