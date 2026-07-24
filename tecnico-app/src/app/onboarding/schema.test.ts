import { describe, it, expect } from 'vitest'
import { companyInfoSchema } from './schema'

const valid = { companyName: 'Construções Silva Lda.' }

describe('companyInfoSchema companyName field', () => {
  it('requires a non-empty company name', () => {
    expect(companyInfoSchema.safeParse({ ...valid, companyName: '' }).success).toBe(false)
  })

  it('rejects a missing company name entirely', () => {
    const result = companyInfoSchema.safeParse({ nif: '', phone: '' })
    expect(result.success).toBe(false)
  })

  it('accepts a non-empty company name with no other fields', () => {
    expect(companyInfoSchema.safeParse(valid).success).toBe(true)
  })
})

describe('companyInfoSchema nif field', () => {
  it('accepts a checksum-valid NIF', () => {
    expect(companyInfoSchema.safeParse({ ...valid, nif: '123456789' }).success).toBe(true)
  })

  it('rejects a 9-digit NIF with the wrong checksum', () => {
    const result = companyInfoSchema.safeParse({ ...valid, nif: '123456780' })
    expect(result.success).toBe(false)
  })

  it('rejects a NIF that is not 9 digits before the checksum is even considered', () => {
    const result = companyInfoSchema.safeParse({ ...valid, nif: '12345678' })
    expect(result.success).toBe(false)
  })

  it('accepts an empty NIF (optional field)', () => {
    expect(companyInfoSchema.safeParse({ ...valid, nif: '' }).success).toBe(true)
  })

  it('accepts a missing NIF entirely', () => {
    expect(companyInfoSchema.safeParse(valid).success).toBe(true)
  })
})

describe('companyInfoSchema phone field', () => {
  it('accepts a missing phone', () => {
    expect(companyInfoSchema.safeParse(valid).success).toBe(true)
  })

  it('accepts an empty phone', () => {
    expect(companyInfoSchema.safeParse({ ...valid, phone: '' }).success).toBe(true)
  })

  it('accepts a provided phone', () => {
    expect(companyInfoSchema.safeParse({ ...valid, phone: '+351 912 345 678' }).success).toBe(true)
  })

  it('rejects a phone longer than 20 characters', () => {
    const result = companyInfoSchema.safeParse({ ...valid, phone: '1'.repeat(21) })
    expect(result.success).toBe(false)
  })
})
