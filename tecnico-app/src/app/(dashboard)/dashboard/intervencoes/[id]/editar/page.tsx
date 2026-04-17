'use client'

import { use } from 'react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { useIntervention, useUpdateIntervention } from '@/hooks/useInterventions'
import { InterventionForm, type InterventionFormValues } from '@/components/features/InterventionForm'
import { getErrorMessage } from '@/lib/api/client'
import type { InterventionMaterial } from '@/types'

export default function EditarIntervencaoPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params)
  const router = useRouter()
  const { data: iv, isLoading } = useIntervention(id)
  const updateIntervention = useUpdateIntervention(id)

  const handleSubmit = (values: InterventionFormValues, materials: InterventionMaterial[]) => {
    updateIntervention.mutate(
      {
        title: values.title,
        description: values.description || undefined,
        scheduledAt: values.scheduledAt || undefined,
        technicianNotes: values.technicianNotes || undefined,
        quoteId: values.quoteId || undefined,
        equipmentIds: values.equipmentIds,
        photos: values.photos ?? [],
        materials: materials.length > 0 ? materials : undefined,
        assignedToUserId: values.assignedToUserId || undefined,
      },
      {
        onSuccess: () => router.push(`/dashboard/intervencoes/${id}`),
      }
    )
  }

  if (isLoading) {
    return (
      <div className="space-y-4 animate-pulse max-w-3xl">
        <div className="h-8 rounded-lg w-1/3" style={{ backgroundColor: 'var(--color-line)' }} />
        <div className="h-48 rounded-xl" style={{ backgroundColor: 'var(--color-line)' }} />
      </div>
    )
  }

  if (!iv) {
    return (
      <div className="text-center py-16">
        <p style={{ color: 'var(--color-muted)' }}>Intervenção não encontrada.</p>
        <Link
          href="/dashboard/intervencoes"
          className="text-sm mt-2 inline-block transition-colors duration-150"
          style={{ color: 'var(--color-brand-500)' }}
        >
          Voltar à lista
        </Link>
      </div>
    )
  }

  return (
    <div className="max-w-3xl space-y-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm" style={{ color: 'var(--color-muted)' }}>
        <Link
          href="/dashboard/intervencoes"
          className="transition-colors duration-150"
          onMouseEnter={(e) => (e.currentTarget.style.color = 'var(--color-ink)')}
          onMouseLeave={(e) => (e.currentTarget.style.color = 'var(--color-muted)')}
        >
          Intervenções
        </Link>
        <span style={{ color: 'var(--color-line-strong)' }}>/</span>
        <Link
          href={`/dashboard/intervencoes/${id}`}
          className="transition-colors duration-150"
          onMouseEnter={(e) => (e.currentTarget.style.color = 'var(--color-ink)')}
          onMouseLeave={(e) => (e.currentTarget.style.color = 'var(--color-muted)')}
        >
          {iv.title}
        </Link>
        <span style={{ color: 'var(--color-line-strong)' }}>/</span>
        <span style={{ color: 'var(--color-ink)' }}>Editar</span>
      </div>

      <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Editar Intervenção</h1>

      {updateIntervention.isError && (
        <p className="text-sm rounded-lg px-4 py-3" style={{ color: '#dc2626', backgroundColor: '#fef2f2' }}>
          {getErrorMessage(updateIntervention.error)}
        </p>
      )}

      <InterventionForm
        defaultValues={{
          title: iv.title,
          description: iv.description ?? '',
          clientId: iv.clientId,
          scheduledAt: iv.scheduledAt
            ? new Date(iv.scheduledAt).toISOString().slice(0, 16)
            : '',
          technicianNotes: iv.technicianNotes ?? '',
          quoteId: iv.quoteId ?? '',
          equipmentIds: iv.equipment.map((e) => e.id),
          photos: iv.photos ?? [],
          assignedToUserId: iv.assignedToUserId ?? '',
        }}
        defaultMaterials={iv.materials ?? []}
        onSubmit={handleSubmit}
        isLoading={updateIntervention.isPending}
        submitLabel="Guardar alterações"
      />
    </div>
  )
}
