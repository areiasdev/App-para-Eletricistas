'use client'

import { use } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useEquipment, useDeleteEquipment } from '@/hooks/useEquipment'
import { formatDate } from '@/lib/utils/formatters'

export default function EquipamentoDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params)
  const router = useRouter()
  const { data: equipment, isLoading } = useEquipment(id)
  const deleteEquipment = useDeleteEquipment()

  const handleDelete = () => {
    if (!confirm(`Apagar o equipamento "${equipment?.type}"?`)) return
    deleteEquipment.mutate(id, { onSuccess: () => router.push('/dashboard/equipamentos') })
  }

  if (isLoading) {
    return (
      <div className="space-y-4 animate-pulse">
        <div className="h-8 bg-gray-100 rounded w-1/3" />
        <div className="h-4 bg-gray-100 rounded w-1/4" />
      </div>
    )
  }

  if (!equipment) {
    return (
      <div className="text-center py-12">
        <p className="text-gray-500">Equipamento não encontrado.</p>
        <Link href="/dashboard/equipamentos" className="text-blue-600 text-sm hover:underline mt-2 inline-block">
          Voltar à lista
        </Link>
      </div>
    )
  }

  return (
    <div className="max-w-2xl space-y-6">
      <div className="flex items-center gap-2 text-sm text-gray-500">
        <Link href="/dashboard/equipamentos" className="hover:text-gray-700">Equipamentos</Link>
        <span>/</span>
        <span className="text-gray-900">{equipment.type}</span>
      </div>

      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{equipment.type}</h1>
          {equipment.brand && (
            <p className="text-sm text-gray-500 mt-1">
              {[equipment.brand, equipment.model].filter(Boolean).join(' ')}
            </p>
          )}
        </div>
        <div className="flex gap-2">
          <Link
            href={`/dashboard/equipamentos/${id}/editar`}
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

      <div className="bg-white rounded-lg border border-gray-200 divide-y divide-gray-100">
        <InfoRow label="Cliente">
          <Link href={`/dashboard/clientes/${equipment.clientId}`} className="text-blue-600 hover:underline text-sm">
            {equipment.clientName}
          </Link>
        </InfoRow>
        {equipment.serialNumber && <InfoRow label="N.º série" value={equipment.serialNumber} />}
        {equipment.installedAt && <InfoRow label="Instalado em" value={formatDate(equipment.installedAt)} />}
        {equipment.nextMaintenance && (
          <InfoRow label="Próx. manutenção" value={formatDate(equipment.nextMaintenance)} />
        )}
        {equipment.notes && <InfoRow label="Notas" value={equipment.notes} />}
        <InfoRow label="Registado em" value={formatDate(equipment.createdAt)} />
      </div>
    </div>
  )
}

function InfoRow({ label, value, children }: { label: string; value?: string; children?: React.ReactNode }) {
  return (
    <div className="flex px-6 py-4 gap-6">
      <span className="text-sm font-medium text-gray-500 w-36 shrink-0">{label}</span>
      {children ?? <span className="text-sm text-gray-900">{value}</span>}
    </div>
  )
}
