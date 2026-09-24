import { describe, it, expect } from 'vitest'
import { Suspense } from 'react'
import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { API_BASE_URL } from '@/lib/config'
import PublicQuotePage from './page'

const QUOTE = {
  number: 'ORC-2026-0007', status: 'Sent', clientName: 'Maria Costa', issuerName: 'Eletro Silva Lda',
  createdAt: '2026-09-20T10:00:00', validUntil: null, notes: null,
  lines: [{ id: '1', description: 'Quadro elétrico', quantity: 1, unitPrice: 300, vatRate: 23, lineTotal: 369, unit: 'un' }],
  subTotal: 300, vatTotal: 69, discount: null, total: 369, isExpired: false,
}

// The page unwraps its route params with React's use(), which suspends once — render inside
// an awaited act() so that suspension resolves before assertions run.
async function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  const params = Promise.resolve({ token: 'tok' })
  await act(async () => { await params })
  return render(
    <QueryClientProvider client={qc}>
      <Suspense fallback={null}>
        <PublicQuotePage params={params} />
      </Suspense>
    </QueryClientProvider>,
  )
}

describe('public quote approval page', () => {
  it('shows the quote and lets the client decline with a reason', async () => {
    let sentReason = ''
    server.use(
      http.get(`${API_BASE_URL}/api/v1/quotes/public/tok`, () => HttpResponse.json(QUOTE)),
      http.post(`${API_BASE_URL}/api/v1/quotes/public/tok/reject`, async ({ request }) => {
        sentReason = ((await request.json()) as { reason: string }).reason
        return HttpResponse.json({ ...QUOTE, status: 'Rejected', rejectionReason: sentReason })
      }),
    )
    const user = userEvent.setup()
    await act(async () => { await renderPage() })

    expect(await screen.findByText('Quadro elétrico')).toBeInTheDocument()
    expect(screen.getByText('Eletro Silva Lda')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Recusar' }))
    await user.type(screen.getByLabelText(/porquê/i), 'Muito caro')
    await user.click(screen.getByRole('button', { name: /recusar orçamento/i }))

    expect(await screen.findByText('Orçamento recusado')).toBeInTheDocument()
    expect(sentReason).toBe('Muito caro')
  })

  it('explains an invalid link', async () => {
    server.use(http.get(`${API_BASE_URL}/api/v1/quotes/public/tok`, () => new HttpResponse(null, { status: 404 })))
    await act(async () => { await renderPage() })
    expect(await screen.findByText(/link inválido ou expirado/i)).toBeInTheDocument()
  })
})
