import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import AceitarConvitePage from './page'

let searchParams = new URLSearchParams()
vi.mock('next/navigation', () => ({
  useSearchParams: () => searchParams,
  useRouter: () => ({ push: vi.fn(), replace: vi.fn() }),
}))

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <AceitarConvitePage />
    </QueryClientProvider>
  )
}

// Regression: the "missing token" error used to be set via a useEffect (setState-in-effect,
// an extra cascading render for something already knowable on the very first render). It's
// now a plain derived value — this asserts it's visible immediately, no effect tick needed.
describe('AceitarConvitePage', () => {
  it('shows an invalid-link error immediately when the URL has no token', () => {
    searchParams = new URLSearchParams()
    renderPage()

    expect(screen.getByText(/link de convite inválido ou expirado/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /ativar conta/i })).toBeDisabled()
  })

  it('shows no error and enables the form when the URL has a token', () => {
    searchParams = new URLSearchParams({ token: 'abc123' })
    renderPage()

    expect(screen.queryByText(/link de convite inválido ou expirado/i)).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: /ativar conta/i })).not.toBeDisabled()
  })
})
