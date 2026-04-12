'use client'

import { use } from 'react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { useClient, useUpdateClient } from '@/hooks/useClients'
import { ClientForm, type ClientFormValues } from '@/components/features/ClientForm'
import { getErrorMessage } from '@/lib/api/client'

export default function EditarClientePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params)
  const router = useRouter()
  const { data: client, isLoading } = useClient(id)
  const updateClient = useUpdateClient(id)

  const handleSubmit = (values: ClientFormValues) => {
    updateClient.mutate(
      {
        name: values.name,
        nif: values.nif || undefined,
        email: values.email || undefined,
        phone: values.phone || undefined,
        notes: values.notes,
        address: values.hasAddress && values.address
          ? {
              street: values.address.street,
              city: values.address.city,
              postalCode: values.address.postalCode,
              country: values.address.country ?? 'Portugal',
            }
          : undefined,
      },
      { onSuccess: () => router.push(`/dashboard/clientes/${id}`) }
    )
  }

  if (isLoading) return <div className="animate-pulse h-8 bg-gray-100 rounded w-1/3" />

  return (
    <div className="max-w-2xl space-y-6">
      <div className="flex items-center gap-2 text-sm text-gray-500">
        <Link href="/dashboard/clientes" className="hover:text-gray-700">Clientes</Link>
        <span>/</span>
        <Link href={`/dashboard/clientes/${id}`} className="hover:text-gray-700">{client?.name}</Link>
        <span>/</span>
        <span className="text-gray-900">Editar</span>
      </div>

      <h1 className="text-2xl font-bold text-gray-900">Editar Cliente</h1>

      {updateClient.isError && (
        <p className="text-sm text-red-600 bg-red-50 rounded-md px-4 py-3">
          {getErrorMessage(updateClient.error)}
        </p>
      )}

      {client && (
        <ClientForm
          defaultValues={{
            name: client.name,
            nif: client.nif ?? '',
            email: client.email ?? '',
            phone: client.phone ?? '',
            notes: client.notes ?? '',
            hasAddress: !!client.address,
            address: client.address
              ? {
                  street: client.address.street,
                  city: client.address.city,
                  postalCode: client.address.postalCode,
                  country: client.address.country,
                }
              : undefined,
          }}
          onSubmit={handleSubmit}
          isLoading={updateClient.isPending}
          submitLabel="Guardar alterações"
        />
      )}
    </div>
  )
}
