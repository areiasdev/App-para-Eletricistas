import { describe, it, expect } from 'vitest'
import { clientSchema } from './ClientForm'
import { validateNif } from '@/lib/utils/formatters'

const valid = { name: 'Cliente Teste', hasAddress: false }

describe('validateNif', () => {
  it('accepts a NIF with a correct checksum digit', () => {
    expect(validateNif('123456789')).toBe(true)
  })

  it('rejects a NIF with an incorrect checksum digit', () => {
    expect(validateNif('123456780')).toBe(false)
  })

  it('rejects a string that is not 9 digits', () => {
    expect(validateNif('12345678')).toBe(false)
    expect(validateNif('1234567890')).toBe(false)
    expect(validateNif('12345678a')).toBe(false)
  })
})

describe('clientSchema nif field', () => {
  it('accepts a checksum-valid NIF', () => {
    expect(clientSchema.safeParse({ ...valid, nif: '123456789' }).success).toBe(true)
  })

  it('rejects a 9-digit NIF with the wrong checksum', () => {
    const result = clientSchema.safeParse({ ...valid, nif: '123456780' })
    expect(result.success).toBe(false)
  })

  it('rejects a NIF that is not 9 digits before the checksum is even considered', () => {
    const result = clientSchema.safeParse({ ...valid, nif: '12345678' })
    expect(result.success).toBe(false)
  })

  it('accepts an empty NIF (optional field)', () => {
    expect(clientSchema.safeParse({ ...valid, nif: '' }).success).toBe(true)
  })

  it('accepts a missing NIF entirely', () => {
    expect(clientSchema.safeParse(valid).success).toBe(true)
  })
})
