import { describe, it, expect, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { InterventionForm } from './InterventionForm'

const BASE_URL = 'http://localhost:5000/api/v1'

const CLIENT_A = { id: 'client-a', name: 'Cliente A' }
const CLIENT_B = { id: 'client-b', name: 'Cliente B' }
const QUOTE_A = { id: 'quote-a', number: 'ORC-2026-0001', status: 'Draft', clientName: 'Cliente A', total: 123.45, createdAt: '2026-01-01' }

function mockEndpoints() {
  server.use(
    http.get(`${BASE_URL}/clients`, () =>
      HttpResponse.json({ items: [CLIENT_A, CLIENT_B], totalCount: 2, page: 1, pageSize: 200 })
    ),
    http.get(`${BASE_URL}/quotes`, ({ request }) => {
      const clientId = new URL(request.url).searchParams.get('clientId')
      const items = clientId === CLIENT_A.id ? [QUOTE_A] : []
      return HttpResponse.json({ items, totalCount: items.length, page: 1, pageSize: 100 })
    }),
    http.get(`${BASE_URL}/equipment`, () =>
      HttpResponse.json({ items: [], totalCount: 0, page: 1, pageSize: 100 })
    ),
    http.get(`${BASE_URL}/team`, () => HttpResponse.json([]))
  )
}

function renderForm(props: Partial<React.ComponentProps<typeof InterventionForm>> = {}) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  const onSubmit = vi.fn()
  const utils = render(
    <QueryClientProvider client={queryClient}>
      <InterventionForm onSubmit={onSubmit} {...props} />
    </QueryClientProvider>
  )
  return { ...utils, onSubmit }
}

// Regression coverage for the bug where `quoteId` existed in the form's validation schema
// and was fully wired end-to-end (create/edit pages, backend), but no <select> was ever
// rendered for it — so no intervention could ever be linked to a quote through the UI,
// which meant revenue reports always showed 0.
describe('InterventionForm — quote linkage', () => {
  it('disables the quote select until a client is chosen', async () => {
    mockEndpoints()
    renderForm()

    const quoteSelect = await screen.findByLabelText(/orçamento associado/i)
    expect(quoteSelect).toBeDisabled()
  })

  it('enables the quote select and lists that client\'s quotes once a client is selected', async () => {
    mockEndpoints()
    const user = userEvent.setup()
    renderForm()

    const clientSelect = await screen.findByLabelText(/cliente \*/i)
    await screen.findByRole('option', { name: CLIENT_A.name })
    await user.selectOptions(clientSelect, CLIENT_A.id)

    const quoteSelect = await screen.findByLabelText(/orçamento associado/i)
    await waitFor(() => expect(quoteSelect).not.toBeDisabled())
    expect(await screen.findByRole('option', { name: /ORC-2026-0001/ })).toBeInTheDocument()
  })

  it('resets the selected quote when the client changes', async () => {
    mockEndpoints()
    const user = userEvent.setup()
    renderForm()

    const clientSelect = await screen.findByLabelText(/cliente \*/i);
    await screen.findByRole('option', { name: CLIENT_A.name })
    await user.selectOptions(clientSelect, CLIENT_A.id)

    const quoteSelect = await screen.findByLabelText<HTMLSelectElement>(/orçamento associado/i)
    await waitFor(() => expect(quoteSelect).not.toBeDisabled())
    await user.selectOptions(quoteSelect, QUOTE_A.id)
    expect(quoteSelect.value).toBe(QUOTE_A.id)

    await user.selectOptions(clientSelect, CLIENT_B.id)

    await waitFor(() => expect(quoteSelect.value).toBe(''))
  })

  it('submits the chosen quoteId as part of the form payload', async () => {
    mockEndpoints()
    const user = userEvent.setup()
    const { onSubmit } = renderForm()

    await user.type(await screen.findByPlaceholderText(/revisão anual/i), 'Reparação de fuga')
    const clientSelect = await screen.findByLabelText(/cliente \*/i)
    await screen.findByRole('option', { name: CLIENT_A.name })
    await user.selectOptions(clientSelect, CLIENT_A.id)

    const quoteSelect = await screen.findByLabelText<HTMLSelectElement>(/orçamento associado/i)
    await waitFor(() => expect(quoteSelect).not.toBeDisabled())
    await user.selectOptions(quoteSelect, QUOTE_A.id)

    await user.click(screen.getByRole('button', { name: /guardar/i }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalled())
    const [values] = onSubmit.mock.calls[0]
    expect(values.quoteId).toBe(QUOTE_A.id)
  })
})
