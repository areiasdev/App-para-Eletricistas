import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { usersApi, type UpdateProfileRequest } from '@/lib/api/users'

const PROFILE_KEY = 'profile'

export function useProfile() {
  return useQuery({
    queryKey: [PROFILE_KEY],
    queryFn: usersApi.getProfile,
  })
}

export function useUpdateProfile() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateProfileRequest) => usersApi.updateProfile(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: [PROFILE_KEY] }),
  })
}
