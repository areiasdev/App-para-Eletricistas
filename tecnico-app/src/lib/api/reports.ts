import { api } from './client'

export interface ProfitabilityByTechnician {
  userId: string
  technicianName: string
  totalInterventions: number
  completedInterventions: number
  materialsCost: number
  quotedRevenue: number
}

export interface ProfitabilityByClient {
  clientId: string
  clientName: string
  totalInterventions: number
  completedInterventions: number
  materialsCost: number
  quotedRevenue: number
}

export interface ProfitabilityReport {
  byTechnician: ProfitabilityByTechnician[]
  byClient: ProfitabilityByClient[]
  totalMaterialsCost: number
  totalQuotedRevenue: number
  from: string
  to: string
}

export const reportsApi = {
  getProfitability: (params?: { from?: string; to?: string }) =>
    api.get<ProfitabilityReport>('/reports/profitability', { params }).then((r) => r.data),
}
