// Units of measure offered on quote lines (free text is also accepted, max 10 chars).
export const UNITS = [
  { value: 'un', label: 'unidade' },
  { value: 'm', label: 'metro' },
  { value: 'm²', label: 'metro quadrado' },
  { value: 'm³', label: 'metro cúbico' },
  { value: 'ml', label: 'metro linear' },
  { value: 'h', label: 'hora' },
  { value: 'dia', label: 'dia' },
  { value: 'kg', label: 'quilograma' },
  { value: 'cx', label: 'caixa' },
  { value: 'rolo', label: 'rolo' },
  { value: 'vg', label: 'verba global' },
] as const

export const DEFAULT_UNIT = 'un'
