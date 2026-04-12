'use client'

import { useState } from 'react'
import Link from 'next/link'
import { useClients, useDeleteClient } from '@/hooks/useClients'
import { formatDate } from '@/lib/utils/formatters'
import { getErrorMessage } from '@/lib/api/client'

export default function ClientesPage() {
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [debouncedSearch, setDebouncedSearch] = useState('')

  const { data, isLoading, isError, error } = useClients({
    search: debouncedSearch || undefined,
    page,
    pageSize: 20,
  })

  const deleteClient = useDeleteClient()

  const handleSearch = (value: string) => {
    setSearch(value)
    setPage(1)
    // debounce simples
    const t = setTimeout(() => setDebouncedSearch(value), 300)
    return () => clearTimeout(t)
  }

  const handleDelete = async (id: string, name: string) => {
    if (!confirm(`Tens a certeza que queres apagar o cliente "${name}"?`)) return
    deleteClient.mutate(id)
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Clientes</h1>
          <p className="text-sm text-gray-500 mt-1">
            {data ? `${data.totalCount} cliente${data.totalCount !== 1 ? 's' : ''}` : ''}
          </p>
        </div>
        <Link
          href="/dashboard/clientes/novo"
          className="inline-flex items-center gap-2 rounded-md bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-500 transition-colors"
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
        className="w-full max-w-sm rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
      />

      {/* Error */}
      {isError && (
        <p className="text-sm text-red-600 bg-red-50 rounded-md px-4 py-3">
          {getErrorMessage(error)}
        </p>
      )}

      {/* Table */}
      <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              {['Nome', 'Email', 'Telefone', 'Orçamentos', 'Criado em', ''].map((h) => (
                <th
                  key={h}
                  className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {isLoading &&
              Array.from({ length: 5 }).map((_, i) => (
                <tr key={i}>
                  {Array.from({ length: 6 }).map((_, j) => (
                    <td key={j} className="px-6 py-4">
                      <div className="h-4 bg-gray-100 rounded animate-pulse" />
                    </td>
                  ))}
                </tr>
              ))}

            {!isLoading && data?.items.length === 0 && (
              <tr>
                <td colSpan={6} className="px-6 py-12 text-center text-sm text-gray-400">
                  {debouncedSearch ? 'Nenhum cliente encontrado.' : 'Ainda não tens clientes. Cria o primeiro!'}
                </td>
              </tr>
            )}

            {data?.items.map((client) => (
              <tr key={client.id} className="hover:bg-gray-50">
                <td className="px-6 py-4 text-sm font-medium text-gray-900">
                  <Link href={`/dashboard/clientes/${client.id}`} className="hover:text-blue-600">
                    {client.name}
                  </Link>
                </td>
                <td className="px-6 py-4 text-sm text-gray-500">{client.email ?? '—'}</td>
                <td className="px-6 py-4 text-sm text-gray-500">{client.phone ?? '—'}</td>
                <td className="px-6 py-4 text-sm text-gray-500">{client.quoteCount}</td>
                <td className="px-6 py-4 text-sm text-gray-500">{formatDate(client.createdAt)}</td>
                <td className="px-6 py-4 text-right text-sm">
                  <div className="flex justify-end gap-3">
                    <Link
                      href={`/dashboard/clientes/${client.id}/editar`}
                      className="text-blue-600 hover:text-blue-800"
                    >
                      Editar
                    </Link>
                    <button
                      onClick={() => handleDelete(client.id, client.name)}
                      className="text-red-500 hover:text-red-700"
                    >
                      Apagar
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-gray-500">
          <span>
            Página {data.page} de {data.totalPages}
          </span>
          <div className="flex gap-2">
            <button
              disabled={!data.hasPreviousPage}
              onClick={() => setPage((p) => p - 1)}
              className="px-3 py-1 rounded border border-gray-300 disabled:opacity-40 hover:bg-gray-50"
            >
              ← Anterior
            </button>
            <button
              disabled={!data.hasNextPage}
              onClick={() => setPage((p) => p + 1)}
              className="px-3 py-1 rounded border border-gray-300 disabled:opacity-40 hover:bg-gray-50"
            >
              Próxima →
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
