import { api } from './client'
import type { Invoice, InvoiceStatus, PaginatedResult } from '@/types'

export interface InvoiceListItem {
  id: string
  number: string
  status: InvoiceStatus
  clientName: string
  total: number
  dueDate: string
  createdAt: string
}

export const invoicesApi = {
  list: (params?: {
    search?: string
    status?: InvoiceStatus
    clientId?: string
    page?: number
    pageSize?: number
  }) =>
    api
      .get<PaginatedResult<InvoiceListItem>>('/invoices', { params })
      .then((r) => r.data),

  getById: (id: string) =>
    api.get<Invoice>(`/invoices/${id}`).then((r) => r.data),

  createFromQuote: (quoteId: string) =>
    api.post<Invoice>(`/invoices/from-quote/${quoteId}`).then((r) => r.data),

  updateStatus: (id: string, status: InvoiceStatus) =>
    api.patch(`/invoices/${id}/status`, { status }),

  downloadPdf: async (id: string, number: string) => {
    const response = await api.get(`/invoices/${id}/pdf`, { responseType: 'blob' })
    const url = URL.createObjectURL(response.data)
    const a = document.createElement('a')
    a.href = url
    a.download = `fatura-${number}.pdf`
    a.click()
    URL.revokeObjectURL(url)
  },

  getPayLink: (id: string) =>
    api.post<{ url: string }>(`/invoices/${id}/pay-link`).then((r) => r.data.url),

  sendEmail: (id: string) => api.post(`/invoices/${id}/send-email`),
}
