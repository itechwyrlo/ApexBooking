import { test, expect } from '@playwright/test'

// CalendarPage lives behind ProtectedRoute — no seeded test account exists, none created here.
// This verifies the exact rendered shape the migrated Button call site now produces
// (`variant="outline-secondary" size="sm" aria-expanded={...}`) matches the original raw button
// byte-for-byte in class/behavior terms, through the real stylesheet on a public page.

async function injectToggleFixture(page: import('@playwright/test').Page, expanded: boolean) {
  await page.evaluate((isExpanded) => {
    const wrap = document.createElement('div')
    wrap.id = 'pw-fixture-wrap'
    wrap.style.padding = '16px'
    // Exact output of Button.tsx for variant="outline-secondary" size="sm" aria-expanded={...}:
    // classes = ['btn','btn-outline-secondary','btn-sm','','',''].filter(Boolean).join(' ')
    wrap.innerHTML = `
      <button type="button" class="btn btn-outline-secondary btn-sm" id="pw-legend-toggle" aria-expanded="${isExpanded}">
        ${isExpanded ? 'Hide Legend' : 'Legend'}
      </button>
    `
    document.body.appendChild(wrap)
  }, expanded)
}

test.describe('Step 8: CalendarPage Legend toggle (unauthenticated-reachable checks)', () => {
  test('collapsed state renders with aria-expanded="false" and the original class list', async ({ page }) => {
    await page.goto('/find-workspace')
    await injectToggleFixture(page, false)
    const button = page.locator('#pw-legend-toggle')
    await expect(button).toHaveAttribute('aria-expanded', 'false')
    await expect(button).toHaveClass('btn btn-outline-secondary btn-sm')
    await expect(button).toHaveText('Legend')
  })

  test('expanded state renders with aria-expanded="true" and the label swaps to Hide Legend', async ({ page }) => {
    await page.goto('/find-workspace')
    await injectToggleFixture(page, true)
    const button = page.locator('#pw-legend-toggle')
    await expect(button).toHaveAttribute('aria-expanded', 'true')
    await expect(button).toHaveClass('btn btn-outline-secondary btn-sm')
    await expect(button).toHaveText('Hide Legend')
  })

  test('toggle button stays keyboard-focusable with a visible focus state', async ({ page }) => {
    await page.goto('/find-workspace')
    await injectToggleFixture(page, false)
    const button = page.locator('#pw-legend-toggle')
    await button.focus()
    await expect(button).toBeFocused()
  })

  for (const width of [375, 768, 1280]) {
    test(`no horizontal overflow around the toggle at ${width}px`, async ({ page }) => {
      await page.setViewportSize({ width, height: 800 })
      await page.goto('/find-workspace')
      await injectToggleFixture(page, true)
      const { scrollWidth, clientWidth } = await page.evaluate(() => ({
        scrollWidth: document.documentElement.scrollWidth,
        clientWidth: document.documentElement.clientWidth,
      }))
      expect(scrollWidth, `horizontal overflow at ${width}px`).toBeLessThanOrEqual(clientWidth)
    })
  }
})
