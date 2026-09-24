import { API_BASE_URL } from '@/lib/config'

/** Prefix of photos uploaded to this install (served by the API from its uploads volume). */
export const UPLOADED_PHOTO_PREFIX = '/uploads/photos/'

/** Mirrors the backend rule: an uploaded photo path, or an external HTTPS URL. */
export function isValidPhotoUrl(value: string): boolean {
  if (value.startsWith(UPLOADED_PHOTO_PREFIX)) return !value.includes('..')
  try {
    const url = new URL(value)
    return url.protocol === 'https:' || url.hostname === 'localhost'
  } catch {
    return false
  }
}

/** Absolute URL for an <img>: uploaded photos live on the API host. */
export function photoSrc(url: string): string {
  return url.startsWith('/') ? `${API_BASE_URL}${url}` : url
}
