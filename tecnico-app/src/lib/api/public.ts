import axios from 'axios'
import type { InvoiceStatus, InvoiceLine } from '@/types'
import { API_BASE_URL } from '@/lib/config'


// Separate, unauthenticated axios usage — no interceptors, no Bearer token — for the public
// "Pagar agora" page reached only via a magic-link token in the URL, never a logged-in session.

export interface PublicInvoice {
  id: string
  number: string
  status: InvoiceStatus
  clientName: string
  issuerCompanyName: string
  total: number
  dueDate: string
  issuedAt: string
  lines: InvoiceLine[]
}

export const publicApi = {
  getInvoiceByToken: (token: string) =>
    axios
      .get<PublicInvoice>(`${API_BASE_URL}/api/v1/invoices/public/${encodeURIComponent(token)}`)
      .then((r) => r.data),

  createInvoiceCheckout: (token: string) =>
    axios
      .post<{ url: string }>(`${API_BASE_URL}/api/v1/invoices/public/${encodeURIComponent(token)}/checkout`)
      .then((r) => r.data.url),
}
