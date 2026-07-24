import { api } from './client'

export interface Profile {
  id: string
  fullName: string
  email: string
  companyName?: string
  nif?: string
  phone?: string
  logoUrl?: string
  brandColor?: string
}

export interface UpdateProfileRequest {
  fullName: string
  companyName?: string
  nif?: string
  phone?: string
  brandColor?: string
}

export const usersApi = {
  getProfile: () => api.get<Profile>('/users/me').then((r) => r.data),
  updateProfile: (data: UpdateProfileRequest) =>
    api.put<Profile>('/users/me', data).then((r) => r.data),
  uploadLogo: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return api
      .post<Profile>('/users/me/logo', formData, {
        // Must NOT set 'application/json' (the api instance's default) — axios needs to
        // auto-detect the FormData body itself so it can set the multipart boundary,
        // which a manually-set Content-Type would omit and break the upload.
        headers: { 'Content-Type': undefined },
      })
      .then((r) => r.data)
  },
}
