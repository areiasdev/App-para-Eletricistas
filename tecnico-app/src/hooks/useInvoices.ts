import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { invoicesApi } from '@/lib/api/invoices'
import type { InvoiceStatus } from '@/types'

const INVOICES_KEY = 'invoices'
const QUOTES_KEY = 'quotes'

export function useInvoices(params?: {
  search?: string
  status?: InvoiceStatus
  clientId?: string
  page?: number
  pageSize?: number
}) {
  return useQuery({
    queryKey: [INVOICES_KEY, params],
    queryFn: () => invoicesApi.list(params),
  })
}

export function useInvoice(id: string) {
  return useQuery({
    queryKey: [INVOICES_KEY, id],
    queryFn: () => invoicesApi.getById(id),
    enabled: !!id,
  })
}

export function useCreateInvoiceFromQuote() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (quoteId: string) => invoicesApi.createFromQuote(quoteId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [INVOICES_KEY] })
      // Creating the invoice also flips the source quote's status to Invoiced.
      qc.invalidateQueries({ queryKey: [QUOTES_KEY] })
    },
  })
}

export function useUpdateInvoiceStatus() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: InvoiceStatus }) =>
      invoicesApi.updateStatus(id, status),
    onSuccess: () => qc.invalidateQueries({ queryKey: [INVOICES_KEY] }),
  })
}
