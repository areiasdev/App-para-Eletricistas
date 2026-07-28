import { describe, it, expect, vi, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { useAuthStore } from '@/stores/authStore'
import LoginPage from './page'

const BASE_URL = 'http://localhost:5000/api/v1'

const push = vi.fn()
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push }),
}))

function renderLoginPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <LoginPage />
    </QueryClientProvider>
  )
}

afterEach(() => {
  push.mockClear()
  useAuthStore.getState().clearAuth()
})

describe('LoginPage', () => {
  it('logs in successfully and navigates to the dashboard', async () => {
    server.use(
      http.post(`${BASE_URL}/auth/login`, () =>
        HttpResponse.json({
          accessToken: 'access-token',
          csrfToken: 'csrf-token',
          user: { id: '1', fullName: 'Ana Silva', email: 'ana@x.pt', role: 'Owner' },
        })
      )
    )

    const user = userEvent.setup()
    renderLoginPage()

    await user.type(screen.getByPlaceholderText('tu@empresa.pt'), 'ana@x.pt')
    await user.type(screen.getByPlaceholderText('••••••••'), 'password123')
    await user.click(screen.getByRole('button', { name: /entrar/i }))

    await waitFor(() => expect(push).toHaveBeenCalledWith('/dashboard'))
    expect(useAuthStore.getState().user?.fullName).toBe('Ana Silva')
  })

  it('shows an error message and does not navigate on invalid credentials', async () => {
    server.use(
      http.post(`${BASE_URL}/auth/login`, () =>
        HttpResponse.json({ detail: 'Credenciais inválidas.' }, { status: 401 })
      )
    )

    const user = userEvent.setup()
    renderLoginPage()

    await user.type(screen.getByPlaceholderText('tu@empresa.pt'), 'ana@x.pt')
    await user.type(screen.getByPlaceholderText('••••••••'), 'wrong-password')
    await user.click(screen.getByRole('button', { name: /entrar/i }))

    expect(await screen.findByText('Credenciais inválidas.')).toBeInTheDocument()
    expect(push).not.toHaveBeenCalled()
  })
})
