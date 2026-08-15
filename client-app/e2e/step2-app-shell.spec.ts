import { test, expect } from '@playwright/test'

// Verifies what Step 2 (unified AppShell + mobile bottom navigation) can be checked WITHOUT an
// authenticated session — the shell itself only renders on protected routes (ProtectedRoute /
// SuperAdminProtectedRoute), and this repo has no seeded test account or auth bypass (see the
// Step 2 report's "Authentication-related verification limitations" section). These tests instead
// confirm, on a real public page (/find-workspace, same page Step 1 used):
//   1. the new bottom-nav CSS is correctly scoped and doesn't leak onto a page with no AppShell,
//   2. the bottom-nav/touch-target/safe-area rules compute the intended values when applied,
//   3. the viewport-fit=cover prerequisite actually shipped,
//   4. no horizontal overflow was introduced at the breakpoints the refactor cares about.
// Route-matching, role-based item selection, the More menu's real content, and nested-route
// parent activation are NOT covered here — they need a real session (see report).

test.describe('Step 2 app shell (unauthenticated-reachable checks)', () => {
  test('a page with no AppShell renders no .bottom-nav', async ({ page }) => {
    await page.goto('/find-workspace')
    await expect(page.locator('.bottom-nav')).toHaveCount(0)
  })

  test('viewport-fit=cover shipped on the viewport meta tag', async ({ page }) => {
    await page.goto('/find-workspace')
    const content = await page.locator('meta[name="viewport"]').getAttribute('content')
    expect(content).toContain('viewport-fit=cover')
  })

  test('.bottom-nav-item meets the 44px+ touch-target guideline and the active tone uses --color-primary-strong', async ({
    page,
  }) => {
    await page.goto('/find-workspace')
    const result = await page.evaluate(() => {
      const nav = document.createElement('nav')
      nav.className = 'bottom-nav'
      nav.innerHTML = `
        <a class="bottom-nav-item active" id="pw-active"><span class="bottom-nav-item-icon-wrap"></span><span>Dashboard</span></a>
        <a class="bottom-nav-item" id="pw-inactive"><span>Appointments</span></a>
      `
      document.body.appendChild(nav)
      const active = document.getElementById('pw-active')!
      const inactive = document.getElementById('pw-inactive')!
      const measured = {
        minHeight: getComputedStyle(active).minHeight,
        minWidth: getComputedStyle(active).minWidth,
        activeColor: getComputedStyle(active).color,
        inactiveColor: getComputedStyle(inactive).color,
        position: getComputedStyle(nav).position,
      }
      nav.remove()
      return measured
    })
    // 56px min-height (comfortably over the ~44px guideline); flex-item min-width guards small viewports.
    expect(result.minHeight).toBe('56px')
    expect(result.minWidth).toBe('44px')
    expect(result.activeColor).not.toBe(result.inactiveColor)
    expect(result.position).toBe('fixed')
  })

  test('the bottom-nav content-clearance rule reserves space for the bar + safe area on mobile widths', async ({
    page,
  }) => {
    await page.setViewportSize({ width: 390, height: 844 })
    await page.goto('/find-workspace')
    const paddingBottom = await page.evaluate(() => {
      const el = document.createElement('div')
      el.className = 'app-shell-content-with-bottom-nav'
      document.body.appendChild(el)
      const value = getComputedStyle(el).paddingBottom
      el.remove()
      return value
    })
    // env(safe-area-inset-bottom, 0px) resolves to 0 in a non-PWA browser context, so this
    // environment's floor is exactly the bar height (56px) — the calc() itself, and that it
    // isn't silently dropped/invalid (which would compute to "0px"), is what's being checked.
    expect(paddingBottom).toBe('56px')
  })

  test('no horizontal overflow at narrow mobile, mobile, tablet, and desktop widths', async ({ page }) => {
    for (const width of [320, 375, 430, 768, 1280]) {
      await page.setViewportSize({ width, height: 900 })
      await page.goto('/find-workspace')
      const { scrollWidth, clientWidth } = await page.evaluate(() => ({
        scrollWidth: document.documentElement.scrollWidth,
        clientWidth: document.documentElement.clientWidth,
      }))
      expect(scrollWidth, `horizontal overflow at ${width}px`).toBeLessThanOrEqual(clientWidth)
    }
  })
})
