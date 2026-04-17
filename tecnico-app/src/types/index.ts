export type QuoteStatus = 'Draft' | 'Sent' | 'Accepted' | 'Rejected' | 'Invoiced'
export type InterventionStatus = 'Scheduled' | 'InProgress' | 'Completed'
export type Plan = 'Free' | 'Pro' | 'Team' | 'Enterprise'
export type UserRole = 'Owner' | 'Admin' | 'Technician' | 'Commercial'

export interface InterventionMaterial {
  name: string
  quantity: number
  unitCost: number
}

export interface InterventionEquipment {
  id: string
  type: string
  brand?: string
  model?: string
}

export interface Intervention {
  id: string
  title: string
  description?: string
  status: InterventionStatus
  scheduledAt?: string
  completedAt?: string
  technicianNotes?: string
  photos: string[]
  materials: InterventionMaterial[]
  assignedToUserId?: string
  assignedToName?: string
  clientId: string
  clientName: string
  quoteId?: string
  quoteNumber?: string
  equipment: InterventionEquipment[]
  createdAt: string
}

export interface TeamMember {
  id: string
  memberId: string
  fullName: string
  email: string
  role: UserRole
  isAccepted: boolean
  createdAt: string
  inviteToken?: string  // Only present on invite creation response
}

export interface User {
  id: string
  fullName: string
  email: string
  plan: Plan
}

export interface AuthResponse {
  accessToken: string
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
  signatureUrl?: string
  pdfUrl?: string
  clientId: string
  clientName: string
  lines: QuoteLine[]
  createdAt: string
}

export interface QuoteLine {
  id: string
  description: string
  quantity: number
  unitPrice: number
  vatRate: number
  lineTotal: number
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
  clientName: string
  createdAt: string
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
