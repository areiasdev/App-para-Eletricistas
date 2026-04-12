'use client'

import { use } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useClient, useDeleteClient } from '@/hooks/useClients'
import { formatDate } from '@/lib/utils/formatters'

export default function ClienteDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params)
  const router = useRouter()
  const { data: client, isLoading } = useClient(id)
  const deleteClient = useDeleteClient()

  const handleDelete = () => {
    if (!confirm(`Tens a certeza que queres apagar o cliente "${client?.name}"?`)) return
    deleteClient.mutate(id, {
      onSuccess: () => router.push('/dashboard/clientes'),
    })
  }

  if (isLoading) {
    return (
      <div className="space-y-4 animate-pulse">
        <div className="h-8 bg-gray-100 rounded w-1/3" />
        <div className="h-4 bg-gray-100 rounded w-1/4" />
      </div>
    )
  }

  if (!client) {
    return (
      <div className="text-center py-12">
        <p className="text-gray-500">Cliente não encontrado.</p>
        <Link href="/dashboard/clientes" className="text-blue-600 text-sm hover:underline mt-2 inline-block">
          Voltar à lista
        </Link>
      </div>
    )
  }

  return (
    <div className="max-w-2xl space-y-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-gray-500">
        <Link href="/dashboard/clientes" className="hover:text-gray-700">Clientes</Link>
        <span>/</span>
        <span className="text-gray-900">{client.name}</span>
      </div>

      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{client.name}</h1>
          {client.nif && <p className="text-sm text-gray-500 mt-1">NIF: {client.nif}</p>}
        </div>
        <div className="flex gap-2">
          <Link
            href={`/dashboard/clientes/${id}/editar`}
            className="rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors"
          >
            Editar
          </Link>
          <button
            onClick={handleDelete}
            className="rounded-md border border-red-200 bg-white px-4 py-2 text-sm font-medium text-red-600 hover:bg-red-50 transition-colors"
          >
            Apagar
          </button>
        </div>
      </div>

      {/* Info cards */}
      <div className="bg-white rounded-lg border border-gray-200 divide-y divide-gray-100">
        {client.email && <InfoRow label="Email" value={client.email} />}
        {client.phone && <InfoRow label="Telefone" value={client.phone} />}
        {client.address && (
          <InfoRow
            label="Morada"
            value={`${client.address.street}, ${client.address.postalCode} ${client.address.city}`}
          />
        )}
        {client.notes && <InfoRow label="Notas" value={client.notes} />}
        <InfoRow label="Cliente desde" value={formatDate(client.createdAt)} />
      </div>
    </div>
  )
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex px-6 py-4 gap-6">
      <span className="text-sm font-medium text-gray-500 w-28 shrink-0">{label}</span>
      <span className="text-sm text-gray-900">{value}</span>
    </div>
  )
}
