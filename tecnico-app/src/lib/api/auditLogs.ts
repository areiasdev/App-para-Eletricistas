import { api } from './client'

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

export interface PaginatedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
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
      .get<PaginatedResult<AuditLogDto>>('/audit-logs', { params })
      .then((r) => r.data),
}
