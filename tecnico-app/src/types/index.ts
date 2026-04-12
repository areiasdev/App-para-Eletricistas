export type QuoteStatus = 'Draft' | 'Sent' | 'Accepted' | 'Rejected' | 'Invoiced'
export type InterventionStatus = 'Scheduled' | 'InProgress' | 'Completed'
export type Plan = 'Free' | 'Pro' | 'Team'

export interface User {
  id: string
  fullName: string
  email: string
  plan: Plan
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  refreshTokenExpiresAt: string
  user: User
}

export interface Client {
  id: string
  name: string
  nif?: string
  email?: string
  phone?: string
  address?: Address
  notes?: string
  createdAt: string
}

export interface Address {
  street: string
  city: string
  postalCode: string
  country: string
}

export interface Quote {
  id: string
  number: string
  status: QuoteStatus
  subTotal: number
  vatTotal: number
  total: number
  discount?: number
  notes?: string
  validUntil?: string
  signedAt?: string
  pdfUrl?: string
  client: Pick<Client, 'id' | 'name'>
  lines: QuoteLine[]
  createdAt: string
}

export interface QuoteLine {
  id: string
  description: string
  quantity: number
  unitPrice: number
  vatRate: number
}

export interface Equipment {
  id: string
  type: string
  brand?: string
  model?: string
  serialNumber?: string
  installedAt?: string
  nextMaintenance?: string
  notes?: string
  photos: string[]
  clientId: string
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
