import { api } from './client'
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

  downloadPdf: async (id: string, number: string) => {
    const response = await api.get(`/quotes/${id}/pdf`, { responseType: 'blob' })
    const url = URL.createObjectURL(response.data)
    const a = document.createElement('a')
    a.href = url
    a.download = `orcamento-${number}.pdf`
    a.click()
    URL.revokeObjectURL(url)
  },

  delete: (id: string) => api.delete(`/quotes/${id}`),
}
