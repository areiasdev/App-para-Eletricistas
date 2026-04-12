import { cn } from '@/lib/utils'
import type { QuoteStatus } from '@/types'

const config: Record<QuoteStatus, { label: string; cls: string }> = {
  Draft:    { label: 'Rascunho',  cls: 'bg-gray-100 text-gray-600' },
  Sent:     { label: 'Enviado',   cls: 'bg-blue-100 text-blue-700' },
  Accepted: { label: 'Aceite',    cls: 'bg-green-100 text-green-700' },
  Rejected: { label: 'Recusado',  cls: 'bg-red-100 text-red-600' },
  Invoiced: { label: 'Faturado',  cls: 'bg-purple-100 text-purple-700' },
}

export function QuoteStatusBadge({ status }: { status: QuoteStatus }) {
  const { label, cls } = config[status] ?? config.Draft
  return (
    <span className={cn('inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium', cls)}>
      {label}
    </span>
  )
}
