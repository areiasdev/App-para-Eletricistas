'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { ClientForm, type ClientFormValues } from '@/components/features/ClientForm'
import { useCreateClient } from '@/hooks/useClients'
import { getErrorMessage, isPlanLimitError } from '@/lib/api/client'
import { UpgradeModal } from '@/components/features/UpgradeModal'

export default function NovoClientePage() {
  const router = useRouter()
  const createClient = useCreateClient()
  const [upgradeMessage, setUpgradeMessage] = useState<string | null>(null)

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
        onError: (err) => {
          if (isPlanLimitError(err)) setUpgradeMessage(getErrorMessage(err))
        },
      }
    )
  }

  return (
    <>
      {upgradeMessage && (
        <UpgradeModal message={upgradeMessage} onClose={() => setUpgradeMessage(null)} />
      )}
      <div className="max-w-2xl space-y-6">
        <div className="flex items-center gap-2 text-sm" style={{ color: 'var(--color-muted)' }}>
          <Link href="/dashboard/clientes" style={{ color: 'var(--color-muted)' }}>Clientes</Link>
          <span>/</span>
          <span style={{ color: 'var(--color-ink)' }}>Novo cliente</span>
        </div>

        <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Novo Cliente</h1>

        {createClient.isError && !isPlanLimitError(createClient.error) && (
          <p className="text-sm rounded-xl px-4 py-3" style={{ color: '#dc2626', backgroundColor: '#fef2f2' }}>
            {getErrorMessage(createClient.error)}
          </p>
        )}

        <ClientForm
          onSubmit={handleSubmit}
          isLoading={createClient.isPending}
          submitLabel="Criar Cliente"
        />
      </div>
    </>
  )
}
