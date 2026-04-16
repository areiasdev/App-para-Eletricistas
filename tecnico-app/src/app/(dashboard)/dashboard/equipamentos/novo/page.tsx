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
        photos: values.photos ?? [],
      },
      { onSuccess: (eq) => router.push(`/dashboard/equipamentos/${eq.id}`) }
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
        <span style={{ color: 'var(--color-ink)' }}>Novo equipamento</span>
      </div>

      <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Novo Equipamento</h1>

      {createEquipment.isError && (
        <p className="text-sm rounded-xl px-4 py-3" style={{ color: '#dc2626', backgroundColor: '#fef2f2' }}>
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
