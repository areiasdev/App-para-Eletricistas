import { api, downloadFile } from './client'
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

  createFromIntervention: (interventionId: string) =>
    api.post<Invoice>(`/invoices/from-intervention/${interventionId}`).then((r) => r.data),

  /** CSV (Excel-ready, ";" separated) of invoices issued between the dates — for the accountant. */
  exportCsv: (from: string, to: string) =>
    downloadFile('/invoices/export', `faturas-${from}-a-${to}.csv`, { from, to }),

  updateStatus: (id: string, status: InvoiceStatus) =>
    api.patch(`/invoices/${id}/status`, { status }),

  downloadPdf: (id: string, number: string) => downloadFile(`/invoices/${id}/pdf`, `fatura-${number}.pdf`),

  getPayLink: (id: string) =>
    api.post<{ url: string }>(`/invoices/${id}/pay-link`).then((r) => r.data.url),

  sendEmail: (id: string) => api.post(`/invoices/${id}/send-email`),
}
