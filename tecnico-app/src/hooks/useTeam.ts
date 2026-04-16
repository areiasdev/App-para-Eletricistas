import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { teamApi } from '@/lib/api/team'
import type { UserRole } from '@/types'

const KEY = 'team'

export function useTeam() {
  return useQuery({
    queryKey: [KEY],
    queryFn: teamApi.list,
  })
}

export function useInviteTeamMember() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: { email: string; role: UserRole }) => teamApi.invite(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}

export function useUpdateTeamMemberRole() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, role }: { id: string; role: UserRole }) => teamApi.updateRole(id, role),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}

export function useRemoveTeamMember() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => teamApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}
