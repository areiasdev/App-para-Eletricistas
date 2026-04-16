'use client'

import { useEffect } from 'react'
import Link from 'next/link'

interface UpgradeModalProps {
  message: string
  onClose: () => void
}

export function UpgradeModal({ message, onClose }: UpgradeModalProps) {
  useEffect(() => {
    const handleKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', handleKey)
    return () => window.removeEventListener('keydown', handleKey)
  }, [onClose])

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
      style={{ backgroundColor: 'rgba(0,0,0,0.45)' }}
      onClick={onClose}
    >
      <div
        className="relative w-full max-w-sm rounded-2xl p-7 shadow-2xl"
        style={{ backgroundColor: 'var(--color-card)' }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Close */}
        <button
          onClick={onClose}
          className="absolute top-4 right-4 w-7 h-7 rounded-full flex items-center justify-center transition-colors duration-150"
          style={{ color: 'var(--color-muted)', backgroundColor: 'var(--color-canvas)' }}
        >
          <svg width="12" height="12" viewBox="0 0 12 12" fill="none">
            <path d="M1 1l10 10M11 1L1 11" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
          </svg>
        </button>

        {/* Icon */}
        <div
          className="w-11 h-11 rounded-xl flex items-center justify-center mb-4"
          style={{ backgroundColor: 'rgba(245,158,11,0.12)' }}
        >
          <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
            <path d="M11 2l2.4 4.8 5.6 1.2-4 3.9.9 5.5L11 14.6l-4.9 2.8.9-5.5L3 8l5.6-1.2L11 2z"
              stroke="var(--color-brand-500)" strokeWidth="1.5" strokeLinejoin="round"/>
          </svg>
        </div>

        <p className="text-base font-bold mb-2" style={{ color: 'var(--color-ink)' }}>
          Upgrade necessário
        </p>
        <p className="text-sm mb-6" style={{ color: 'var(--color-muted)', lineHeight: '1.6' }}>
          {message}
        </p>

        <div className="flex gap-3">
          <button
            onClick={onClose}
            className="flex-1 rounded-lg border px-4 py-2.5 text-sm font-medium transition-all duration-150"
            style={{ borderColor: 'var(--color-line-strong)', color: 'var(--color-ink)', backgroundColor: 'var(--color-card)' }}
          >
            Agora não
          </button>
          <Link
            href="/dashboard/planos"
            onClick={onClose}
            className="flex-1 rounded-lg px-4 py-2.5 text-sm font-semibold text-center transition-all duration-150 hover:brightness-110"
            style={{ backgroundColor: 'var(--color-brand-500)', color: 'var(--color-sidebar)' }}
          >
            Ver planos →
          </Link>
        </div>
      </div>
    </div>
  )
}
