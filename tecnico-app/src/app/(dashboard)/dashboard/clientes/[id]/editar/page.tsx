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

  if (isLoading) {
    return (
      <div className="max-w-2xl space-y-4 animate-pulse">
        <div className="h-8 rounded-lg w-1/3" style={{ backgroundColor: 'var(--color-line)' }} />
        <div className="h-4 rounded-lg w-1/4" style={{ backgroundColor: 'var(--color-line)' }} />
      </div>
    )
  }

  return (
    <div className="max-w-2xl space-y-6">
      <div className="flex items-center gap-2 text-sm" style={{ color: 'var(--color-muted)' }}>
        <Link href="/dashboard/clientes"
          onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-ink)')}
          onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-muted)')}
        >Clientes</Link>
        <span style={{ color: 'var(--color-line-strong)' }}>/</span>
        <Link href={`/dashboard/clientes/${id}`}
          onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-ink)')}
          onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-muted)')}
        >{client?.name}</Link>
        <span style={{ color: 'var(--color-line-strong)' }}>/</span>
        <span style={{ color: 'var(--color-ink)' }}>Editar</span>
      </div>

      <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Editar Cliente</h1>

      {updateClient.isError && (
        <p className="text-sm rounded-xl px-4 py-3" style={{ color: '#dc2626', backgroundColor: '#fef2f2' }}>
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
