import { describe, it, expect, afterEach } from 'vitest'
import { renderHook } from '@testing-library/react'
import { useAuthStore } from '@/stores/authStore'
import { useCanManage } from './useCanManage'
import type { UserRole } from '@/types'

function setUserRole(role: UserRole) {
  useAuthStore.getState().setAuth(
    { id: '1', fullName: 'Test User', email: 'test@x.pt', role },
    'access-token',
    'csrf-token'
  )
}

afterEach(() => {
  useAuthStore.getState().clearAuth()
})

describe('useCanManage', () => {
  it('returns true for Owner', () => {
    setUserRole('Owner')
    expect(renderHook(() => useCanManage()).result.current).toBe(true)
  })

  it('returns true for Admin', () => {
    setUserRole('Admin')
    expect(renderHook(() => useCanManage()).result.current).toBe(true)
  })

  it('returns false for Technician', () => {
    setUserRole('Technician')
    expect(renderHook(() => useCanManage()).result.current).toBe(false)
  })

  it('returns false for Commercial', () => {
    setUserRole('Commercial')
    expect(renderHook(() => useCanManage()).result.current).toBe(false)
  })

  it('returns false when logged out', () => {
    expect(renderHook(() => useCanManage()).result.current).toBe(false)
  })
})
