'use client'

import { useRef, useState } from 'react'
import { toast } from 'sonner'
import { uploadsApi } from '@/lib/api/uploads'
import { getErrorMessage } from '@/lib/api/client'
import { isValidPhotoUrl, photoSrc } from '@/lib/photos'

interface PhotoUploaderProps {
  value: string[]
  onChange: (urls: string[]) => void
  max?: number
}

/**
 * Take a photo with the phone camera (or pick from the gallery) and upload it straight away.
 * Photos are downsized in the browser first, so this works on site with a weak mobile signal.
 * Pasting an external link is still possible as a fallback.
 */
export function PhotoUploader({ value, onChange, max = 20 }: PhotoUploaderProps) {
  const cameraRef = useRef<HTMLInputElement>(null)
  const galleryRef = useRef<HTMLInputElement>(null)
  const [uploading, setUploading] = useState(0)
  const [linkInput, setLinkInput] = useState('')
  const remaining = max - value.length

  const upload = async (files: FileList | null) => {
    if (!files?.length) return
    const selected = Array.from(files).slice(0, Math.max(0, remaining))
    if (selected.length < files.length) toast.warning(`Máximo de ${max} fotos.`)

    setUploading((n) => n + selected.length)
    const uploaded: string[] = []
    for (const file of selected) {
      try {
        uploaded.push(await uploadsApi.uploadPhoto(file))
      } catch (err) {
        toast.error(`${file.name}: ${getErrorMessage(err)}`)
      } finally {
        setUploading((n) => n - 1)
      }
    }
    if (uploaded.length) onChange([...value, ...uploaded])
  }

  const addLink = () => {
    const url = linkInput.trim()
    if (!url) return
    if (!isValidPhotoUrl(url)) return toast.error('O link deve começar por https://')
    if (value.includes(url)) return toast.error('Essa foto já foi adicionada.')
    onChange([...value, url])
    setLinkInput('')
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          onClick={() => cameraRef.current?.click()}
          disabled={remaining <= 0}
          className="rounded-lg px-4 py-2.5 text-sm font-semibold inline-flex items-center gap-2 disabled:opacity-50"
          style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
        >
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden>
            <path d="M2 5.5A1.5 1.5 0 0 1 3.5 4h1.3l1-1.5h4.4l1 1.5h1.3A1.5 1.5 0 0 1 14 5.5v6a1.5 1.5 0 0 1-1.5 1.5h-9A1.5 1.5 0 0 1 2 11.5v-6Z" stroke="currentColor" strokeWidth="1.3"/>
            <circle cx="8" cy="8.3" r="2.3" stroke="currentColor" strokeWidth="1.3"/>
          </svg>
          Tirar foto
        </button>
        <button
          type="button"
          onClick={() => galleryRef.current?.click()}
          disabled={remaining <= 0}
          className="rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
          style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)' }}
        >
          Escolher da galeria
        </button>
        {uploading > 0 && (
          <span className="self-center text-sm" style={{ color: 'var(--color-muted)' }}>A carregar {uploading}…</span>
        )}
      </div>

      <input ref={cameraRef} type="file" accept="image/*" capture="environment" hidden
        onChange={(e) => { upload(e.target.files); e.target.value = '' }} />
      <input ref={galleryRef} type="file" accept="image/png,image/jpeg,image/webp" multiple hidden
        onChange={(e) => { upload(e.target.files); e.target.value = '' }} />

      {value.length > 0 && (
        <ul className="grid grid-cols-3 sm:grid-cols-4 gap-2">
          {value.map((url) => (
            <li key={url} className="relative aspect-square rounded-lg overflow-hidden border" style={{ borderColor: 'var(--color-line)' }}>
              <a href={photoSrc(url)} target="_blank" rel="noopener noreferrer">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={photoSrc(url)} alt="" loading="lazy" className="w-full h-full object-cover" />
              </a>
              <button
                type="button"
                onClick={() => onChange(value.filter((u) => u !== url))}
                aria-label="Remover foto"
                className="absolute top-1 right-1 w-7 h-7 rounded-full text-sm font-bold flex items-center justify-center"
                style={{ backgroundColor: 'rgba(0,0,0,0.6)', color: 'white' }}
              >
                ×
              </button>
            </li>
          ))}
        </ul>
      )}

      <details className="text-xs" style={{ color: 'var(--color-subtle)' }}>
        <summary className="cursor-pointer">Adicionar por link</summary>
        <div className="flex gap-2 mt-2">
          <input
            type="url"
            value={linkInput}
            onChange={(e) => setLinkInput(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addLink() } }}
            placeholder="https://…"
            className="form-input flex-1"
            style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)' }}
          />
          <button type="button" onClick={addLink} className="rounded-lg border px-3 text-sm"
            style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)' }}>
            Adicionar
          </button>
        </div>
      </details>
    </div>
  )
}
