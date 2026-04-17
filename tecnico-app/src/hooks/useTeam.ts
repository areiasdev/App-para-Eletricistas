import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { teamApi } from '@/lib/api/team'
import type { TeamMember, UserRole } from '@/types'

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
    // B4: optimistic update — apply role change immediately, rollback on error
    onMutate: async ({ id, role }) => {
      await qc.cancelQueries({ queryKey: [KEY] })
      const prev = qc.getQueryData<TeamMember[]>([KEY])
      qc.setQueryData<TeamMember[]>([KEY], (old) =>
        old?.map((m) => (m.id === id ? { ...m, role } : m)) ?? []
      )
      return { prev }
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.prev) qc.setQueryData([KEY], ctx.prev)
    },
    onSettled: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}

export function useRemoveTeamMember() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => teamApi.remove(id),
    // B4: optimistic remove — hide row immediately, restore on error
    onMutate: async (id) => {
      await qc.cancelQueries({ queryKey: [KEY] })
      const prev = qc.getQueryData<TeamMember[]>([KEY])
      qc.setQueryData<TeamMember[]>([KEY], (old) => old?.filter((m) => m.id !== id) ?? [])
      return { prev }
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.prev) qc.setQueryData([KEY], ctx.prev)
    },
    onSettled: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}

export function useAcceptInvite() {
  return useMutation({
    mutationFn: (data: { token: string; fullName: string; newPassword: string }) =>
      teamApi.acceptInvite(data),
  })
}
