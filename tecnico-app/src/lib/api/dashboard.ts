import { api } from './client'
import type { QuoteStatus } from '@/types'

export interface RecentQuote {
  id: string
  number: string
  status: QuoteStatus
  clientName: string
  total: number
  createdAt: string
}

export interface UpcomingMaintenance {
  equipmentId: string
  type: string
  brand?: string
  model?: string
  clientName: string
  nextMaintenance: string
  daysUntil: number
}

export interface DashboardStats {
  totalClients: number
  totalQuotes: number
  draftQuotes: number
  sentQuotes: number
  acceptedQuotes: number
  totalRevenue: number
  pendingRevenue: number
  totalInterventions: number
  scheduledInterventions: number
  inProgressInterventions: number
  recentQuotes: RecentQuote[]
  upcomingMaintenance: UpcomingMaintenance[]
  /** Issued + overdue invoices still to be paid. */
  outstandingAmount: number
  overdueInvoices: number
  overdueAmount: number
  interventionsToday: number
}

export const dashboardApi = {
  getStats: () =>
    api.get<DashboardStats>('/dashboard/stats').then((r) => r.data),
}
