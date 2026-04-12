import { api } from './client'
import type { Intervention, InterventionStatus, PaginatedResult } from '@/types'

export interface InterventionListItem {
  id: string
  title: string
  status: InterventionStatus
  scheduledAt?: string
  completedAt?: string
  clientId: string
  clientName: string
  equipmentCount: number
  createdAt: string
}

export interface CreateInterventionRequest {
  title: string
  description?: string
  clientId: string
  scheduledAt?: string
  quoteId?: string
  equipmentIds: string[]
}

export interface UpdateInterventionRequest {
  title: string
  description?: string
  scheduledAt?: string
  technicianNotes?: string
  quoteId?: string
  equipmentIds: string[]
}

export const interventionsApi = {
  list: (params?: {
    search?: string
    status?: InterventionStatus
    clientId?: string
    page?: number
    pageSize?: number
  }) =>
    api
      .get<PaginatedResult<InterventionListItem>>('/interventions', { params })
      .then((r) => r.data),

  getById: (id: string) =>
    api.get<Intervention>(`/interventions/${id}`).then((r) => r.data),

  create: (data: CreateInterventionRequest) =>
    api.post<Intervention>('/interventions', data).then((r) => r.data),

  update: (id: string, data: UpdateInterventionRequest) =>
    api.put<Intervention>(`/interventions/${id}`, data).then((r) => r.data),

  updateStatus: (id: string, status: InterventionStatus) =>
    api.patch(`/interventions/${id}/status`, { status }),

  delete: (id: string) => api.delete(`/interventions/${id}`),
}
