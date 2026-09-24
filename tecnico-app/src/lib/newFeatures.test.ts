import { describe, it, expect } from 'vitest'
import { calculateQuoteTotals, roundCents } from '@/components/features/QuoteForm'
import { formatQuantity, formatUnitPrice } from '@/lib/utils/formatters'
import { isValidPhotoUrl, photoSrc } from '@/lib/photos'
import { API_BASE_URL } from '@/lib/config'

describe('quote totals match the backend rounding', () => {
  it('rounds each line to the cent before summing (half away from zero)', () => {
    // 3 × 33,335 = 100,005 → 100,01 (not 100,00 as float/banker's rounding gives)
    const { subTotal, vatTotal, total } = calculateQuoteTotals(
      [{ quantity: 3, unitPrice: 33.335, vatRate: 23 }, { quantity: 1.5, unitPrice: 25.01, vatRate: 6 }],
      undefined,
    )
    expect(subTotal).toBe(137.53)
    expect(vatTotal).toBe(25.25)
    expect(total).toBe(162.78) // same value the API returned for this quote in the live test
  })

  it('roundCents handles float noise', () => {
    expect(roundCents(1.005)).toBe(1.01)
    expect(roundCents(0.1 + 0.2)).toBe(0.3)
  })
})

describe('formatters', () => {
  it('shows sub-cent unit prices', () => {
    expect(formatUnitPrice(0.4575)).toMatch(/0,4575/)
    expect(formatUnitPrice(35)).toMatch(/35,00/)
  })
  it('shows quantity with unit', () => {
    expect(formatQuantity(2.5, 'h')).toBe('2,5 h')
    expect(formatQuantity(25, 'm')).toBe('25 m')
  })
})

describe('photo urls', () => {
  it('accepts uploaded paths and https links only', () => {
    expect(isValidPhotoUrl('/uploads/photos/a/b.jpg')).toBe(true)
    expect(isValidPhotoUrl('https://x.pt/f.jpg')).toBe(true)
    expect(isValidPhotoUrl('/uploads/photos/../../secret')).toBe(false)
    expect(isValidPhotoUrl('http://x.pt/f.jpg')).toBe(false)
  })
  it('serves uploaded photos from the API host', () => {
    expect(photoSrc('/uploads/photos/a.jpg')).toBe(`${API_BASE_URL}/uploads/photos/a.jpg`)
    expect(photoSrc('https://x.pt/f.jpg')).toBe('https://x.pt/f.jpg')
  })
})
