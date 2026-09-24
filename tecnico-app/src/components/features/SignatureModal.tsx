'use client'

import { useEffect, useRef, useState } from 'react'
import SignatureCanvas from 'react-signature-canvas'

interface SignatureModalProps {
  title?: string
  /** Document reference shown under the title (quote number, job title…). */
  subtitle: string
  /** Asks for the signer's name too (job sheets, online acceptance). */
  requireName?: boolean
  defaultName?: string
  /** Legal line shown above the buttons, e.g. what the signer is agreeing to. */
  declaration?: string
  confirmLabel?: string
  onConfirm: (dataUrl: string, signerName: string) => void
  onClose: () => void
  isLoading?: boolean
}

export function SignatureModal({
  title = 'Assinar Orçamento',
  subtitle,
  requireName = false,
  defaultName = '',
  declaration,
  confirmLabel = 'Confirmar assinatura',
  onConfirm,
  onClose,
  isLoading,
}: SignatureModalProps) {
  const padRef = useRef<SignatureCanvas>(null)
  const dialogRef = useRef<HTMLDivElement>(null)
  const closeButtonRef = useRef<HTMLButtonElement>(null)
  const [isEmpty, setIsEmpty] = useState(true)
  const [signerName, setSignerName] = useState(defaultName)
  const canConfirm = !isEmpty && (!requireName || signerName.trim().length > 0)

  // Escape-to-close, Tab focus trap, initial focus, and focus restore on unmount —
  // this is the only true modal dialog in the app, reachable from a normal user flow.
  useEffect(() => {
    const previouslyFocused = document.activeElement as HTMLElement | null
    closeButtonRef.current?.focus()

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose()
        return
      }
      if (e.key !== 'Tab' || !dialogRef.current) return

      const focusable = dialogRef.current.querySelectorAll<HTMLElement>(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])',
      )
      if (focusable.length === 0) return
      const first = focusable[0]
      const last = focusable[focusable.length - 1]

      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault()
        last.focus()
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault()
        first.focus()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('keydown', handleKeyDown)
      previouslyFocused?.focus()
    }
  }, [onClose])

  const handleClear = () => {
    padRef.current?.clear()
    setIsEmpty(true)
  }

  const handleConfirm = () => {
    if (!padRef.current || !canConfirm) return
    const dataUrl = padRef.current.getTrimmedCanvas().toDataURL('image/png')
    onConfirm(dataUrl, signerName.trim())
  }

  return (
    // Backdrop
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
      style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}
      onClick={(e) => { if (e.target === e.currentTarget) onClose() }}
    >
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="signature-modal-title"
        className="rounded-2xl w-full max-w-lg space-y-5"
        style={{ backgroundColor: 'var(--color-card)', padding: '28px' }}
      >
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h2 id="signature-modal-title" className="text-lg font-bold" style={{ color: 'var(--color-ink)' }}>
              {title}
            </h2>
            <p className="text-sm mt-0.5" style={{ color: 'var(--color-muted)' }}>
              {subtitle}
            </p>
          </div>
          <button
            ref={closeButtonRef}
            type="button"
            onClick={onClose}
            aria-label="Fechar"
            className="w-8 h-8 rounded-full flex items-center justify-center transition-colors duration-150"
            style={{ backgroundColor: 'var(--color-canvas)', color: 'var(--color-muted)' }}
          >
            ✕
          </button>
        </div>

        {requireName && (
          <div>
            <label htmlFor="signer-name" className="text-sm font-medium block mb-1" style={{ color: 'var(--color-ink)' }}>
              Nome de quem assina *
            </label>
            <input
              id="signer-name"
              value={signerName}
              onChange={(e) => setSignerName(e.target.value)}
              autoComplete="name"
              maxLength={200}
              className="form-input"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)' }}
            />
          </div>
        )}

        {/* Canvas area */}
        <div
          className="rounded-xl border overflow-hidden"
          style={{ borderColor: 'var(--color-line-strong)', backgroundColor: 'var(--color-neutral-50)' }}
        >
          <SignatureCanvas
            ref={padRef}
            penColor="#1a1a1a"
            canvasProps={{
              width: 560,
              height: 200,
              style: { width: '100%', height: 200, display: 'block' },
            }}
            onBegin={() => setIsEmpty(false)}
          />
        </div>

        <p className="text-xs text-center" style={{ color: 'var(--color-subtle)' }}>
          Assine com o rato ou dedo dentro da área acima
        </p>

        {declaration && (
          <p className="text-xs" style={{ color: 'var(--color-muted)' }}>{declaration}</p>
        )}

        {/* Actions */}
        <div className="flex justify-between items-center">
          <button
            type="button"
            onClick={handleClear}
            className="text-sm font-medium transition-colors duration-150"
            style={{ color: 'var(--color-muted)' }}
            onMouseEnter={(e) => (e.currentTarget.style.color = 'var(--color-ink)')}
            onMouseLeave={(e) => (e.currentTarget.style.color = 'var(--color-muted)')}
          >
            Limpar
          </button>
          <div className="flex gap-3">
            <button
              type="button"
              onClick={onClose}
              className="rounded-lg border px-4 py-2 text-sm font-medium transition-all duration-150"
              style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)' }}
            >
              Cancelar
            </button>
            <button
              type="button"
              onClick={handleConfirm}
              disabled={!canConfirm || isLoading}
              className="rounded-lg px-5 py-2 text-sm font-semibold transition-all duration-150 disabled:opacity-50"
              style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
            >
              {isLoading ? 'A guardar...' : confirmLabel}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
