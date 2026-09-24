import { api } from './client'

/** Longest side of an uploaded photo — plenty for documenting work, ~10x smaller than a raw phone shot. */
const MAX_PHOTO_DIMENSION = 1600
const JPEG_QUALITY = 0.82

/**
 * Downscales a phone photo in the browser before upload. Saves mobile data on site and keeps
 * job-sheet PDFs small. Falls back to the original file if the browser can't decode it.
 */
export async function compressImage(file: File): Promise<Blob> {
  if (!file.type.startsWith('image/')) return file
  try {
    const bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' })
    const scale = Math.min(1, MAX_PHOTO_DIMENSION / Math.max(bitmap.width, bitmap.height))
    const canvas = document.createElement('canvas')
    canvas.width = Math.round(bitmap.width * scale)
    canvas.height = Math.round(bitmap.height * scale)
    canvas.getContext('2d')?.drawImage(bitmap, 0, 0, canvas.width, canvas.height)
    bitmap.close()
    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', JPEG_QUALITY))
    return blob && blob.size < file.size ? blob : file
  } catch {
    return file
  }
}

export const uploadsApi = {
  /** Returns the stored photo's URL path ("/uploads/photos/…"). */
  uploadPhoto: async (file: File) => {
    const body = await compressImage(file)
    const formData = new FormData()
    const name = body === file ? file.name : file.name.replace(/\.\w+$/, '') + '.jpg'
    formData.append('file', body, name)
    return api
      .post<{ url: string }>('/uploads/photos', formData, { headers: { 'Content-Type': undefined } })
      .then((r) => r.data.url)
  },
}
