'use client'

import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { cn } from '@/lib/utils'
import { useAuthStore } from '@/stores/authStore'
import { useRouter } from 'next/navigation'
import { useQuery } from '@tanstack/react-query'
import { billingApi } from '@/lib/api/billing'

const navItems = [
  {
    href: '/dashboard',
    label: 'Dashboard',
    icon: (
      <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
        <rect x="1" y="1" width="6" height="6" rx="1" fill="currentColor" opacity=".4"/>
        <rect x="9" y="1" width="6" height="6" rx="1" fill="currentColor"/>
        <rect x="1" y="9" width="6" height="6" rx="1" fill="currentColor"/>
        <rect x="9" y="9" width="6" height="6" rx="1" fill="currentColor" opacity=".4"/>
      </svg>
    ),
  },
  {
    href: '/dashboard/clientes',
    label: 'Clientes',
    icon: (
      <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
        <circle cx="6" cy="5" r="3" fill="currentColor"/>
        <path d="M1 13c0-2.761 2.239-5 5-5h.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
        <circle cx="12" cy="10" r="2.5" stroke="currentColor" strokeWidth="1.5"/>
        <path d="M12 7.5V10l1.5 1.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
      </svg>
    ),
  },
  {
    href: '/dashboard/orcamentos',
    label: 'Orçamentos',
    icon: (
      <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
        <rect x="2" y="1" width="12" height="14" rx="1.5" stroke="currentColor" strokeWidth="1.5"/>
        <path d="M5 5h6M5 8h6M5 11h4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
      </svg>
    ),
  },
  {
    href: '/dashboard/equipamentos',
    label: 'Equipamentos',
    icon: (
      <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
        <path d="M6 2L2 6l2 2 1-1 5 5 1-1 2 2 4-4-2-2-1 1L9 3 8 4 6 2z" stroke="currentColor" strokeWidth="1.3" strokeLinejoin="round"/>
      </svg>
    ),
  },
  {
    href: '/dashboard/intervencoes',
    label: 'Intervenções',
    icon: (
      <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
        <rect x="1" y="3" width="14" height="11" rx="1.5" stroke="currentColor" strokeWidth="1.5"/>
        <path d="M5 1v4M11 1v4M1 7h14" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
      </svg>
    ),
  },
  {
    href: '/dashboard/planos',
    label: 'Plano',
    icon: (
      <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
        <path d="M8 1l1.8 3.6L14 5.6l-3 2.9.7 4.1L8 10.4l-3.7 2.2.7-4.1L2 5.6l4.2-.9L8 1z" stroke="currentColor" strokeWidth="1.3" strokeLinejoin="round"/>
      </svg>
    ),
  },
]

const planColors: Record<string, { bg: string; text: string }> = {
  Pro:  { bg: 'rgba(245,158,11,0.18)', text: 'var(--color-brand-400)' },
  Team: { bg: 'rgba(124,58,237,0.18)', text: '#a78bfa' },
  Free: { bg: 'rgba(255,255,255,0.07)', text: 'rgba(255,255,255,0.35)' },
}

export function Sidebar() {
  const pathname = usePathname()
  const router = useRouter()
  const { user, clearAuth } = useAuthStore()
  const { data: billing } = useQuery({
    queryKey: ['billing-me'],
    queryFn: billingApi.getMe,
    staleTime: 1000 * 60 * 5,
  })
  const plan = billing?.plan ?? 'Free'
  const planColor = planColors[plan] ?? planColors.Free

  const handleLogout = () => {
    clearAuth()
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    router.push('/login')
  }

  return (
    <aside className="w-56 flex flex-col shrink-0" style={{ backgroundColor: 'var(--color-sidebar)' }}>
      {/* Logo */}
      <div className="px-5 py-5 border-b border-white/8">
        <Link href="/dashboard" className="flex items-center gap-2.5 group">
          <span
            className="flex items-center justify-center w-7 h-7 rounded-md text-sm font-bold"
            style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
          >
            ⚡
          </span>
          <span className="text-sm font-semibold tracking-tight text-white/90">
            TécnicoApp
          </span>
        </Link>
      </div>

      {/* Nav */}
      <nav className="flex-1 px-3 py-4 space-y-0.5">
        {navItems.map((item) => {
          const isActive =
            item.href === '/dashboard'
              ? pathname === '/dashboard'
              : pathname.startsWith(item.href)

          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                'group flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-all duration-150',
                isActive
                  ? 'text-white'
                  : 'text-white/45 hover:text-white/80 hover:bg-white/6'
              )}
              style={isActive ? { backgroundColor: 'rgba(245, 158, 11, 0.15)' } : undefined}
            >
              {/* Active indicator */}
              <span
                className={cn(
                  'absolute left-0 w-0.5 h-5 rounded-r-full transition-all duration-150',
                  isActive ? 'opacity-100' : 'opacity-0'
                )}
                style={{ backgroundColor: 'var(--color-brand-500)' }}
              />
              <span
                className={cn(
                  'transition-colors duration-150',
                  isActive ? 'text-brand-400' : ''
                )}
                style={isActive ? { color: 'var(--color-brand-400)' } : undefined}
              >
                {item.icon}
              </span>
              {item.label}
            </Link>
          )
        })}
      </nav>

      {/* User */}
      <div className="px-4 py-4 border-t border-white/8">
        <div className="flex items-center gap-2.5 mb-3">
          <div
            className="w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold shrink-0"
            style={{ backgroundColor: 'var(--color-brand-600)', color: 'white' }}
          >
            {user?.fullName?.charAt(0).toUpperCase() ?? '?'}
          </div>
          <div className="min-w-0">
            <p className="text-xs font-medium text-white/80 truncate leading-tight">{user?.fullName}</p>
            <div className="flex items-center gap-1.5">
              <p className="text-xs text-white/35 truncate leading-tight">{user?.email}</p>
              <span
                className="shrink-0 rounded px-1 py-px text-[9px] font-bold uppercase tracking-wider leading-none"
                style={{ backgroundColor: planColor.bg, color: planColor.text }}
              >
                {plan}
              </span>
            </div>
          </div>
        </div>
        <button
          onClick={handleLogout}
          className="text-xs text-white/35 hover:text-white/70 transition-colors duration-150 w-full text-left"
        >
          Terminar sessão →
        </button>
      </div>
    </aside>
  )
}
