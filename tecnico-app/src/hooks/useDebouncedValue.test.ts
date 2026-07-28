import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { useDebouncedValue } from './useDebouncedValue'

describe('useDebouncedValue', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  it('returns the initial value immediately', () => {
    const { result } = renderHook(() => useDebouncedValue('a', 300))
    expect(result.current).toBe('a')
  })

  it('does not update before the delay elapses', () => {
    const { result, rerender } = renderHook(({ value }) => useDebouncedValue(value, 300), {
      initialProps: { value: 'a' },
    })

    rerender({ value: 'b' })
    act(() => vi.advanceTimersByTime(299))

    expect(result.current).toBe('a')
  })

  it('updates once the delay elapses', () => {
    const { result, rerender } = renderHook(({ value }) => useDebouncedValue(value, 300), {
      initialProps: { value: 'a' },
    })

    rerender({ value: 'b' })
    act(() => vi.advanceTimersByTime(300))

    expect(result.current).toBe('b')
  })

  it('resets the timer on every intermediate change, only settling on the last value', () => {
    // Mirrors the real bug: typing "abc" fast should only ever commit "abc", not
    // fire a stale update for "a" or "ab" after they've already been superseded.
    const { result, rerender } = renderHook(({ value }) => useDebouncedValue(value, 300), {
      initialProps: { value: '' },
    })

    rerender({ value: 'a' })
    act(() => vi.advanceTimersByTime(100))
    rerender({ value: 'ab' })
    act(() => vi.advanceTimersByTime(100))
    rerender({ value: 'abc' })
    act(() => vi.advanceTimersByTime(299))
    expect(result.current).toBe('')

    act(() => vi.advanceTimersByTime(1))
    expect(result.current).toBe('abc')
  })
})
