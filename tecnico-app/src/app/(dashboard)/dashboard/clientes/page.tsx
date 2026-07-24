'use client'

import { useState } from 'react'
import Link from 'next/link'
import { toast } from 'sonner'
import { useClients, useDeleteClient } from '@/hooks/useClients'
import { useDebouncedValue } from '@/hooks/useDebouncedValue'
import { useCanManage } from '@/hooks/useCanManage'
import { formatDate } from '@/lib/utils/formatters'
import { getErrorMessage } from '@/lib/api/client'

export default function ClientesPage() {
  const canManage = useCanManage()
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const debouncedSearch = useDebouncedValue(search, 300)

  const { data, isLoading, isError, error } = useClients({
    search: debouncedSearch || undefined,
    page,
    pageSize: 20,
  })

  const deleteClient = useDeleteClient()

  const handleSearch = (value: string) => {
    setSearch(value)
    setPage(1)
  }

  const handleDelete = (id: string, name: string) => {
    if (!confirm(`Tens a certeza que queres apagar o cliente "${name}"?`)) return
    deleteClient.mutate(id, {
      onSuccess: () => toast.success('Cliente apagado.'),
      onError: (err) => toast.error(getErrorMessage(err)),
    })
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Clientes</h1>
          <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
            {data ? `${data.totalCount} cliente${data.totalCount !== 1 ? 's' : ''}` : '\u00a0'}
          </p>
        </div>
        <Link
          href="/dashboard/clientes/novo"
          className="rounded-lg px-4 py-2 text-sm font-semibold transition-all duration-150 hover:brightness-110 active:scale-[0.99]"
          style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
        >
          + Novo Cliente
        </Link>
      </div>

      {/* Search */}
      <input
        type="search"
        placeholder="Pesquisar por nome, email ou telefone..."
        value={search}
        onChange={(e) => handleSearch(e.target.value)}
        className="w-full max-w-sm rounded-lg px-3 py-2 text-sm outline-none transition-all duration-150"
        style={{
          border: '1.5px solid var(--color-line-strong)',
          backgroundColor: 'var(--color-card)',
          color: 'var(--color-ink)',
        }}
        onFocus={e => { e.currentTarget.style.borderColor = 'var(--color-brand-500)'; e.currentTarget.style.boxShadow = '0 0 0 3px rgba(245,158,11,0.12)' }}
        onBlur={e => { e.currentTarget.style.borderColor = 'var(--color-line-strong)'; e.currentTarget.style.boxShadow = 'none' }}
      />

      {/* Error */}
      {isError && (
        <p className="text-sm rounded-xl px-4 py-3" style={{ color: '#dc2626', backgroundColor: '#fef2f2' }}>
          {getErrorMessage(error)}
        </p>
      )}

      {/* Table */}
      <div className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <table className="min-w-full">
          <thead>
            <tr style={{ borderBottom: '1px solid var(--color-line)', backgroundColor: 'var(--color-canvas)' }}>
              {['Nome', 'Email', 'Telefone', 'Orçamentos', 'Desde', ''].map(h => (
                <th key={h} className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {isLoading && Array.from({ length: 5 }).map((_, i) => (
              <tr key={i} style={{ borderBottom: '1px solid var(--color-line)' }}>
                {Array.from({ length: 6 }).map((_, j) => (
                  <td key={j} className="px-5 py-4">
                    <div className="h-4 rounded animate-pulse" style={{ backgroundColor: 'var(--color-line)' }} />
                  </td>
                ))}
              </tr>
            ))}

            {!isLoading && data?.items.length === 0 && (
              <tr>
                <td colSpan={6} className="px-5 py-14 text-center text-sm" style={{ color: 'var(--color-subtle)' }}>
                  {debouncedSearch
                    ? 'Nenhum cliente encontrado.'
                    : <>Ainda não tens clientes.{' '}
                        <Link href="/dashboard/clientes/novo" className="font-medium underline underline-offset-2" style={{ color: 'var(--color-brand-600)' }}>
                          Cria o primeiro
                        </Link>
                      </>}
                </td>
              </tr>
            )}

            {data?.items.map(client => (
              <tr
                key={client.id}
                style={{ borderBottom: '1px solid var(--color-line)' }}
                onMouseEnter={e => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
                onMouseLeave={e => (e.currentTarget.style.backgroundColor = '')}
              >
                <td className="px-5 py-3.5">
                  <Link
                    href={`/dashboard/clientes/${client.id}`}
                    className="text-sm font-medium transition-colors duration-150"
                    style={{ color: 'var(--color-ink)' }}
                    onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-brand-500)')}
                    onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-ink)')}
                  >
                    {client.name}
                  </Link>
                </td>
                <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-muted)' }}>{client.email ?? '—'}</td>
                <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-muted)' }}>{client.phone ?? '—'}</td>
                <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-muted)' }}>{client.quoteCount}</td>
                <td className="px-5 py-3.5 text-sm" style={{ color: 'var(--color-subtle)' }}>{formatDate(client.createdAt)}</td>
                <td className="px-5 py-3.5">
                  <div className="flex justify-end gap-3">
                    <Link
                      href={`/dashboard/clientes/${client.id}/editar`}
                      className="text-xs font-medium transition-colors duration-150"
                      style={{ color: 'var(--color-muted)' }}
                      onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-ink)')}
                      onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-muted)')}
                    >
                      Editar
                    </Link>
                    {canManage && (
                      <button
                        onClick={() => handleDelete(client.id, client.name)}
                        className="text-xs font-medium transition-colors duration-150"
                        style={{ color: 'var(--color-subtle)' }}
                        onMouseEnter={e => (e.currentTarget.style.color = '#dc2626')}
                        onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-subtle)')}
                      >
                        Apagar
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between text-sm" style={{ color: 'var(--color-muted)' }}>
          <span>Página {data.page} de {data.totalPages}</span>
          <div className="flex gap-2">
            <button
              disabled={!data.hasPreviousPage}
              onClick={() => setPage(p => p - 1)}
              className="px-3 py-1.5 rounded-lg border text-sm font-medium transition-colors duration-150 disabled:opacity-40"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
            >
              ← Anterior
            </button>
            <button
              disabled={!data.hasNextPage}
              onClick={() => setPage(p => p + 1)}
              className="px-3 py-1.5 rounded-lg border text-sm font-medium transition-colors duration-150 disabled:opacity-40"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
            >
              Próxima →
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
