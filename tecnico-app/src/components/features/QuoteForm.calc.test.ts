import { describe, it, expect } from 'vitest'
import { calculateQuoteTotals } from './QuoteForm'

describe('calculateQuoteTotals', () => {
  it('sums subtotal as quantity times unit price across lines', () => {
    const { subTotal } = calculateQuoteTotals(
      [
        { quantity: 2, unitPrice: 10, vatRate: 23 },
        { quantity: 1, unitPrice: 5, vatRate: 23 },
      ],
      undefined
    )
    expect(subTotal).toBe(25)
  })

  it('applies each line its own VAT rate', () => {
    const { vatTotal } = calculateQuoteTotals(
      [
        { quantity: 1, unitPrice: 100, vatRate: 23 }, // 23
        { quantity: 1, unitPrice: 100, vatRate: 6 },  // 6
      ],
      undefined
    )
    expect(vatTotal).toBe(29)
  })

  it('subtracts the discount from subtotal plus VAT', () => {
    const { total } = calculateQuoteTotals(
      [{ quantity: 1, unitPrice: 100, vatRate: 23 }],
      10
    )
    // 100 + 23 - 10 = 113
    expect(total).toBe(113)
  })

  it('treats a missing discount as zero', () => {
    const { total } = calculateQuoteTotals([{ quantity: 1, unitPrice: 100, vatRate: 0 }], undefined)
    expect(total).toBe(100)
  })

  it('returns zeros for an empty/undefined line list', () => {
    expect(calculateQuoteTotals(undefined, undefined)).toEqual({ subTotal: 0, vatTotal: 0, total: 0 })
  })
})
