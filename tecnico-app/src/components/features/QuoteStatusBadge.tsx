import type { QuoteStatus } from '@/types'

const config: Record<QuoteStatus, { label: string; bg: string; color: string; dot: string }> = {
  Draft:    { label: 'Rascunho',  bg: '#f4f4f5', color: '#52525b', dot: '#a1a1aa' },
  Sent:     { label: 'Enviado',   bg: '#eff6ff', color: '#1d4ed8', dot: '#3b82f6' },
  Accepted: { label: 'Aceite',    bg: '#f0fdf4', color: '#15803d', dot: '#22c55e' },
  Rejected: { label: 'Recusado',  bg: '#fff1f2', color: '#be123c', dot: '#f43f5e' },
  Invoiced: { label: 'Faturado',  bg: '#faf5ff', color: '#7e22ce', dot: '#a855f7' },
}

export function QuoteStatusBadge({ status }: { status: QuoteStatus }) {
  const c = config[status] ?? config.Draft
  return (
    <span
      className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium"
      style={{ backgroundColor: c.bg, color: c.color }}
    >
      <span className="w-1.5 h-1.5 rounded-full shrink-0" style={{ backgroundColor: c.dot }} />
      {c.label}
    </span>
  )
}
