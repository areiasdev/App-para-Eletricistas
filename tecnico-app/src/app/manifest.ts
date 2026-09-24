import type { MetadataRoute } from 'next'
import { APP_NAME } from '@/lib/config'

// Installable on the phone home screen ("Adicionar ao ecrã principal") — opens full-screen
// like a native app, which is how technicians use it on site.
export default function manifest(): MetadataRoute.Manifest {
  return {
    name: APP_NAME,
    short_name: APP_NAME,
    description: 'Orçamentos, intervenções, folhas de obra e faturas para eletricistas e técnicos.',
    lang: 'pt-PT',
    start_url: '/dashboard',
    scope: '/',
    display: 'standalone',
    orientation: 'portrait',
    background_color: '#17171a',
    theme_color: '#17171a',
    icons: [
      { src: '/icon-192.png', sizes: '192x192', type: 'image/png' },
      { src: '/icon-512.png', sizes: '512x512', type: 'image/png' },
      { src: '/icon-maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
    ],
    shortcuts: [
      { name: 'Agenda', url: '/dashboard/agenda' },
      { name: 'Nova intervenção', url: '/dashboard/intervencoes/novo' },
      { name: 'Novo orçamento', url: '/dashboard/orcamentos/novo' },
    ],
  }
}
