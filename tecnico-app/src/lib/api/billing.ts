import { api } from './client'

export interface BillingMe {
  plan: 'Free' | 'Pro' | 'Team' | 'Enterprise'
  hasActiveSubscription: boolean
}

export const billingApi = {
  getMe: () =>
    api.get<BillingMe>('/billing/me').then((r) => r.data),

  createCheckout: async (plan: 'Pro' | 'Team') => {
    const frontendUrl = window.location.origin
    const { data } = await api.post<{ url: string }>('/billing/checkout', {
      plan,
      frontendUrl,
    })
    window.location.href = data.url
  },

  createPortal: async () => {
    const frontendUrl = window.location.origin
    const { data } = await api.post<{ url: string }>('/billing/portal', {
      frontendUrl,
    })
    window.location.href = data.url
  },
}
