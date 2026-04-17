import { api } from './client'
import type { TeamMember, UserRole } from '@/types'

export const teamApi = {
  list: () =>
    api.get<TeamMember[]>('/team').then((r) => r.data),

  invite: (data: { email: string; role: UserRole }) =>
    api.post<TeamMember>('/team/invite', data).then((r) => r.data),

  updateRole: (id: string, role: UserRole) =>
    api.patch<TeamMember>(`/team/${id}/role`, { role }).then((r) => r.data),

  remove: (id: string) =>
    api.delete(`/team/${id}`),

  acceptInvite: (data: { token: string; fullName: string; newPassword: string }) =>
    api.post('/team/accept-invite', data),
}
