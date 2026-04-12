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
      },
      { onSuccess: () => router.push(`/dashboard/equipamentos/${id}`) }
    )
  }

  if (isLoading) return <div className="animate-pulse h-8 bg-gray-100 rounded w-1/3" />

  return (
    <div className="max-w-2xl space-y-6">
      <div className="flex items-center gap-2 text-sm text-gray-500">
        <Link href="/dashboard/equipamentos" className="hover:text-gray-700">Equipamentos</Link>
        <span>/</span>
        <Link href={`/dashboard/equipamentos/${id}`} className="hover:text-gray-700">{equipment?.type}</Link>
        <span>/</span>
        <span className="text-gray-900">Editar</span>
      </div>

      <h1 className="text-2xl font-bold text-gray-900">Editar Equipamento</h1>

      {updateEquipment.isError && (
        <p className="text-sm text-red-600 bg-red-50 rounded-md px-4 py-3">
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
