import { api } from './client'

export interface Profile {
  id: string
  fullName: string
  email: string
  companyName?: string
  nif?: string
  phone?: string
  logoUrl?: string
  plan: string
}

export interface UpdateProfileRequest {
  fullName: string
  companyName?: string
  nif?: string
  phone?: string
  logoUrl?: string
}

export const usersApi = {
  getProfile: () => api.get<Profile>('/users/me').then((r) => r.data),
  updateProfile: (data: UpdateProfileRequest) =>
    api.put<Profile>('/users/me', data).then((r) => r.data),
}
