'use client'

import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { EquipmentForm, type EquipmentFormValues } from '@/components/features/EquipmentForm'
import { useCreateEquipment } from '@/hooks/useEquipment'
import { getErrorMessage } from '@/lib/api/client'

export default function NovoEquipamentoPage() {
  const router = useRouter()
  const createEquipment = useCreateEquipment()

  const handleSubmit = (values: EquipmentFormValues) => {
    createEquipment.mutate(
      {
        clientId: values.clientId,
        type: values.type,
        brand: values.brand || undefined,
        model: values.model || undefined,
        serialNumber: values.serialNumber || undefined,
        installedAt: values.installedAt || undefined,
        nextMaintenance: values.nextMaintenance || undefined,
        notes: values.notes,
      },
      { onSuccess: (eq) => router.push(`/dashboard/equipamentos/${eq.id}`) }
    )
  }

  return (
    <div className="max-w-2xl space-y-6">
      <div className="flex items-center gap-2 text-sm text-gray-500">
        <Link href="/dashboard/equipamentos" className="hover:text-gray-700">Equipamentos</Link>
        <span>/</span>
        <span className="text-gray-900">Novo equipamento</span>
      </div>

      <h1 className="text-2xl font-bold text-gray-900">Novo Equipamento</h1>

      {createEquipment.isError && (
        <p className="text-sm text-red-600 bg-red-50 rounded-md px-4 py-3">
          {getErrorMessage(createEquipment.error)}
        </p>
      )}

      <EquipmentForm
        onSubmit={handleSubmit}
        isLoading={createEquipment.isPending}
        submitLabel="Registar Equipamento"
      />
    </div>
  )
}
