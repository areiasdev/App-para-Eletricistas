import { api } from './client'
import type { Client, PaginatedResult } from '@/types'

export interface ClientListItem {
  id: string
  name: string
  email?: string
  phone?: string
  quoteCount: number
  createdAt: string
}

export interface CreateClientRequest {
  name: string
  nif?: string
  email?: string
  phone?: string
  notes?: string
  address?: {
    street: string
    city: string
    postalCode: string
    country: string
  }
}

export type UpdateClientRequest = CreateClientRequest

export const clientsApi = {
  list: (params?: { search?: string; page?: number; pageSize?: number }) =>
    api
      .get<PaginatedResult<ClientListItem>>('/clients', { params })
      .then((r) => r.data),

  getById: (id: string) =>
    api.get<Client>(`/clients/${id}`).then((r) => r.data),

  create: (data: CreateClientRequest) =>
    api.post<Client>('/clients', data).then((r) => r.data),

  update: (id: string, data: UpdateClientRequest) =>
    api.put<Client>(`/clients/${id}`, data).then((r) => r.data),

  delete: (id: string) => api.delete(`/clients/${id}`),
}
