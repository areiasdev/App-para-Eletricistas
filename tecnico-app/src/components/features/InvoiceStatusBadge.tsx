import type { InvoiceStatus } from '@/types'

const config: Record<InvoiceStatus, { label: string; bg: string; color: string; dot: string }> = {
  Issued:    { label: 'Emitida',    bg: 'var(--color-info-50)', color: 'var(--color-info-700)', dot: 'var(--color-info-500)' },
  Paid:      { label: 'Paga',       bg: 'var(--color-success-50)', color: 'var(--color-success-700)', dot: 'var(--color-success-500)' },
  Overdue:   { label: 'Em atraso',  bg: 'var(--color-brand-50)', color: 'var(--color-brand-700)', dot: 'var(--color-brand-500)' },
  Cancelled: { label: 'Cancelada',  bg: 'var(--color-neutral-100)', color: 'var(--color-neutral-600)', dot: 'var(--color-neutral-400)' },
}

export function InvoiceStatusBadge({ status }: { status: InvoiceStatus }) {
  const c = config[status] ?? config.Issued
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
