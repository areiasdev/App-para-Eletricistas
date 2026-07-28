import type { InvoiceStatus } from '@/types'

const config: Record<InvoiceStatus, { label: string; bg: string; color: string; dot: string }> = {
  Issued:    { label: 'Emitida',    bg: '#eff6ff', color: '#1d4ed8', dot: '#3b82f6' },
  Paid:      { label: 'Paga',       bg: '#f0fdf4', color: '#15803d', dot: '#22c55e' },
  Overdue:   { label: 'Em atraso',  bg: '#fffbeb', color: '#b45309', dot: '#f59e0b' },
  Cancelled: { label: 'Cancelada',  bg: '#f4f4f5', color: '#52525b', dot: '#a1a1aa' },
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
