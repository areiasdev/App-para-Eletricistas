'use client'

import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { InterventionForm, type InterventionFormValues } from '@/components/features/InterventionForm'
import { useCreateIntervention } from '@/hooks/useInterventions'
import { getErrorMessage } from '@/lib/api/client'
import type { InterventionMaterial } from '@/types'

export default function NovaIntervencaoPage() {
  const router = useRouter()
  const createIntervention = useCreateIntervention()

  const handleSubmit = (values: InterventionFormValues, materials: InterventionMaterial[]) => {
    createIntervention.mutate(
      {
        title: values.title,
        description: values.description || undefined,
        clientId: values.clientId,
        scheduledAt: values.scheduledAt || undefined,
        quoteId: values.quoteId || undefined,
        equipmentIds: values.equipmentIds,
        photos: values.photos ?? [],
        materials: materials.length > 0 ? materials : undefined,
      },
      {
        onSuccess: (iv) => router.push(`/dashboard/intervencoes/${iv.id}`),
      }
    )
  }

  return (
    <div className="max-w-3xl space-y-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm" style={{ color: 'var(--color-muted)' }}>
        <Link
          href="/dashboard/intervencoes"
          className="transition-colors duration-150"
          style={{ color: 'var(--color-muted)' }}
          onMouseEnter={(e) => (e.currentTarget.style.color = 'var(--color-ink)')}
          onMouseLeave={(e) => (e.currentTarget.style.color = 'var(--color-muted)')}
        >
          Intervenções
        </Link>
        <span style={{ color: 'var(--color-line-strong)' }}>/</span>
        <span style={{ color: 'var(--color-ink)' }}>Nova intervenção</span>
      </div>

      <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Nova Intervenção</h1>

      {createIntervention.isError && (
        <p className="text-sm rounded-lg px-4 py-3" style={{ color: '#dc2626', backgroundColor: '#fef2f2' }}>
          {getErrorMessage(createIntervention.error)}
        </p>
      )}

      <InterventionForm
        onSubmit={handleSubmit}
        isLoading={createIntervention.isPending}
        submitLabel="Criar Intervenção"
      />
    </div>
  )
}
