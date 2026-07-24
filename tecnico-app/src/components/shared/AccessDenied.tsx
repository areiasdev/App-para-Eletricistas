export function AccessDenied() {
  return (
    <div className="max-w-3xl">
      <div className="rounded-xl border p-10 text-center" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <p className="text-sm font-semibold" style={{ color: 'var(--color-ink)' }}>Acesso restrito</p>
        <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
          Esta página está disponível apenas para o proprietário e administradores.
        </p>
      </div>
    </div>
  )
}
