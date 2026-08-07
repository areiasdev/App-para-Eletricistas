import type { InterventionStatus } from '@/types'

const config: Record<InterventionStatus, { label: string; bg: string; color: string; dot: string }> = {
  Scheduled:  { label: 'Agendada',    bg: 'var(--color-info-50)', color: 'var(--color-info-700)', dot: 'var(--color-info-500)' },
  InProgress: { label: 'Em curso',    bg: 'var(--color-brand-50)', color: 'var(--color-brand-700)', dot: 'var(--color-brand-500)' },
  Completed:  { label: 'Concluída',   bg: 'var(--color-success-50)', color: 'var(--color-success-700)', dot: 'var(--color-success-500)' },
}

export function InterventionStatusBadge({ status }: { status: InterventionStatus }) {
  const c = config[status] ?? config.Scheduled
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
