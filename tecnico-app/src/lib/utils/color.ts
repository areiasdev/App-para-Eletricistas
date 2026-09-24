// Derives a full Tailwind-style shade scale from a single brand color a company picks,
// matching the token set already used throughout the app (--color-brand-50/100/200/400/500/600/700).
// Lightness is normalized per shade rather than preserved from the input, so an unusual
// choice (e.g. a very light or very dark pick) still produces a usable, readable scale.

function hexToHsl(hex: string): [number, number, number] {
  const clean = hex.replace('#', '')
  const r = parseInt(clean.slice(0, 2), 16) / 255
  const g = parseInt(clean.slice(2, 4), 16) / 255
  const b = parseInt(clean.slice(4, 6), 16) / 255

  const max = Math.max(r, g, b)
  const min = Math.min(r, g, b)
  const l = (max + min) / 2

  if (max === min) return [0, 0, l]

  const d = max - min
  const s = l > 0.5 ? d / (2 - max - min) : d / (max + min)

  let h: number
  switch (max) {
    case r: h = ((g - b) / d + (g < b ? 6 : 0)) / 6; break
    case g: h = ((b - r) / d + 2) / 6; break
    default: h = ((r - g) / d + 4) / 6
  }

  return [h, s, l]
}

function hslToHex(h: number, s: number, l: number): string {
  if (s === 0) {
    const v = Math.round(l * 255)
    return rgbToHex(v, v, v)
  }

  const hue2rgb = (p: number, q: number, t: number) => {
    let tt = t
    if (tt < 0) tt += 1
    if (tt > 1) tt -= 1
    if (tt < 1 / 6) return p + (q - p) * 6 * tt
    if (tt < 1 / 2) return q
    if (tt < 2 / 3) return p + (q - p) * (2 / 3 - tt) * 6
    return p
  }

  const q = l < 0.5 ? l * (1 + s) : l + s - l * s
  const p = 2 * l - q
  const r = hue2rgb(p, q, h + 1 / 3)
  const g = hue2rgb(p, q, h)
  const b = hue2rgb(p, q, h - 1 / 3)

  return rgbToHex(Math.round(r * 255), Math.round(g * 255), Math.round(b * 255))
}

function rgbToHex(r: number, g: number, b: number): string {
  return '#' + [r, g, b].map((v) => Math.max(0, Math.min(255, v)).toString(16).padStart(2, '0')).join('')
}

const LIGHTNESS_BY_SHADE: Record<string, number> = {
  '50': 0.96,
  '100': 0.9,
  '200': 0.8,
  '300': 0.7,
  '400': 0.62,
  '500': 0.5,
  '600': 0.4,
  '700': 0.28,
  '800': 0.22,
}

const DARK_TEXT = '#141416'
const LIGHT_TEXT = '#ffffff'

/** WCAG relative luminance of a #rrggbb colour. */
export function relativeLuminance(hex: string): number {
  const channel = (i: number) => {
    const c = parseInt(hex.slice(i, i + 2), 16) / 255
    return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4)
  }
  return 0.2126 * channel(1) + 0.7152 * channel(3) + 0.0722 * channel(5)
}

/** WCAG contrast ratio between two #rrggbb colours (1–21). */
export function contrastRatio(a: string, b: string): number {
  const [hi, lo] = [relativeLuminance(a), relativeLuminance(b)].sort((x, y) => y - x)
  return (hi + 0.05) / (lo + 0.05)
}

/** Text colour (near-black or white) that reads best on the given background. */
export function readableTextOn(backgroundHex: string): string {
  return contrastRatio(backgroundHex, DARK_TEXT) >= contrastRatio(backgroundHex, LIGHT_TEXT) ? DARK_TEXT : LIGHT_TEXT
}

export function generateBrandShades(baseHex: string): Record<string, string> {
  if (!/^#[0-9a-fA-F]{6}$/.test(baseHex)) return {}

  const [h, s] = hexToHsl(baseHex)
  // Slightly boost saturation for very light/dark shades so they don't wash out to grey.
  const shades: Record<string, string> = {}
  for (const [shade, lightness] of Object.entries(LIGHTNESS_BY_SHADE)) {
    shades[shade] = hslToHex(h, Math.min(1, s * 1.05), lightness)
  }

  // Buttons are brand-500 with black or white text. For some colours (mid greys, pure reds)
  // neither reaches 4.5:1 at 50% lightness, so darken the fill a little until white text does.
  let l500 = LIGHTNESS_BY_SHADE['500']
  const readable = (bg: string) => Math.max(contrastRatio(bg, DARK_TEXT), contrastRatio(bg, LIGHT_TEXT)) >= 4.5
  while (!readable(shades['500']) && l500 > 0.2) {
    l500 -= 0.02
    shades['500'] = hslToHex(h, Math.min(1, s * 1.05), l500)
  }
  return shades
}

/**
 * CSS that applies a company's brand colour: the shade scale plus the text colour that stays
 * readable on a brand-filled button, whatever colour was picked (dark text on yellow, white on blue).
 * Dark-mode tints are derived from these in globals.css (html.dark), so only :root is emitted.
 */
export function brandStyleSheet(baseHex: string): string | null {
  const shades = generateBrandShades(baseHex)
  if (Object.keys(shades).length === 0) return null
  const vars = Object.entries(shades).map(([shade, hex]) => `--color-brand-${shade}: ${hex};`)
  vars.push(`--color-on-brand: ${readableTextOn(shades['500'])};`)
  return `:root { ${vars.join(' ')} }`
}
