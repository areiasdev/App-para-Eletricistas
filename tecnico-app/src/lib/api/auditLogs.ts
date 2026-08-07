import { api } from './client'
import type { PaginatedResult } from '@/types'

export interface AuditLogDto {
  id: number
  entityType: string
  entityId: string
  action: string
  userId: string | null
  userEmail: string | null
  changes: string | null
  occurredAt: string
}

export const auditLogsApi = {
  get: (params: {
    page?: number
    pageSize?: number
    entityType?: string
    from?: string
    to?: string
  }) =>
    api
      .get<PaginatedResult<AuditLogDto>>('/auditlogs', { params })
      .then((r) => r.data),
}
