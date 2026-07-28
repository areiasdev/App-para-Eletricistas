'use client'

import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useClients } from '@/hooks/useClients'

const equipmentSchema = z.object({
  clientId: z.string().min(1, 'Seleciona um cliente.'),
  type: z.string().min(1, 'O tipo é obrigatório.').max(100),
  brand: z.string().max(100).optional().or(z.literal('')),
  model: z.string().max(100).optional().or(z.literal('')),
  serialNumber: z.string().max(100).optional().or(z.literal('')),
  installedAt: z.string().optional(),
  nextMaintenance: z.string().optional(),
  notes: z.string().optional(),
  photos: z.array(z.string().url('URL inválido')).optional(),
})

export type EquipmentFormValues = z.infer<typeof equipmentSchema>

interface EquipmentFormProps {
  defaultValues?: Partial<EquipmentFormValues>
  onSubmit: (values: EquipmentFormValues) => void
  isLoading?: boolean
  submitLabel?: string
  lockClient?: boolean
}

export function EquipmentForm({
  defaultValues,
  onSubmit,
  isLoading,
  submitLabel = 'Guardar',
  lockClient = false,
}: EquipmentFormProps) {
  const { data: clientsData } = useClients({ pageSize: 200 })

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<EquipmentFormValues>({
    resolver: zodResolver(equipmentSchema),
    defaultValues: { photos: [], ...defaultValues },
  })

  const photos = watch('photos') ?? []
  const [photoInput, setPhotoInput] = useState('')

  const addPhoto = () => {
    const url = photoInput.trim()
    if (!url) return
    setValue('photos', [...photos, url])
    setPhotoInput('')
  }

  const removePhoto = (i: number) => {
    setValue('photos', photos.filter((_, idx) => idx !== i))
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">

      {/* ── Identificação ── */}
      <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <div className="px-5 py-3.5 border-b" style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}>
          <h2 className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-muted)' }}>
            Identificação
          </h2>
        </div>

        <div className="p-5 space-y-4">
          {!lockClient && (
            <FormField label="Cliente *" error={errors.clientId?.message}>
              <select
                {...register('clientId')}
                className="form-input"
                style={{ borderColor: errors.clientId ? '#fca5a5' : 'var(--color-line-strong)' }}
              >
                <option value="">Selecionar cliente...</option>
                {clientsData?.items.map(c => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </select>
            </FormField>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField label="Tipo *" error={errors.type?.message}>
              <input
                {...register('type')}
                placeholder="Ex: Ar condicionado, Caldeira, Elevador"
                className="form-input"
                style={{ borderColor: errors.type ? '#fca5a5' : 'var(--color-line-strong)' }}
              />
            </FormField>
            <FormField label="Marca" error={errors.brand?.message}>
              <input
                {...register('brand')}
                placeholder="Ex: Daikin, Bosch"
                className="form-input"
                style={{ borderColor: 'var(--color-line-strong)' }}
              />
            </FormField>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField label="Modelo" error={errors.model?.message}>
              <input
                {...register('model')}
                placeholder="Ex: FTX35K"
                className="form-input"
                style={{ borderColor: 'var(--color-line-strong)' }}
              />
            </FormField>
            <FormField label="Número de série" error={errors.serialNumber?.message}>
              <input
                {...register('serialNumber')}
                placeholder="Ex: SN-12345678"
                className="form-input"
                style={{ borderColor: 'var(--color-line-strong)' }}
              />
            </FormField>
          </div>
        </div>
      </section>

      {/* ── Manutenção ── */}
      <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <div className="px-5 py-3.5 border-b" style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}>
          <h2 className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-muted)' }}>
            Manutenção
          </h2>
        </div>

        <div className="p-5 space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField label="Data de instalação" error={errors.installedAt?.message}>
              <input
                type="date"
                {...register('installedAt')}
                className="form-input"
                style={{ borderColor: 'var(--color-line-strong)' }}
              />
            </FormField>
            <FormField label="Próxima manutenção" error={errors.nextMaintenance?.message}>
              <input
                type="date"
                {...register('nextMaintenance')}
                className="form-input"
                style={{ borderColor: 'var(--color-line-strong)' }}
              />
              <p className="text-xs mt-1" style={{ color: 'var(--color-subtle)' }}>
                Receberás um alerta 7 dias antes.
              </p>
            </FormField>
          </div>

          <FormField label="Notas" error={errors.notes?.message}>
            <textarea
              {...register('notes')}
              rows={3}
              placeholder="Observações, histórico de avarias, etc."
              className="form-input resize-none"
              style={{ borderColor: 'var(--color-line-strong)' }}
            />
          </FormField>
        </div>
      </section>

      {/* ── Fotos ── */}
      <section className="rounded-xl border overflow-hidden" style={{ backgroundColor: 'var(--color-card)', borderColor: 'var(--color-line)' }}>
        <div className="px-5 py-3.5 border-b" style={{ backgroundColor: 'var(--color-canvas)', borderColor: 'var(--color-line)' }}>
          <h2 className="text-xs font-bold uppercase tracking-widest" style={{ color: 'var(--color-muted)' }}>
            Fotos
          </h2>
        </div>
        <div className="p-5 space-y-3">
          <p className="text-xs" style={{ color: 'var(--color-subtle)' }}>
            Adiciona URLs de fotos do equipamento (ex: Google Drive, Dropbox, Imgur).
          </p>
          <div className="flex gap-2">
            <input
              type="url"
              value={photoInput}
              onChange={e => setPhotoInput(e.target.value)}
              onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); addPhoto() } }}
              placeholder="https://exemplo.com/foto.jpg"
              className="form-input flex-1"
              style={{ borderColor: 'var(--color-line-strong)' }}
            />
            <button
              type="button"
              onClick={addPhoto}
              className="rounded-lg px-4 py-2 text-sm font-medium transition-all duration-150"
              style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
            >
              Adicionar
            </button>
          </div>
          {photos.length > 0 && (
            <ul className="space-y-2">
              {photos.map((url, i) => (
                <li key={i} className="flex items-center gap-2 rounded-lg border px-3 py-2"
                  style={{ borderColor: 'var(--color-line)', backgroundColor: 'var(--color-canvas)' }}>
                  <span className="text-xs truncate flex-1" style={{ color: 'var(--color-ink)' }}>{url}</span>
                  <button
                    type="button"
                    onClick={() => removePhoto(i)}
                    className="shrink-0 text-xs px-2 py-0.5 rounded transition-colors duration-150"
                    style={{ color: '#dc2626' }}
                  >
                    Remover
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>

      <div className="flex justify-end">
        <button
          type="submit"
          disabled={isLoading}
          className="rounded-lg px-6 py-2.5 text-sm font-semibold transition-all duration-150 disabled:opacity-60 hover:brightness-110 active:scale-[0.99]"
          style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
        >
          {isLoading ? 'A guardar...' : submitLabel}
        </button>
      </div>

      <style>{`
        .form-input {
          width: 100%;
          border-radius: 0.5rem;
          border: 1.5px solid var(--color-line-strong);
          padding: 0.5rem 0.75rem;
          font-size: 0.875rem;
          background-color: var(--color-card);
          color: var(--color-ink);
          outline: none;
          transition: border-color 0.15s, box-shadow 0.15s;
          font-family: var(--font-outfit), system-ui, sans-serif;
        }
        .form-input:focus {
          border-color: var(--color-brand-500);
          box-shadow: 0 0 0 3px rgba(245,158,11,0.12);
        }
        .form-input::placeholder {
          color: var(--color-subtle);
        }
      `}</style>
    </form>
  )
}

function FormField({ label, id, error, children }: { label: string; id?: string; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label htmlFor={id} className="block text-xs font-semibold uppercase tracking-wide mb-1.5" style={{ color: 'var(--color-muted)' }}>
        {label}
      </label>
      {children}
      {error && <p className="mt-1 text-xs" style={{ color: '#dc2626' }}>{error}</p>}
    </div>
  )
}
