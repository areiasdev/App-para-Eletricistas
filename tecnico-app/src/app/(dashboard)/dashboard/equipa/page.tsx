'use client'

import { useState } from 'react'
import { toast } from 'sonner'
import { useTeam, useInviteTeamMember, useUpdateTeamMemberRole, useRemoveTeamMember } from '@/hooks/useTeam'
import { getErrorMessage } from '@/lib/api/client'
import type { UserRole, TeamMember } from '@/types'

const roleLabels: Record<UserRole, string> = {
  Owner: 'Proprietário',
  Admin: 'Administrador',
  Technician: 'Técnico',
  Commercial: 'Comercial',
}

const roleColors: Record<UserRole, { bg: string; text: string }> = {
  Owner:      { bg: 'rgba(245,158,11,0.15)', text: 'var(--color-brand-500)' },
  Admin:      { bg: 'rgba(124,58,237,0.15)', text: '#a78bfa' },
  Technician: { bg: 'rgba(59,130,246,0.15)', text: '#60a5fa' },
  Commercial: { bg: 'rgba(16,185,129,0.15)', text: '#34d399' },
}

export default function EquipaPage() {
  const { data: members = [], isLoading } = useTeam()
  const inviteMember = useInviteTeamMember()
  const updateRole = useUpdateTeamMemberRole()
  const removeMember = useRemoveTeamMember()

  const [inviteEmail, setInviteEmail] = useState('')
  const [inviteRole, setInviteRole] = useState<UserRole>('Technician')
  const [inviteError, setInviteError] = useState<string | null>(null)

  const handleInvite = async () => {
    if (!inviteEmail.trim()) return
    setInviteError(null)
    inviteMember.mutate(
      { email: inviteEmail.trim(), role: inviteRole },
      {
        onSuccess: () => {
          setInviteEmail('')
          toast.success('Membro adicionado com sucesso.')
        },
        onError: (err) => setInviteError(getErrorMessage(err)),
      }
    )
  }

  const handleRoleChange = (member: TeamMember, role: UserRole) => {
    updateRole.mutate(
      { id: member.id, role },
      { onError: (err) => toast.error(getErrorMessage(err)) }
    )
  }

  const handleRemove = (member: TeamMember) => {
    if (!confirm(`Remover ${member.fullName || member.email} da equipa?`)) return
    removeMember.mutate(member.id, {
      onSuccess: () => toast.success('Membro removido.'),
      onError: (err) => toast.error(getErrorMessage(err)),
    })
  }

  return (
    <div className="max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold" style={{ color: 'var(--color-ink)' }}>Equipa</h1>
        <p className="text-sm mt-1" style={{ color: 'var(--color-muted)' }}>
          Gere os membros que têm acesso à tua conta.
        </p>
      </div>

      {/* Invite */}
      <div className="rounded-xl border p-6 space-y-4" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
          Adicionar membro
        </h2>

        {inviteError && (
          <p className="text-sm rounded-lg px-4 py-3" style={{ color: '#dc2626', backgroundColor: '#fef2f2' }}>
            {inviteError}
          </p>
        )}

        <div className="flex flex-col sm:flex-row gap-3">
          <input
            type="email"
            value={inviteEmail}
            onChange={e => setInviteEmail(e.target.value)}
            onKeyDown={e => { if (e.key === 'Enter') handleInvite() }}
            placeholder="email@empresa.pt"
            className="flex-1 rounded-lg border px-3 py-2.5 text-sm outline-none transition-all duration-150"
            style={{
              borderColor: 'var(--color-line-strong)',
              backgroundColor: 'var(--color-canvas)',
              color: 'var(--color-ink)',
            }}
          />
          <select
            value={inviteRole}
            onChange={e => setInviteRole(e.target.value as UserRole)}
            className="rounded-lg border px-3 py-2.5 text-sm outline-none transition-all duration-150"
            style={{
              borderColor: 'var(--color-line-strong)',
              backgroundColor: 'var(--color-canvas)',
              color: 'var(--color-ink)',
            }}
          >
            <option value="Technician">Técnico</option>
            <option value="Commercial">Comercial</option>
            <option value="Admin">Administrador</option>
          </select>
          <button
            onClick={handleInvite}
            disabled={inviteMember.isPending || !inviteEmail.trim()}
            className="rounded-lg px-5 py-2.5 text-sm font-semibold transition-all duration-150 disabled:opacity-60 hover:brightness-110"
            style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
          >
            {inviteMember.isPending ? 'A adicionar...' : 'Adicionar'}
          </button>
        </div>

        <p className="text-xs" style={{ color: 'var(--color-subtle)' }}>
          Se o utilizador ainda não tiver conta, será criada automaticamente. Poderá redefinir a password no primeiro acesso.
        </p>
      </div>

      {/* Members list */}
      <div className="rounded-xl border overflow-hidden" style={{ borderColor: 'var(--color-line)' }}>
        <div className="px-6 py-4 border-b" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
          <h2 className="text-xs font-semibold uppercase tracking-wide" style={{ color: 'var(--color-muted)' }}>
            Membros da equipa
            {members.length > 0 && (
              <span className="ml-2 font-mono normal-case" style={{ color: 'var(--color-ink)' }}>
                ({members.length})
              </span>
            )}
          </h2>
        </div>

        {isLoading ? (
          <div className="space-y-3 p-6">
            {[1, 2, 3].map(i => (
              <div key={i} className="h-12 rounded-lg animate-pulse" style={{ backgroundColor: 'var(--color-line)' }} />
            ))}
          </div>
        ) : members.length === 0 ? (
          <div className="px-6 py-12 text-center">
            <p className="text-sm" style={{ color: 'var(--color-muted)' }}>Ainda não adicionaste nenhum membro.</p>
          </div>
        ) : (
          <ul className="divide-y" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
            {members.map((member) => {
              const roleColor = roleColors[member.role] ?? roleColors.Technician
              return (
                <li key={member.id} className="flex items-center gap-4 px-6 py-4">
                  {/* Avatar */}
                  <div
                    className="w-9 h-9 rounded-full flex items-center justify-center text-sm font-bold shrink-0"
                    style={{ backgroundColor: 'var(--color-canvas)', color: 'var(--color-brand-500)' }}
                  >
                    {(member.fullName || member.email).charAt(0).toUpperCase()}
                  </div>

                  {/* Info */}
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2 flex-wrap">
                      <p className="text-sm font-medium truncate" style={{ color: 'var(--color-ink)' }}>
                        {member.fullName || member.email}
                      </p>
                      <span
                        className="rounded px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wider"
                        style={{ backgroundColor: roleColor.bg, color: roleColor.text }}
                      >
                        {roleLabels[member.role]}
                      </span>
                      {!member.isAccepted && (
                        <span className="rounded px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider"
                          style={{ backgroundColor: 'rgba(245,158,11,0.1)', color: 'var(--color-brand-500)' }}>
                          Pendente
                        </span>
                      )}
                    </div>
                    {member.fullName && (
                      <p className="text-xs truncate" style={{ color: 'var(--color-muted)' }}>{member.email}</p>
                    )}
                  </div>

                  {/* Role change */}
                  <select
                    value={member.role}
                    onChange={e => handleRoleChange(member, e.target.value as UserRole)}
                    className="rounded-lg border px-2 py-1.5 text-xs outline-none transition-all duration-150"
                    style={{
                      borderColor: 'var(--color-line-strong)',
                      backgroundColor: 'var(--color-canvas)',
                      color: 'var(--color-ink)',
                    }}
                  >
                    <option value="Technician">Técnico</option>
                    <option value="Commercial">Comercial</option>
                    <option value="Admin">Administrador</option>
                  </select>

                  {/* Remove */}
                  <button
                    onClick={() => handleRemove(member)}
                    disabled={removeMember.isPending}
                    className="rounded-lg border px-3 py-1.5 text-xs font-medium transition-all duration-150 disabled:opacity-60"
                    style={{ borderColor: '#fecaca', color: '#dc2626', backgroundColor: 'var(--color-card)' }}
                    onMouseEnter={e => (e.currentTarget.style.backgroundColor = '#fef2f2')}
                    onMouseLeave={e => (e.currentTarget.style.backgroundColor = 'var(--color-card)')}
                  >
                    Remover
                  </button>
                </li>
              )
            })}
          </ul>
        )}
      </div>
    </div>
  )
}
