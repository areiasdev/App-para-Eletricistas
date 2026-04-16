import { api } from './client'
import type { Equipment, PaginatedResult } from '@/types'

export interface EquipmentListItem {
  id: string
  type: string
  brand?: string
  model?: string
  serialNumber?: string
  nextMaintenance?: string
  clientId: string
  clientName: string
  createdAt: string
}

export interface CreateEquipmentRequest {
  clientId: string
  type: string
  brand?: string
  model?: string
  serialNumber?: string
  installedAt?: string
  nextMaintenance?: string
  notes?: string
  photos?: string[]
}

export interface UpdateEquipmentRequest {
  type: string
  brand?: string
  model?: string
  serialNumber?: string
  installedAt?: string
  nextMaintenance?: string
  notes?: string
  photos?: string[]
}

export const equipmentApi = {
  list: (params?: { search?: string; clientId?: string; page?: number; pageSize?: number }) =>
    api
      .get<PaginatedResult<EquipmentListItem>>('/equipment', { params })
      .then((r) => r.data),

  getById: (id: string) =>
    api.get<Equipment>(`/equipment/${id}`).then((r) => r.data),

  create: (data: CreateEquipmentRequest) =>
    api.post<Equipment>('/equipment', data).then((r) => r.data),

  update: (id: string, data: UpdateEquipmentRequest) =>
    api.put<Equipment>(`/equipment/${id}`, data).then((r) => r.data),

  delete: (id: string) => api.delete(`/equipment/${id}`),
}
