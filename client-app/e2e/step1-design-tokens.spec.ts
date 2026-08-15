import { test, expect } from '@playwright/test'

// Verifies the Step 1 design-token changes documented in
// docs/superpowers/plans/2026-08-14-step1-design-system-adjustments.md.
//
// Runs against /find-workspace — a public route (AuthLayout, no auth required) — so every check
// exercises the real, fully-loaded bootstrap.min.css + theme.css cascade rather than an isolated
// fixture page. TrendPill/.progress-thin have no call site in the app by design (Step 1 ships them
// unwired), so those two checks inject their markup into the live page via the browser at test
// time and remove it immediately after — nothing is added to the app's source for this.

test.describe('Step 1 design tokens', () => {
  test('a real .card surface renders at the new 20px radius', async ({ page }) => {
    await page.goto('/find-workspace')
    const card = page.locator('.card').first()
    await expect(card).toBeVisible()
    const radius = await card.evaluate((el) => getComputedStyle(el).borderTopLeftRadius)
    expect(radius).toBe('20px')
  })

  test('.modal-content keeps its original 16px radius (not repointed to --radius-xl)', async ({ page }) => {
    await page.goto('/find-workspace')
    const radius = await page.evaluate(() => {
      const el = document.createElement('div')
      el.className = 'modal-content'
      document.body.appendChild(el)
      const value = getComputedStyle(el).borderTopLeftRadius
      el.remove()
      return value
    })
    expect(radius).toBe('16px')
  })

  test('form-control meets the 40px minimum height; form-control-sm is unaffected', async ({ page }) => {
    await page.goto('/find-workspace')
    const heights = await page.evaluate(() => {
      const normal = document.createElement('input')
      normal.className = 'form-control'
      const small = document.createElement('input')
      small.className = 'form-control form-control-sm'
      document.body.append(normal, small)
      const result = {
        normal: getComputedStyle(normal).minHeight,
        small: getComputedStyle(small).minHeight,
      }
      normal.remove()
      small.remove()
      return result
    })
    expect(heights.normal).toBe('40px')
    expect(heights.small).not.toBe('40px')
  })

  test('dark mode repaints the canvas/surface tokens', async ({ page }) => {
    await page.goto('/find-workspace')
    const light = await page.evaluate(() => getComputedStyle(document.body).backgroundColor)
    await page.evaluate(() => document.documentElement.setAttribute('data-bs-theme', 'dark'))
    const dark = await page.evaluate(() => getComputedStyle(document.body).backgroundColor)
    expect(dark).not.toBe(light)
  })

  test('the teal tenant palette overrides --color-primary', async ({ page }) => {
    await page.goto('/find-workspace')
    const indigo = await page.evaluate(() =>
      getComputedStyle(document.documentElement).getPropertyValue('--color-primary').trim(),
    )
    await page.evaluate(() => document.documentElement.setAttribute('data-palette', 'teal'))
    const teal = await page.evaluate(() =>
      getComputedStyle(document.documentElement).getPropertyValue('--color-primary').trim(),
    )
    expect(teal).not.toBe(indigo)
    expect(teal.toLowerCase()).toBe('#0d9488')
  })

  test('TrendPill markup and .progress-thin render with the intended tones (unwired — injected only for this check)', async ({
    page,
  }) => {
    await page.goto('/find-workspace')
    const result = await page.evaluate(() => {
      const wrap = document.createElement('div')
      wrap.innerHTML = `
        <span class="trend-pill badge-tone-success" id="pw-trend"><span>+8%</span></span>
        <div class="progress progress-thin" id="pw-progress"><div class="progress-bar" style="width: 58%"></div></div>
      `
      document.body.appendChild(wrap)
      const pill = document.getElementById('pw-trend')!
      const bar = document.getElementById('pw-progress')!
      const measured = {
        pillRadius: getComputedStyle(pill).borderRadius,
        pillBg: getComputedStyle(pill).backgroundColor,
        barBorderRadius: getComputedStyle(bar).getPropertyValue('--bs-progress-border-radius').trim(),
      }
      wrap.remove()
      return measured
    })
    expect(result.pillRadius).toBe('999px')
    expect(result.pillBg).not.toBe('rgba(0, 0, 0, 0)')
    expect(result.barBorderRadius).toBe('999px')
  })
})
