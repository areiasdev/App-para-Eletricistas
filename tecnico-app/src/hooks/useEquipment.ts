import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  equipmentApi,
  type CreateEquipmentRequest,
  type UpdateEquipmentRequest,
} from '@/lib/api/equipment'

const EQ_KEY = 'equipment'

export function useEquipmentList(params?: {
  search?: string
  clientId?: string
  page?: number
  pageSize?: number
}) {
  return useQuery({
    queryKey: [EQ_KEY, params],
    queryFn: () => equipmentApi.list(params),
  })
}

export function useEquipment(id: string) {
  return useQuery({
    queryKey: [EQ_KEY, id],
    queryFn: () => equipmentApi.getById(id),
    enabled: !!id,
  })
}

export function useCreateEquipment() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateEquipmentRequest) => equipmentApi.create(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: [EQ_KEY] }),
  })
}

export function useUpdateEquipment(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateEquipmentRequest) => equipmentApi.update(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: [EQ_KEY] }),
  })
}

export function useDeleteEquipment() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => equipmentApi.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [EQ_KEY] }),
  })
}
