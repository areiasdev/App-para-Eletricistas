import axios from 'axios'
import { API_BASE_URL } from '@/lib/config'
import type { QuoteLine, QuoteStatus } from '@/types'

// Client-facing quote approval — no session, the link token is the credential.
export interface PublicQuote {
  number: string
  status: QuoteStatus
  clientName: string
  issuerName: string
  issuerEmail?: string | null
  issuerPhone?: string | null
  issuerLogoUrl?: string | null
  issuerBrandColor?: string | null
  createdAt: string
  validUntil?: string | null
  notes?: string | null
  lines: QuoteLine[]
  subTotal: number
  vatTotal: number
  discount?: number | null
  total: number
  clientDecisionAt?: string | null
  acceptedByName?: string | null
  rejectionReason?: string | null
  isExpired: boolean
}

const base = (token: string) => `${API_BASE_URL}/api/v1/quotes/public/${encodeURIComponent(token)}`

export const publicQuotesApi = {
  get: (token: string) => axios.get<PublicQuote>(base(token)).then((r) => r.data),
  pdfUrl: (token: string) => `${base(token)}/pdf`,
  accept: (token: string, name: string, signatureDataUrl: string) =>
    axios.post<PublicQuote>(`${base(token)}/accept`, { name, signatureDataUrl }).then((r) => r.data),
  reject: (token: string, reason: string) =>
    axios.post<PublicQuote>(`${base(token)}/reject`, { reason }).then((r) => r.data),
}
