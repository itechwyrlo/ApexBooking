import { test, expect } from '@playwright/test'

// Step 3 (status badge consolidation) added no new CSS — every badge tone it touches
// (badge-tone-success/warning/danger/neutral/primary/teal) already existed and was verified in
// Step 1's suite. What actually changed is which React component renders which tone for which
// status, and that only renders on protected pages this repo has no way to reach without a real
// session (see the Step 3 report's "Any issues requiring a decision" section — same limitation as
// Steps 1 and 2). This spec is a narrow regression guard confirming the six tones every new/edited
// badge component (BookingStatusBadge, RefundRequestStatusBadge, ActiveStatusBadge, etc.) can be
// given still resolve to six visually distinct colors through the real stylesheet.

test.describe('Step 3 status badge tone system (unauthenticated-reachable checks)', () => {
  test('all six badge-tone-* classes remain visually distinct', async ({ page }) => {
    await page.goto('/find-workspace')
    const colors = await page.evaluate(() => {
      const tones = ['neutral', 'primary', 'success', 'warning', 'danger', 'teal']
      const wrap = document.createElement('div')
      wrap.innerHTML = tones.map((tone) => `<span class="badge rounded-pill fw-medium badge-tone-${tone}" data-tone="${tone}">x</span>`).join('')
      document.body.appendChild(wrap)
      const result = tones.map((tone) => {
        const el = wrap.querySelector(`[data-tone="${tone}"]`) as HTMLElement
        const style = getComputedStyle(el)
        return { tone, bg: style.backgroundColor, color: style.color, radius: style.borderRadius }
      })
      wrap.remove()
      return result
    })

    const backgrounds = colors.map((c) => c.bg)
    expect(new Set(backgrounds).size).toBe(colors.length) // every tone's background is unique
    for (const c of colors) {
      expect(c.radius, `${c.tone} should stay pill-shaped`).not.toBe('0px')
    }
  })
})
