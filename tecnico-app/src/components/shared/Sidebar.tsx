'use client'

import { useState } from 'react'
import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { cn } from '@/lib/utils'
import { useAuthStore } from '@/stores/authStore'
import { useThemeStore } from '@/stores/themeStore'
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
    href: '/dashboard/equipa',
    label: 'Equipa',
    icon: (
      <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
        <circle cx="5.5" cy="5" r="2.5" stroke="currentColor" strokeWidth="1.4"/>
        <circle cx="10.5" cy="5" r="2.5" stroke="currentColor" strokeWidth="1.4"/>
        <path d="M1 13c0-2.209 2.015-4 4.5-4" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round"/>
        <path d="M7 13c0-2.209 1.791-4 4-4s4 1.791 4 4" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round"/>
      </svg>
    ),
    teamOnly: true,
  },
  {
    href: '/dashboard/relatorios',
    label: 'Relatórios',
    icon: (
      <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
        <rect x="1" y="8" width="3" height="7" rx="1" fill="currentColor" opacity=".5"/>
        <rect x="6" y="5" width="3" height="10" rx="1" fill="currentColor" opacity=".7"/>
        <rect x="11" y="2" width="3" height="13" rx="1" fill="currentColor"/>
      </svg>
    ),
    enterpriseOnly: true,
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
  {
    href: '/dashboard/perfil',
    label: 'Perfil',
    icon: (
      <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
        <circle cx="8" cy="5" r="3" stroke="currentColor" strokeWidth="1.5"/>
        <path d="M2 14c0-3.314 2.686-6 6-6s6 2.686 6 6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
      </svg>
    ),
  },
]

const planColors: Record<string, { bg: string; text: string }> = {
  Pro:        { bg: 'rgba(245,158,11,0.18)', text: 'var(--color-brand-400)' },
  Team:       { bg: 'rgba(124,58,237,0.18)', text: '#a78bfa' },
  Enterprise: { bg: 'rgba(16,185,129,0.18)', text: '#34d399' },
  Free:       { bg: 'rgba(255,255,255,0.07)', text: 'rgba(255,255,255,0.35)' },
}

function SidebarContent({ onNavClick }: { onNavClick?: () => void }) {
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
  const { theme, toggleTheme } = useThemeStore()

  const handleLogout = () => {
    clearAuth()
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    router.push('/login')
  }

  return (
    <div className="flex flex-col h-full" style={{ backgroundColor: 'var(--color-sidebar)' }}>
      {/* Logo */}
      <div className="px-5 py-5 border-b border-white/8">
        <Link href="/dashboard" className="flex items-center gap-2.5" onClick={onNavClick}>
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
          if ('teamOnly' in item && item.teamOnly && plan !== 'Team' && plan !== 'Enterprise') return null
          if ('enterpriseOnly' in item && item.enterpriseOnly && plan !== 'Enterprise') return null
          const isActive =
            item.href === '/dashboard'
              ? pathname === '/dashboard'
              : pathname.startsWith(item.href)

          return (
            <Link
              key={item.href}
              href={item.href}
              onClick={onNavClick}
              className={cn(
                'group flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-all duration-150',
                isActive
                  ? 'text-white'
                  : 'text-white/45 hover:text-white/80 hover:bg-white/6'
              )}
              style={isActive ? { backgroundColor: 'rgba(245, 158, 11, 0.15)' } : undefined}
            >
              <span
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
        <div className="flex items-center justify-between">
          <button
            onClick={handleLogout}
            className="text-xs text-white/35 hover:text-white/70 transition-colors duration-150"
          >
            Terminar sessão →
          </button>
          <button
            onClick={toggleTheme}
            title={theme === 'dark' ? 'Modo claro' : 'Modo escuro'}
            className="w-7 h-7 flex items-center justify-center rounded-md transition-colors duration-150"
            style={{ color: 'rgba(255,255,255,0.35)' }}
            onMouseEnter={e => (e.currentTarget.style.color = 'rgba(255,255,255,0.7)')}
            onMouseLeave={e => (e.currentTarget.style.color = 'rgba(255,255,255,0.35)')}
          >
            {theme === 'dark' ? (
              <svg width="15" height="15" viewBox="0 0 15 15" fill="none">
                <circle cx="7.5" cy="7.5" r="3" stroke="currentColor" strokeWidth="1.4"/>
                <path d="M7.5 1v1.5M7.5 12.5V14M1 7.5h1.5M12.5 7.5H14M3.2 3.2l1.05 1.05M10.75 10.75l1.05 1.05M10.75 4.25l1.05-1.05M3.2 11.8l1.05-1.05"
                  stroke="currentColor" strokeWidth="1.4" strokeLinecap="round"/>
              </svg>
            ) : (
              <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
                <path d="M12.5 9A6 6 0 0 1 5 1.5a6 6 0 1 0 7.5 7.5z" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
            )}
          </button>
        </div>
      </div>
    </div>
  )
}

export function Sidebar() {
  const [mobileOpen, setMobileOpen] = useState(false)

  return (
    <>
      {/* Desktop sidebar — always visible on lg+ */}
      <aside className="hidden lg:flex w-56 shrink-0 flex-col h-full">
        <SidebarContent />
      </aside>

      {/* Mobile top bar */}
      <div
        className="lg:hidden fixed top-0 left-0 right-0 z-30 flex items-center justify-between px-4 h-14 border-b"
        style={{ backgroundColor: 'var(--color-sidebar)', borderColor: 'rgba(255,255,255,0.08)' }}
      >
        <Link href="/dashboard" className="flex items-center gap-2">
          <span
            className="flex items-center justify-center w-7 h-7 rounded-md text-sm font-bold"
            style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
          >
            ⚡
          </span>
          <span className="text-sm font-semibold tracking-tight text-white/90">TécnicoApp</span>
        </Link>
        <button
          onClick={() => setMobileOpen(true)}
          className="w-9 h-9 flex items-center justify-center rounded-md transition-colors duration-150"
          style={{ color: 'rgba(255,255,255,0.6)' }}
          aria-label="Abrir menu"
        >
          <svg width="18" height="18" viewBox="0 0 18 18" fill="none">
            <path d="M2 4h14M2 9h14M2 14h14" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
          </svg>
        </button>
      </div>

      {/* Mobile drawer overlay */}
      {mobileOpen && (
        <>
          <div
            className="lg:hidden fixed inset-0 z-40 bg-black/50"
            onClick={() => setMobileOpen(false)}
          />
          <div
            className="lg:hidden fixed top-0 left-0 bottom-0 z-50 w-64"
            style={{ backgroundColor: 'var(--color-sidebar)' }}
          >
            <div className="flex items-center justify-between px-5 py-4 border-b border-white/8">
              <Link href="/dashboard" className="flex items-center gap-2" onClick={() => setMobileOpen(false)}>
                <span
                  className="flex items-center justify-center w-7 h-7 rounded-md text-sm font-bold"
                  style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
                >
                  ⚡
                </span>
                <span className="text-sm font-semibold tracking-tight text-white/90">TécnicoApp</span>
              </Link>
              <button
                onClick={() => setMobileOpen(false)}
                className="w-8 h-8 flex items-center justify-center rounded-md"
                style={{ color: 'rgba(255,255,255,0.5)' }}
                aria-label="Fechar menu"
              >
                <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
                  <path d="M1 1l12 12M13 1L1 13" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
                </svg>
              </button>
            </div>
            <div className="h-[calc(100%-57px)]">
              <SidebarContent onNavClick={() => setMobileOpen(false)} />
            </div>
          </div>
        </>
      )}
    </>
  )
}
