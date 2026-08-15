import { test, expect } from '@playwright/test'

// All four dashboards live behind ProtectedRoute/SuperAdminProtectedRoute — no seeded test account
// exists in this repo, none created here. This spec verifies StatTile's real rendered shape (both
// variants) and the two grid arrangements Step 6 introduces (3-tile col-6/col-md-4 revenue row,
// 5-tile row-cols tenant-lifecycle row) through the real stylesheet on a public page
// (/find-workspace), at the six requested viewports. Real dashboard data/loading states are not
// covered — see the report's verification-limitation note.

const THREE_TILE_ROW_HTML = `
<div class="row g-3 mb-3" id="pw-revenue-row">
  <div class="col-6 col-md-4">
    <div class="card border-0 shadow-sm h-100 stat-tile-emphasized" id="pw-total-tile">
      <div class="card-body">
        <div class="d-flex align-items-center gap-2 mb-2">
          <span class="stat-tile-icon-chip d-inline-flex align-items-center justify-content-center rounded-circle badge-tone-primary" style="width:36px;height:36px"><span>icon</span></span>
          <p class="text-muted small mb-0">Total Revenue</p>
        </div>
        <p class="stat-tile-value fs-3 fw-bold mb-0">1,250.00 PHP</p>
      </div>
    </div>
  </div>
  <div class="col-6 col-md-4">
    <div class="card border-0 shadow-sm h-100" id="pw-online-tile">
      <div class="card-body">
        <div class="d-flex align-items-center gap-2 mb-2">
          <span class="stat-tile-icon-chip d-inline-flex align-items-center justify-content-center rounded-circle badge-tone-success" style="width:36px;height:36px"><span>icon</span></span>
          <p class="text-muted small mb-0">Online Revenue</p>
        </div>
        <p class="stat-tile-value fs-3 fw-bold mb-0">800.00 PHP</p>
      </div>
    </div>
  </div>
  <div class="col-6 col-md-4">
    <div class="card border-0 shadow-sm h-100" id="pw-pov-tile">
      <div class="card-body">
        <div class="d-flex align-items-center gap-2 mb-2">
          <span class="stat-tile-icon-chip d-inline-flex align-items-center justify-content-center rounded-circle badge-tone-neutral" style="width:36px;height:36px"><span>icon</span></span>
          <p class="text-muted small mb-0">Pay-on-Visit Revenue</p>
        </div>
        <p class="stat-tile-value fs-3 fw-bold mb-0">450.00 PHP</p>
      </div>
    </div>
  </div>
</div>
`

const FIVE_TILE_ROW_HTML = `
<div class="row g-3 row-cols-2 row-cols-md-3 row-cols-lg-5" id="pw-lifecycle-row">
  ${['Total Tenants', 'Active Tenants', 'Pending Requests', 'Approved Requests', 'Rejected Requests']
    .map(
      (label, i) => `
    <div class="col">
      <div class="card border-0 shadow-sm" id="pw-lifecycle-tile-${i}">
        <div class="card-body">
          <p class="text-muted small mb-2">${label}</p>
          <p class="fs-3 fw-bold mb-0">${(i + 1) * 3}</p>
        </div>
      </div>
    </div>`,
    )
    .join('')}
</div>
`

async function injectFixture(page: import('@playwright/test').Page, html: string) {
  await page.evaluate((markup) => {
    const wrap = document.createElement('div')
    wrap.id = 'pw-fixture-wrap'
    wrap.style.padding = '16px'
    wrap.innerHTML = markup
    document.body.appendChild(wrap)
  }, html)
}

