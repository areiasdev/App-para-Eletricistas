'use client'

import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { ClientForm, type ClientFormValues } from '@/components/features/ClientForm'
import { useCreateClient } from '@/hooks/useClients'
import { getErrorMessage } from '@/lib/api/client'

export default function NovoClientePage() {
  const router = useRouter()
  const createClient = useCreateClient()

  const handleSubmit = (values: ClientFormValues) => {
    createClient.mutate(
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
      {
        onSuccess: (client) => router.push(`/dashboard/clientes/${client.id}`),
      }
    )
  }

  return (
    <div className="max-w-2xl space-y-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-gray-500">
        <Link href="/dashboard/clientes" className="hover:text-gray-700">Clientes</Link>
        <span>/</span>
        <span className="text-gray-900">Novo cliente</span>
      </div>

      <h1 className="text-2xl font-bold text-gray-900">Novo Cliente</h1>

      {createClient.isError && (
        <p className="text-sm text-red-600 bg-red-50 rounded-md px-4 py-3">
          {getErrorMessage(createClient.error)}
        </p>
      )}

      <ClientForm
        onSubmit={handleSubmit}
        isLoading={createClient.isPending}
        submitLabel="Criar Cliente"
      />
    </div>
  )
}
