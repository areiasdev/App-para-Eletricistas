// Build-time configuration. NEXT_PUBLIC_* values are inlined into the bundle by Next.js, so
// read them here once instead of repeating process.env lookups (and fallbacks) per file.

/** Public URL of the API. Local default matches the backend's launchSettings (port 5092). */
export const API_BASE_URL = (process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5092').replace(/\/+$/, '')

/** Product name on login/onboarding screens and the browser tab (not client-facing documents). */
export const APP_NAME = process.env.NEXT_PUBLIC_APP_NAME || 'TécnicoApp'

/** Single-letter mark shown in place of a logo. */
export const APP_INITIAL = APP_NAME.charAt(0).toUpperCase()
