import { describe, it, expect } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { http, HttpResponse } from 'msw'
import type { ReactNode } from 'react'
import { server } from '@/test/msw/server'
import { useClients, useClient } from './useClients'

const BASE_URL = 'http://localhost:5000/api/v1'

function wrapper({ children }: { children: ReactNode }) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
}

describe('useClients', () => {
  it('returns the paginated client list from the API', async () => {
    server.use(
      http.get(`${BASE_URL}/clients`, () =>
        HttpResponse.json({
          items: [{ id: '1', name: 'Cliente A', quoteCount: 0, createdAt: '2026-01-01' }],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        })
      )
    )

    const { result } = renderHook(() => useClients(), { wrapper })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data?.items).toHaveLength(1)
    expect(result.current.data?.items[0].name).toBe('Cliente A')
  })

  it('surfaces an API error via isError', async () => {
    server.use(
      http.get(`${BASE_URL}/clients`, () => HttpResponse.json({ detail: 'Erro' }, { status: 500 }))
    )

    const { result } = renderHook(() => useClients(), { wrapper })

    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})

describe('useClient', () => {
  it('does not fetch when id is empty', () => {
    const { result } = renderHook(() => useClient(''), { wrapper })
    expect(result.current.fetchStatus).toBe('idle')
  })

  it('fetches a single client by id', async () => {
    server.use(
      http.get(`${BASE_URL}/clients/abc`, () =>
        HttpResponse.json({ id: 'abc', name: 'Cliente Único', createdAt: '2026-01-01', lines: [] })
      )
    )

    const { result } = renderHook(() => useClient('abc'), { wrapper })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data?.name).toBe('Cliente Único')
  })
})
