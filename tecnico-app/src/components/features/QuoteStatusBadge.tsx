import type { QuoteStatus } from '@/types'

const config: Record<QuoteStatus, { label: string; bg: string; color: string; dot: string }> = {
  Draft:    { label: 'Rascunho',  bg: 'var(--color-neutral-100)', color: 'var(--color-neutral-600)', dot: 'var(--color-neutral-400)' },
  Sent:     { label: 'Enviado',   bg: 'var(--color-info-50)', color: 'var(--color-info-700)', dot: 'var(--color-info-500)' },
  Accepted: { label: 'Aceite',    bg: 'var(--color-success-50)', color: 'var(--color-success-700)', dot: 'var(--color-success-500)' },
  Rejected: { label: 'Recusado',  bg: 'var(--color-role-rose-bg)', color: 'var(--color-role-rose-text)', dot: 'var(--color-role-rose-dot)' },
  Invoiced: { label: 'Faturado',  bg: 'var(--color-role-purple-bg)', color: 'var(--color-role-purple-text)', dot: 'var(--color-role-purple-dot)' },
}

export function QuoteStatusBadge({ status }: { status: QuoteStatus }) {
  const c = config[status] ?? config.Draft
  return (
    <span
      className="inline-flex items-center gap-1.5 rounded-sm px-2 py-0.5 text-xs font-medium border whitespace-nowrap"
      style={{ backgroundColor: c.bg, color: c.color, borderColor: `color-mix(in srgb, ${c.dot} 35%, transparent)` }}
    >
      <span className="w-1.5 h-1.5 rounded-full shrink-0" style={{ backgroundColor: c.dot }} />
      {c.label}
    </span>
  )
}
