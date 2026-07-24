'use client'

import { use, useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useClient, useDeleteClient } from '@/hooks/useClients'
import { formatDate } from '@/lib/utils/formatters'
import { portal } from '@/lib/api/portal'
import { getErrorMessage } from '@/lib/api/client'

export default function ClienteDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params)
  const router = useRouter()
  const { data: client, isLoading } = useClient(id)
  const deleteClient = useDeleteClient()
  const [portalSending, setPortalSending] = useState(false)
  const [portalMsg, setPortalMsg] = useState<{ type: 'ok' | 'err'; text: string } | null>(null)

  const handleSendPortalAccess = async () => {
    setPortalSending(true)
    setPortalMsg(null)
    try {
      const res = await portal.sendAccess(id)
      setPortalMsg({ type: 'ok', text: res.message ?? 'Email enviado com sucesso.' })
    } catch (err) {
      setPortalMsg({ type: 'err', text: getErrorMessage(err) })
    } finally {
      setPortalSending(false)
    }
  }

  const handleDelete = () => {
    if (!confirm(`Tens a certeza que queres apagar o cliente "${client?.name}"?`)) return
    deleteClient.mutate(id, { onSuccess: () => router.push('/dashboard/clientes') })
  }

  if (isLoading) {
    return (
      <div className="max-w-2xl space-y-4 animate-pulse">
        <div className="h-8 rounded-lg w-1/3" style={{ backgroundColor: 'var(--color-line)' }} />
        <div className="h-4 rounded-lg w-1/4" style={{ backgroundColor: 'var(--color-line)' }} />
      </div>
    )
  }

  if (!client) {
    return (
      <div className="text-center py-16">
        <p style={{ color: 'var(--color-muted)' }}>Cliente não encontrado.</p>
        <Link href="/dashboard/clientes" className="text-sm mt-2 inline-block" style={{ color: 'var(--color-brand-500)' }}>
          Voltar à lista
        </Link>
      </div>
    )
  }

  return (
    <div className="max-w-2xl space-y-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm" style={{ color: 'var(--color-muted)' }}>
        <Link href="/dashboard/clientes"
          onMouseEnter={e => (e.currentTarget.style.color = 'var(--color-ink)')}
          onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-muted)')}
        >Clientes</Link>
        <span style={{ color: 'var(--color-line-strong)' }}>/</span>
        <span style={{ color: 'var(--color-ink)' }}>{client.name}</span>
      </div>

      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>{client.name}</h1>
          {client.nif && <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>NIF: {client.nif}</p>}
        </div>
        <div className="flex gap-2">
          <Link
            href={`/dashboard/clientes/${id}/editar`}
            className="rounded-lg border px-4 py-2 text-sm font-medium transition-all duration-150"
            style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
            onMouseEnter={e => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
            onMouseLeave={e => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
          >
            Editar
          </Link>
          <button
            onClick={handleDelete}
            className="rounded-lg border px-4 py-2 text-sm font-medium transition-all duration-150"
            style={{ borderColor: '#fecaca', color: '#dc2626', backgroundColor: 'var(--color-card)' }}
            onMouseEnter={e => (e.currentTarget.style.backgroundColor = '#fef2f2')}
            onMouseLeave={e => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
          >
            Apagar
          </button>
        </div>
      </div>

      {/* Info */}
      <div className="rounded-xl border divide-y overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        {client.email && <InfoRow label="Email" value={client.email} />}
        {client.phone && <InfoRow label="Telefone" value={client.phone} />}
        {client.address && (
          <InfoRow
            label="Morada"
            value={`${client.address.street}, ${client.address.postalCode} ${client.address.city}`}
          />
        )}
        {client.notes && <InfoRow label="Notas" value={client.notes} />}
        <InfoRow label="Cliente desde" value={formatDate(client.createdAt)} />
      </div>

      {/* Portal access */}
      {client.email && (
        <div className="space-y-2">
          <div className="flex items-center gap-3">
            <button
              onClick={handleSendPortalAccess}
              disabled={portalSending}
              className="rounded-lg border px-4 py-2 text-sm font-medium transition-all duration-150 disabled:opacity-60"
              style={{ borderColor: 'rgba(16,185,129,0.4)', color: '#34d399', backgroundColor: 'rgba(16,185,129,0.06)' }}
              onMouseEnter={e => (e.currentTarget.style.backgroundColor = 'rgba(16,185,129,0.12)')}
              onMouseLeave={e => (e.currentTarget.style.backgroundColor = 'rgba(16,185,129,0.06)')}
            >
              {portalSending ? 'A enviar...' : 'Enviar acesso ao portal →'}
            </button>
          </div>
          {portalMsg && (
            <p className="text-xs px-1" style={{ color: portalMsg.type === 'ok' ? '#34d399' : '#f87171' }}>
              {portalMsg.text}
            </p>
          )}
        </div>
      )}

      {/* Related links */}
      <div className="flex flex-wrap gap-3">
        {[
          { href: `/dashboard/orcamentos?clientId=${id}`, label: 'Ver orçamentos →' },
          { href: `/dashboard/equipamentos?clientId=${id}`, label: 'Ver equipamentos →' },
          { href: `/dashboard/intervencoes?clientId=${id}`, label: 'Ver intervenções →' },
        ].map(({ href, label }) => (
          <Link
            key={href}
            href={href}
            className="rounded-lg border px-4 py-2 text-sm font-medium transition-all duration-150"
            style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
            onMouseEnter={e => (e.currentTarget.style.backgroundColor = 'var(--color-canvas)')}
            onMouseLeave={e => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
          >
            {label}
          </Link>
        ))}
      </div>
    </div>
  )
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex px-6 py-4 gap-6" style={{ borderColor: 'var(--color-line)' }}>
      <span className="text-sm font-medium w-28 shrink-0" style={{ color: 'var(--color-muted)' }}>{label}</span>
      <span className="text-sm" style={{ color: 'var(--color-ink)' }}>{value}</span>
    </div>
  )
}
