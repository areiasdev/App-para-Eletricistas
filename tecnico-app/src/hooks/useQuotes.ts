import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { quotesApi, type CreateQuoteRequest, type UpdateQuoteRequest } from '@/lib/api/quotes'
import type { QuoteStatus } from '@/types'

const QUOTES_KEY = 'quotes'

export function useQuotes(params?: {
  search?: string
  status?: QuoteStatus
  clientId?: string
  page?: number
  pageSize?: number
}) {
  return useQuery({
    queryKey: [QUOTES_KEY, params],
    queryFn: () => quotesApi.list(params),
  })
}

export function useQuote(id: string) {
  return useQuery({
    queryKey: [QUOTES_KEY, id],
    queryFn: () => quotesApi.getById(id),
    enabled: !!id,
  })
}

export function useCreateQuote() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateQuoteRequest) => quotesApi.create(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUOTES_KEY] }),
  })
}

export function useUpdateQuote(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateQuoteRequest) => quotesApi.update(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUOTES_KEY] }),
  })
}

export function useUpdateQuoteStatus() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: QuoteStatus }) =>
      quotesApi.updateStatus(id, status),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUOTES_KEY] }),
  })
}

export function useSignQuote() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, signatureDataUrl }: { id: string; signatureDataUrl: string }) =>
      quotesApi.sign(id, signatureDataUrl),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUOTES_KEY] }),
  })
}

export function useDeleteQuote() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => quotesApi.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUOTES_KEY] }),
  })
}
