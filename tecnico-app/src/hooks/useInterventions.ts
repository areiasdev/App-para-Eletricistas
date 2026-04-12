import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  interventionsApi,
  type CreateInterventionRequest,
  type UpdateInterventionRequest,
} from '@/lib/api/interventions'
import type { InterventionStatus } from '@/types'

const KEY = 'interventions'

export function useInterventions(params?: {
  search?: string
  status?: InterventionStatus
  clientId?: string
  page?: number
  pageSize?: number
}) {
  return useQuery({
    queryKey: [KEY, params],
    queryFn: () => interventionsApi.list(params),
  })
}

export function useIntervention(id: string) {
  return useQuery({
    queryKey: [KEY, id],
    queryFn: () => interventionsApi.getById(id),
    enabled: !!id,
  })
}

export function useCreateIntervention() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateInterventionRequest) => interventionsApi.create(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}

export function useUpdateIntervention(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateInterventionRequest) => interventionsApi.update(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}

export function useUpdateInterventionStatus() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: InterventionStatus }) =>
      interventionsApi.updateStatus(id, status),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}

export function useDeleteIntervention() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => interventionsApi.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}
