export const formatCurrency = (value: number): string =>
  new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR' }).format(value)
// → "1.234,50 €"

export const formatDate = (date: string | Date): string =>
  new Intl.DateTimeFormat('pt-PT').format(new Date(date))
// → "12/04/2025"

export const formatDateTime = (date: string | Date): string =>
  new Intl.DateTimeFormat('pt-PT', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(date))

export const validateNif = (nif: string): boolean => {
  if (!/^\d{9}$/.test(nif)) return false
  const digits = nif.split('').map(Number)
  const checksum = digits.slice(0, 8).reduce((sum, d, i) => sum + d * (9 - i), 0)
  return (11 - (checksum % 11)) % 10 === digits[8]
}
