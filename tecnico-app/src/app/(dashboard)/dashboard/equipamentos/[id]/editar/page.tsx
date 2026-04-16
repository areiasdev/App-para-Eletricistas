'use client'

import { use } from 'react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { useEquipment, useUpdateEquipment } from '@/hooks/useEquipment'
import { EquipmentForm, type EquipmentFormValues } from '@/components/features/EquipmentForm'
import { getErrorMessage } from '@/lib/api/client'

export default function EditarEquipamentoPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params)
  const router = useRouter()
  const { data: equipment, isLoading } = useEquipment(id)
  const updateEquipment = useUpdateEquipment(id)

  const handleSubmit = (values: EquipmentFormValues) => {
    updateEquipment.mutate(
      {
        type: values.type,
        brand: values.brand || undefined,
        model: values.model || undefined,
        serialNumber: values.serialNumber || undefined,
        installedAt: values.installedAt || undefined,
        nextMaintenance: values.nextMaintenance || undefined,
        notes: values.notes,
        photos: values.photos ?? [],
      },
      { onSuccess: () => router.push(`/dashboard/equipamentos/${id}`) }
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
        <Link href="/dashboard/equipamentos"
          onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-ink)')}
          onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-muted)')}
        >Equipamentos</Link>
        <span style={{ color: 'var(--color-line-strong)' }}>/</span>
        <Link href={`/dashboard/equipamentos/${id}`}
          onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-ink)')}
          onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-muted)')}
        >{equipment?.type}</Link>
        <span style={{ color: 'var(--color-line-strong)' }}>/</span>
        <span style={{ color: 'var(--color-ink)' }}>Editar</span>
      </div>

      <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Editar Equipamento</h1>

      {updateEquipment.isError && (
        <p className="text-sm rounded-xl px-4 py-3" style={{ color: '#dc2626', backgroundColor: '#fef2f2' }}>
          {getErrorMessage(updateEquipment.error)}
        </p>
      )}

      {equipment && (
        <EquipmentForm
          defaultValues={{
            clientId: equipment.clientId,
            type: equipment.type,
            brand: equipment.brand ?? '',
            model: equipment.model ?? '',
            serialNumber: equipment.serialNumber ?? '',
            installedAt: equipment.installedAt
              ? new Date(equipment.installedAt).toISOString().split('T')[0]
              : '',
            nextMaintenance: equipment.nextMaintenance
              ? new Date(equipment.nextMaintenance).toISOString().split('T')[0]
              : '',
            notes: equipment.notes ?? '',
            photos: equipment.photos ?? [],
          }}
          onSubmit={handleSubmit}
          isLoading={updateEquipment.isPending}
          submitLabel="Guardar alterações"
          lockClient
        />
      )}
    </div>
  )
}
