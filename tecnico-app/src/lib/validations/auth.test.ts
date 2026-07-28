import { describe, it, expect } from 'vitest'
import { registerSchema, loginSchema } from './auth'

describe('registerSchema', () => {
  const valid = {
    fullName: 'João Silva',
    email: 'joao@empresa.pt',
    password: 'Password123',
    confirmPassword: 'Password123',
  }

  it('accepts a fully valid payload', () => {
    expect(registerSchema.safeParse(valid).success).toBe(true)
  })

  it('rejects mismatched passwords', () => {
    const result = registerSchema.safeParse({ ...valid, confirmPassword: 'Different123' })
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues.some((i) => i.path.includes('confirmPassword'))).toBe(true)
    }
  })

  it('rejects a password without an uppercase letter', () => {
    const result = registerSchema.safeParse({ ...valid, password: 'password123', confirmPassword: 'password123' })
    expect(result.success).toBe(false)
  })

  it('rejects a password without a digit', () => {
    const result = registerSchema.safeParse({ ...valid, password: 'Passwordonly', confirmPassword: 'Passwordonly' })
    expect(result.success).toBe(false)
  })

  it('rejects a password shorter than 8 characters', () => {
    const result = registerSchema.safeParse({ ...valid, password: 'P1x', confirmPassword: 'P1x' })
    expect(result.success).toBe(false)
  })

  it('rejects an invalid email', () => {
    const result = registerSchema.safeParse({ ...valid, email: 'not-an-email' })
    expect(result.success).toBe(false)
  })

  it('rejects a name shorter than 2 characters', () => {
    const result = registerSchema.safeParse({ ...valid, fullName: 'A' })
    expect(result.success).toBe(false)
  })
})

describe('loginSchema', () => {
  it('accepts a valid email and non-empty password', () => {
    expect(loginSchema.safeParse({ email: 'user@x.pt', password: 'anything' }).success).toBe(true)
  })

  it('rejects an invalid email', () => {
    expect(loginSchema.safeParse({ email: 'not-an-email', password: 'anything' }).success).toBe(false)
  })

  it('rejects an empty password', () => {
    expect(loginSchema.safeParse({ email: 'user@x.pt', password: '' }).success).toBe(false)
  })
})
