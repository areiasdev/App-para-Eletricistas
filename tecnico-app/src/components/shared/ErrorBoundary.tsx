'use client'

import { Component, type ErrorInfo, type ReactNode } from 'react'

interface Props {
  children: ReactNode
  fallback?: ReactNode
}

interface State {
  hasError: boolean
  error: Error | null
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props)
    this.state = { hasError: false, error: null }
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('[ErrorBoundary]', error, info.componentStack)
  }

  handleReset = () => {
    this.setState({ hasError: false, error: null })
  }

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) return this.props.fallback

      return (
        <div
          className="flex flex-col items-center justify-center py-20 text-center gap-4"
          role="alert"
        >
          <div
            className="w-12 h-12 rounded-full flex items-center justify-center"
            style={{ backgroundColor: '#fef2f2' }}
          >
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path
                d="M12 9v4m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"
                stroke="#dc2626"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
          </div>
          <div>
            <p className="text-sm font-semibold" style={{ color: 'var(--color-ink)' }}>
              Algo correu mal
            </p>
            <p className="text-xs mt-1" style={{ color: 'var(--color-muted)' }}>
              Ocorreu um erro inesperado. Tenta novamente ou recarrega a página.
            </p>
          </div>
          <button
            onClick={this.handleReset}
            className="rounded-lg px-4 py-2 text-sm font-medium transition-all duration-150 border"
            style={{
              borderColor: 'var(--color-line-strong)',
              color: 'var(--color-ink)',
              backgroundColor: 'var(--color-card)',
            }}
          >
            Tentar novamente
          </button>
        </div>
      )
    }

    return this.props.children
  }
}