test.describe('Step 6 dashboard presentation (unauthenticated-reachable checks)', () => {
  test('emphasized StatTile is visually distinct from its siblings but stays the same size', async ({ page }) => {
    await page.goto('/find-workspace')
    await injectFixture(page, THREE_TILE_ROW_HTML)

    const [totalBg, onlineBg, totalBox, onlineBox] = await Promise.all([
      page.locator('#pw-total-tile').evaluate((el) => getComputedStyle(el).backgroundColor),
      page.locator('#pw-online-tile').evaluate((el) => getComputedStyle(el).backgroundColor),
      page.locator('#pw-total-tile').boundingBox(),
      page.locator('#pw-online-tile').boundingBox(),
    ])
    expect(totalBg).not.toBe(onlineBg) // emphasized tint is distinct
    expect(totalBox?.height).toBe(onlineBox?.height) // but the card itself isn't a different size

    const valueColor = await page.locator('#pw-total-tile .stat-tile-value').evaluate((el) => getComputedStyle(el).color)
    const siblingValueColor = await page.locator('#pw-online-tile .stat-tile-value').evaluate((el) => getComputedStyle(el).color)
    expect(valueColor).not.toBe(siblingValueColor) // emphasized value text is bolder-toned too
  })

  test('revenue row tiles stay equal height at 375px even when a currency value wraps to two lines', async ({ page }) => {
    // The narrow col-6 column at this width is exactly where a longer value ("1,250.00 PHP")
    // wraps while a shorter sibling ("800.00 PHP") doesn't — .card needs h-100 for Bootstrap's
    // row-stretch behavior to actually reach the card element, not just its column.
    await page.setViewportSize({ width: 375, height: 900 })
    await page.goto('/find-workspace')
    await injectFixture(page, THREE_TILE_ROW_HTML)

    const [totalBox, onlineBox] = await Promise.all([
      page.locator('#pw-total-tile').boundingBox(),
      page.locator('#pw-online-tile').boundingBox(),
    ])
    expect(totalBox!.height).toBe(onlineBox!.height)
  })

  for (const width of [320, 375, 430, 768, 992, 1280]) {
    test(`revenue row and lifecycle row switch column count correctly at ${width}px, no overflow`, async ({ page }) => {
      await page.setViewportSize({ width, height: 900 })
      await page.goto('/find-workspace')
      await injectFixture(page, THREE_TILE_ROW_HTML + FIVE_TILE_ROW_HTML)

      const isMdUp = width >= 768
      const isLgUp = width >= 992

      // 3-tile revenue row: col-6 (2-per-row) below 768px, col-md-4 (3-per-row) at/above it.
      const [totalBox, onlineBox, povBox] = await Promise.all([
        page.locator('#pw-total-tile').boundingBox(),
        page.locator('#pw-online-tile').boundingBox(),
        page.locator('#pw-pov-tile').boundingBox(),
      ])
      if (isMdUp) {
        expect(totalBox!.y).toBe(onlineBox!.y) // all three share one row
        expect(onlineBox!.y).toBe(povBox!.y)
      } else {
        expect(totalBox!.y).toBe(onlineBox!.y) // first two share a row
        expect(povBox!.y).toBeGreaterThan(onlineBox!.y) // third wraps to its own row
      }

      // 5-tile lifecycle row: 2-per-row until md, 3-per-row until lg, 5-per-row at lg+.
      const tileBoxes = await Promise.all(
        [0, 1, 2, 3, 4].map((i) => page.locator(`#pw-lifecycle-tile-${i}`).boundingBox()),
      )
      const rowYs = new Set(tileBoxes.map((box) => box!.y))
      if (isLgUp) {
        expect(rowYs.size).toBe(1) // all 5 on one row
      } else if (isMdUp) {
        expect(rowYs.size).toBe(2) // 3 + 2
      } else {
        expect(rowYs.size).toBe(3) // 2 + 2 + 1
      }

      const { scrollWidth, clientWidth } = await page.evaluate(() => ({
        scrollWidth: document.documentElement.scrollWidth,
        clientWidth: document.documentElement.clientWidth,
      }))
      expect(scrollWidth, `horizontal overflow at ${width}px`).toBeLessThanOrEqual(clientWidth)
    })
  }

  test('StatTile emphasized styling holds up in dark mode and the teal tenant palette', async ({ page }) => {
    await page.goto('/find-workspace')
    await injectFixture(page, THREE_TILE_ROW_HTML)
    await page.evaluate(() => {
      document.documentElement.setAttribute('data-bs-theme', 'dark')
      document.documentElement.setAttribute('data-palette', 'teal')
    })

    const [totalBg, onlineBg] = await Promise.all([
      page.locator('#pw-total-tile').evaluate((el) => getComputedStyle(el).backgroundColor),
      page.locator('#pw-online-tile').evaluate((el) => getComputedStyle(el).backgroundColor),
    ])
    expect(totalBg).not.toBe(onlineBg)
    expect(totalBg).not.toBe('rgba(0, 0, 0, 0)')
  })

  test('all five lifecycle tiles are the same height regardless of label length', async ({ page }) => {
    await page.goto('/find-workspace')
    await injectFixture(page, FIVE_TILE_ROW_HTML)
    const heights = await Promise.all(
      [0, 1, 2, 3, 4].map((i) => page.locator(`#pw-lifecycle-tile-${i}`).evaluate((el) => el.getBoundingClientRect().height)),
    )
    expect(new Set(heights).size).toBe(1)
  })
})

test.describe('Step 6 screenshots', () => {
  test('screenshot: mobile (375px)', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 1100 })
    await page.goto('/find-workspace')
    await injectFixture(page, THREE_TILE_ROW_HTML + FIVE_TILE_ROW_HTML)
    await page.locator('#pw-fixture-wrap').screenshot({ path: 'e2e/screenshots/step6-mobile-375.png' })
  })

  test('screenshot: desktop (1280px)', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 700 })
    await page.goto('/find-workspace')
    await injectFixture(page, THREE_TILE_ROW_HTML + FIVE_TILE_ROW_HTML)
    await page.locator('#pw-fixture-wrap').screenshot({ path: 'e2e/screenshots/step6-desktop-1280.png' })
  })
})
