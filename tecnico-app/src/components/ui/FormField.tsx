interface FormFieldProps {
  label: string
  id?: string
  error?: string
  children: React.ReactNode
}

export function FormField({ label, id, error, children }: FormFieldProps) {
  return (
    <div>
      <label htmlFor={id} className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
        {label}
      </label>
      {children}
      {error && <p className="mt-1 text-xs" style={{ color: 'var(--color-danger-600)' }}>{error}</p>}
    </div>
  )
}
