// Portuguese VAT (IVA) rates offered on quote lines. Continental rates; Madeira/Açores use
// lower rates and would be added here if an install needs them.
export const VAT_RATES = [
  { value: 23, label: '23% — taxa normal' },
  { value: 13, label: '13% — intermédia' },
  { value: 6, label: '6% — reduzida (ex.: obras de reabilitação em habitação)' },
  { value: 0, label: '0% — isento / autoliquidação' },
] as const

export const DEFAULT_VAT_RATE = 23
