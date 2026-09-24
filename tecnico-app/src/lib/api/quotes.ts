import { api, downloadFile } from './client'
import type { Quote, QuoteStatus, PaginatedResult } from '@/types'

export interface QuoteListItem {
  id: string
  number: string
  status: QuoteStatus
  clientName: string
  total: number
  validUntil?: string
  createdAt: string
}

export interface QuoteLineRequest {
  description: string
  quantity: number
  unitPrice: number
  vatRate: number
  unit?: string
}

export interface CreateQuoteRequest {
  clientId: string
  discount?: number
  notes?: string
  validUntil?: string
  lines: QuoteLineRequest[]
}

export type UpdateQuoteRequest = CreateQuoteRequest

export const quotesApi = {
  list: (params?: {
    search?: string
    status?: QuoteStatus
    clientId?: string
    page?: number
    pageSize?: number
  }) =>
    api
      .get<PaginatedResult<QuoteListItem>>('/quotes', { params })
      .then((r) => r.data),

  getById: (id: string) =>
    api.get<Quote>(`/quotes/${id}`).then((r) => r.data),

  create: (data: CreateQuoteRequest) =>
    api.post<Quote>('/quotes', data).then((r) => r.data),

  update: (id: string, data: UpdateQuoteRequest) =>
    api.put<Quote>(`/quotes/${id}`, data).then((r) => r.data),

  updateStatus: (id: string, status: QuoteStatus) =>
    api.patch(`/quotes/${id}/status`, { status }),

  sign: (id: string, signatureDataUrl: string) =>
    api.post(`/quotes/${id}/sign`, { signatureDataUrl }),

  downloadPdf: (id: string, number: string) => downloadFile(`/quotes/${id}/pdf`, `orcamento-${number}.pdf`),

  sendEmail: (id: string) => api.post(`/quotes/${id}/send-email`),

  duplicate: (id: string) => api.post<Quote>(`/quotes/${id}/duplicate`).then((r) => r.data),

  delete: (id: string) => api.delete(`/quotes/${id}`),
}
