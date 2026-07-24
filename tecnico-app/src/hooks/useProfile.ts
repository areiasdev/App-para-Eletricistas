import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { usersApi, type UpdateProfileRequest } from '@/lib/api/users'
import { useAuthStore } from '@/stores/authStore'

const PROFILE_KEY = 'profile'

export function useProfile() {
  return useQuery({
    queryKey: [PROFILE_KEY],
    queryFn: usersApi.getProfile,
  })
}

export function useUpdateProfile() {
  const qc = useQueryClient()
  const updateUser = useAuthStore((s) => s.updateUser)
  return useMutation({
    mutationFn: (data: UpdateProfileRequest) => usersApi.updateProfile(data),
    onSuccess: (profile) => {
      qc.invalidateQueries({ queryKey: [PROFILE_KEY] })
      // Keep the sidebar/branding in sync without waiting for a token refresh.
      updateUser({ companyName: profile.companyName, logoUrl: profile.logoUrl, brandColor: profile.brandColor })
    },
  })
}

export function useUploadLogo() {
  const qc = useQueryClient()
  const updateUser = useAuthStore((s) => s.updateUser)
  return useMutation({
    mutationFn: (file: File) => usersApi.uploadLogo(file),
    onSuccess: (profile) => {
      qc.invalidateQueries({ queryKey: [PROFILE_KEY] })
      updateUser({ logoUrl: profile.logoUrl })
    },
  })
}
