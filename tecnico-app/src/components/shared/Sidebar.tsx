'use client'

import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { cn } from '@/lib/utils'
import { useAuthStore } from '@/stores/authStore'
import { useRouter } from 'next/navigation'

const navItems = [
  { href: '/dashboard', label: 'Dashboard', icon: '📊' },
  { href: '/dashboard/clientes', label: 'Clientes', icon: '👥' },
  { href: '/dashboard/orcamentos', label: 'Orçamentos', icon: '📋' },
  { href: '/dashboard/equipamentos', label: 'Equipamentos', icon: '🔧' },
  { href: '/dashboard/intervencoes', label: 'Intervenções', icon: '🗓️' },
]

export function Sidebar() {
  const pathname = usePathname()
  const router = useRouter()
  const { user, clearAuth } = useAuthStore()

  const handleLogout = () => {
    clearAuth()
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    router.push('/login')
  }

  return (
    <aside className="w-56 bg-white border-r border-gray-200 flex flex-col">
      {/* Logo */}
      <div className="px-4 py-5 border-b border-gray-200">
        <span className="text-lg font-bold text-gray-900">⚡ TécnicoApp</span>
      </div>

      {/* Nav */}
      <nav className="flex-1 px-2 py-4 space-y-1">
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
                'flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors',
                isActive
                  ? 'bg-blue-50 text-blue-700'
                  : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
              )}
            >
              <span>{item.icon}</span>
              {item.label}
            </Link>
          )
        })}
      </nav>

      {/* User / logout */}
      <div className="px-4 py-4 border-t border-gray-200">
        <p className="text-xs font-medium text-gray-900 truncate">{user?.fullName}</p>
        <p className="text-xs text-gray-400 truncate mb-2">{user?.email}</p>
        <button
          onClick={handleLogout}
          className="text-xs text-gray-500 hover:text-red-600 transition-colors"
        >
          Terminar sessão
        </button>
      </div>
    </aside>
  )
}
