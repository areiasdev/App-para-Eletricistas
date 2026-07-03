import axios from 'axios'
import { api } from './client'

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'

// Separate axios instance — uses portal JWT, not the regular auth token
let _portalToken: string | null = null

export function setPortalToken(token: string | null) {
  _portalToken = token
}

const portalApi = axios.create({
  baseURL: `${BASE_URL}/api/v1/portal`,
  headers: { 'Content-Type': 'application/json' },
})

portalApi.interceptors.request.use((config) => {
  if (_portalToken) config.headers.Authorization = `Bearer ${_portalToken}`
  return config
})

export interface PortalLoginResponse {
  accessToken: string
  clientName: string
  clientEmail: string | null
}

export interface PortalClientDto {
  id: string
  name: string
  email: string | null
  phone: string | null
  address: string | null
  notes: string | null
}

export interface PortalEquipmentDto {
  id: string
  name: string
  brand: string | null
  model: string | null
  serialNumber: string | null
  installDate: string | null
  nextMaintenanceDate: string | null
  notes: string | null
}

export interface PortalInterventionDto {
  id: string
  type: string
  status: string
  description: string | null
  scheduledAt: string | null
  completedAt: string | null
  technicianName: string | null
}

export interface PortalQuoteDto {
  id: string
  number: string
  status: string
  total: number
  validUntil: string | null
  createdAt: string
}

export const portal = {
  login: (token: string) =>
    axios
      .post<PortalLoginResponse>(`${BASE_URL}/api/v1/portal/login`, { token })
      .then((r) => r.data),

  me: () => portalApi.get<PortalClientDto>('/me').then((r) => r.data),

  equipment: () =>
    portalApi.get<PortalEquipmentDto[]>('/equipment').then((r) => r.data),

  interventions: () =>
    portalApi.get<PortalInterventionDto[]>('/interventions').then((r) => r.data),

  quotes: () => portalApi.get<PortalQuoteDto[]>('/quotes').then((r) => r.data),

  // Uses regular authenticated api (tech-side action)
  sendAccess: (clientId: string) =>
    api.post('/portal/send-access', { clientId }).then((r) => r.data),
}
