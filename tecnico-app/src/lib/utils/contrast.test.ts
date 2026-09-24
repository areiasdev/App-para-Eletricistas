import { describe, it, expect } from 'vitest'
import { readFileSync } from 'fs'
import path from 'path'
import { brandStyleSheet, contrastRatio } from './color'

// Checks every text/background pairing the UI uses, in both themes, against WCAG AA
// (4.5:1 body text, 3:1 input borders). Resolves the real values from globals.css —
// including var() references and color-mix() — so a palette edit that breaks contrast fails here.

const css = readFileSync(path.resolve(__dirname, '../../app/globals.css'), 'utf8')

function block(selector: string): Record<string, string> {
  const start = css.indexOf(selector)
  const body = css.slice(css.indexOf('{', start) + 1, css.indexOf('\n}', start))
  const vars: Record<string, string> = {}
  for (const m of body.matchAll(/(--[\w-]+):\s*([^;]+);/g)) vars[m[1]] = m[2].trim()
  return vars
}

const hex = (v: number[]) => '#' + v.map((c) => Math.round(c).toString(16).padStart(2, '0')).join('')
const rgb = (h: string) => [1, 3, 5].map((i) => parseInt(h.slice(i, i + 2), 16))

function resolve(value: string, vars: Record<string, string>): string {
  value = value.trim()
  const ref = value.match(/^var\((--[\w-]+)\)$/)
  if (ref) return resolve(vars[ref[1]], vars)
  const mix = value.match(/^color-mix\(in srgb,\s*(.+?)\s+(\d+)%,\s*(.+)\)$/)
  if (mix) {
    const [a, b, p] = [rgb(resolve(mix[1], vars)), rgb(resolve(mix[3], vars)), Number(mix[2]) / 100]
    return hex(a.map((c, i) => c * p + b[i] * (1 - p)))
  }
  if (/^#[0-9a-f]{6}$/i.test(value)) return value.toLowerCase()
  throw new Error(`Cannot resolve ${value}`)
}

const theme = block('@theme inline')
const light = { ...theme, ...block(':root {') }
const dark = { ...light, ...block('html.dark {') }

const TEXT_PAIRS: [fg: string, bg: string][] = [
  ['ink', 'card'], ['ink', 'canvas'], ['muted', 'card'], ['muted', 'canvas'], ['subtle', 'card'], ['subtle', 'canvas'],
  ['brand-text', 'card'], ['brand-text', 'canvas'], ['on-brand', 'brand-500'],
  ['brand-700', 'brand-50'], ['brand-800', 'brand-100'],
  ['danger-600', 'card'], ['danger-600', 'danger-50'], ['danger-700', 'danger-50'],
  ['success-600', 'card'], ['success-700', 'success-50'], ['success-700', 'success-100'],
  ['info-600', 'card'], ['info-700', 'info-50'],
  ['warning-700', 'warning-50'],
  ['neutral-600', 'neutral-100'],
  ['role-purple-text', 'role-purple-bg'], ['role-rose-text', 'role-rose-bg'],
  ['sidebar-text', 'sidebar'], ['sidebar-muted', 'sidebar'], ['sidebar-accent', 'sidebar'], ['sidebar-text', 'sidebar-active'],
]
const WHITE_ON = ['danger-solid', 'success-solid', 'info-solid', 'role-purple-solid']

describe.each([['light', light], ['dark', dark]] as const)('%s theme contrast', (_, vars) => {
  const c = (name: string) => resolve(`var(--color-${name})`, vars)

  it.each(TEXT_PAIRS)('%s on %s ≥ 4.5:1', (fg, bg) => {
    expect(contrastRatio(c(fg), c(bg))).toBeGreaterThanOrEqual(4.5)
  })

  it.each(WHITE_ON)('white on %s ≥ 4.5:1', (bg) => {
    expect(contrastRatio('#ffffff', c(bg))).toBeGreaterThanOrEqual(4.5)
  })

  it('input borders are visible (≥ 3:1)', () => {
    expect(contrastRatio(c('line-strong'), c('card'))).toBeGreaterThanOrEqual(3)
  })
})

describe('company brand colours stay readable', () => {
  // A spread of colours a company might pick, from pale yellow to near-black.
  it.each(['#2563eb', '#16a34a', '#dc2626', '#facc15', '#7c3aed', '#0f172a', '#f97316', '#06b6d4', '#e5e7eb'])('%s', (brand) => {
    const custom = Object.fromEntries(
      [...brandStyleSheet(brand)!.matchAll(/(--[\w-]+):\s*([^;]+);/g)].map((m) => [m[1], m[2]]),
    )
    for (const [name, vars] of [['light', { ...light, ...custom }], ['dark', { ...dark, ...custom, ...block('html.dark {') }]] as const) {
      const c = (n: string) => resolve(`var(--color-${n})`, vars)
      expect(contrastRatio(c('on-brand'), c('brand-500')), `${name} on-brand`).toBeGreaterThanOrEqual(4.5)
      expect(contrastRatio(c('brand-text'), c('card')), `${name} brand-text`).toBeGreaterThanOrEqual(4.5)
      expect(contrastRatio(c('sidebar-accent'), c('sidebar')), `${name} sidebar-accent`).toBeGreaterThanOrEqual(4.5)
    }
  })
})
