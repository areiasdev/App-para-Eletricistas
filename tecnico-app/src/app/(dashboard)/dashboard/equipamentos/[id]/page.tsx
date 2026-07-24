'use client'

import { use } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useEquipment, useDeleteEquipment } from '@/hooks/useEquipment'
import { useCanManage } from '@/hooks/useCanManage'
import { formatDate } from '@/lib/utils/formatters'

export default function EquipamentoDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params)
  const router = useRouter()
  const canManage = useCanManage()
  const { data: equipment, isLoading } = useEquipment(id)
  const deleteEquipment = useDeleteEquipment()

  const handleDelete = () => {
    if (!confirm(`Apagar o equipamento "${equipment?.type}"?`)) return
    deleteEquipment.mutate(id, { onSuccess: () => router.push('/dashboard/equipamentos') })
  }

  if (isLoading) {
    return (
      <div className="max-w-2xl space-y-4 animate-pulse">
        <div className="h-8 rounded-lg w-1/3" style={{ backgroundColor: 'var(--color-line)' }} />
        <div className="h-4 rounded-lg w-1/4" style={{ backgroundColor: 'var(--color-line)' }} />
      </div>
    )
  }

  if (!equipment) {
    return (
      <div className="text-center py-16">
        <p style={{ color: 'var(--color-muted)' }}>Equipamento não encontrado.</p>
        <Link href="/dashboard/equipamentos" className="text-sm mt-2 inline-block" style={{ color: 'var(--color-brand-500)' }}>
          Voltar à lista
        </Link>
      </div>
    )
  }

  const daysUntilMaintenance = equipment.nextMaintenance
    ? Math.ceil((new Date(equipment.nextMaintenance).getTime() - Date.now()) / (1000 * 60 * 60 * 24))
    : null

  return (
    <div className="max-w-2xl space-y-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm" style={{ color: 'var(--color-muted)' }}>
        <Link href="/dashboard/equipamentos"
          onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-ink)')}
          onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-muted)')}
        >Equipamentos</Link>
        <span style={{ color: 'var(--color-line-strong)' }}>/</span>
        <span style={{ color: 'var(--color-ink)' }}>{equipment.type}</span>
      </div>

      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>{equipment.type}</h1>
          {equipment.brand && (
            <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
              {[equipment.brand, equipment.model].filter(Boolean).join(' · ')}
            </p>
          )}
        </div>
        <div className="flex gap-2">
          <Link
            href={`/dashboard/equipamentos/${id}/editar`}
            className="rounded-lg border px-4 py-2 text-sm font-medium transition-all duration-150"
            style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
            onMouseEnter={e => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
            onMouseLeave={e => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
          >
            Editar
          </Link>
          {canManage && (
            <button
              onClick={handleDelete}
              className="rounded-lg border px-4 py-2 text-sm font-medium transition-all duration-150"
              style={{ borderColor: '#fecaca', color: '#dc2626', backgroundColor: 'var(--color-card)' }}
              onMouseEnter={e => (e.currentTarget.style.backgroundColor = '#fef2f2')}
              onMouseLeave={e => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
            >
              Apagar
            </button>
          )}
        </div>
      </div>

      {/* Maintenance alert */}
      {daysUntilMaintenance !== null && daysUntilMaintenance <= 30 && (
        <div
          className="rounded-xl px-5 py-3.5 flex items-center gap-3"
          style={daysUntilMaintenance < 0
            ? { backgroundColor: '#fef2f2', border: '1px solid #fecaca' }
            : { backgroundColor: '#fef3c7', border: '1px solid #fde68a' }}
        >
          <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
            <circle cx="7" cy="7" r="6" stroke={daysUntilMaintenance < 0 ? '#dc2626' : '#b45309'} strokeWidth="1.5"/>
            <path d="M7 4v3M7 9.5v.5" stroke={daysUntilMaintenance < 0 ? '#dc2626' : '#b45309'} strokeWidth="1.5" strokeLinecap="round"/>
          </svg>
          <p className="text-sm font-medium" style={{ color: daysUntilMaintenance < 0 ? '#dc2626' : '#b45309' }}>
            {daysUntilMaintenance < 0
              ? `Manutenção vencida há ${Math.abs(daysUntilMaintenance)} dia${Math.abs(daysUntilMaintenance) !== 1 ? 's' : ''}`
              : `Manutenção em ${daysUntilMaintenance} dia${daysUntilMaintenance !== 1 ? 's' : ''}`}
          </p>
        </div>
      )}

      {/* Info */}
      <div className="rounded-xl border overflow-hidden divide-y" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <InfoRow label="Cliente">
          <Link
            href={`/dashboard/clientes/${equipment.clientId}`}
            className="text-sm transition-colors duration-150"
            style={{ color: 'var(--color-brand-600)' }}
            onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-brand-500)')}
            onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-brand-600)')}
          >
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

      {/* Photo gallery */}
      {equipment.photos.length > 0 && (
        <div className="rounded-xl border p-6 space-y-4" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
          <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
            Fotos ({equipment.photos.length})
          </h2>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            {equipment.photos.map((url, i) => (
              <a
                key={i}
                href={url}
                target="_blank"
                rel="noopener noreferrer"
                className="block rounded-lg overflow-hidden border aspect-video relative group"
                style={{ borderColor: 'var(--color-line)' }}
              >
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={url}
                  alt={`Foto ${i + 1}`}
                  className="w-full h-full object-cover transition-transform duration-200 group-hover:scale-105"
                  onError={e => {
                    const parent = e.currentTarget.parentElement
                    if (parent) {
                      e.currentTarget.style.display = 'none'
                      parent.innerHTML = `<div class="w-full h-full flex items-center justify-center text-xs" style="background:var(--color-canvas);color:var(--color-muted)">Sem pré-visualização</div>`
                    }
                  }}
                />
                <div
                  className="absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity duration-150 flex items-center justify-center"
                  style={{ backgroundColor: 'rgba(0,0,0,0.35)' }}
                >
                  <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
                    <path d="M10 3H3v14h14v-7M13 3h4v4M20 0l-8 8" stroke="white" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                  </svg>
                </div>
              </a>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

function InfoRow({ label, value, children }: { label: string; value?: string; children?: React.ReactNode }) {
  return (
    <div className="flex px-6 py-4 gap-6">
      <span className="text-sm font-medium w-36 shrink-0" style={{ color: 'var(--color-muted)' }}>{label}</span>
      {children ?? <span className="text-sm" style={{ color: 'var(--color-ink)' }}>{value}</span>}
    </div>
  )
}
