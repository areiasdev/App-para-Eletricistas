import type { InterventionStatus } from '@/types'

const config: Record<InterventionStatus, { label: string; bg: string; color: string; dot: string }> = {
  Scheduled:  { label: 'Agendada',    bg: '#eff6ff', color: '#1d4ed8', dot: '#3b82f6' },
  InProgress: { label: 'Em curso',    bg: '#fffbeb', color: '#b45309', dot: '#f59e0b' },
  Completed:  { label: 'Concluída',   bg: '#f0fdf4', color: '#15803d', dot: '#22c55e' },
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
