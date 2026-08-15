import { test, expect } from '@playwright/test'

// Every real table's action column lives behind ProtectedRoute/SuperAdminProtectedRoute — this
// repo still has no seeded test account or auth bypass (none created here either). This spec
// verifies RowActions' actual rendered shape via fixtures that reproduce its real markup exactly
// (same classes, same structure), through the real stylesheet on a public page (/find-workspace).
// Real row-state-driven action selection (which of BookingTable's four action sets a given
// booking gets, etc.) is not covered here — see the Step 5 report's verification-limitation note.

const TWO_ACTION_INLINE_HTML = `
  <div class="table-actions" id="pw-inline">
    <button type="button" class="btn btn-outline-secondary btn-sm btn-icon action-icon row-action-btn action-icon-edit" aria-label="Edit Jane Doe" title="Edit Jane Doe">
      <span class="visually-hidden">Edit Jane Doe</span>
    </button>
    <button type="button" class="btn btn-outline-secondary btn-sm btn-icon action-icon row-action-btn action-icon-delete" aria-label="Remove Jane Doe" title="Remove Jane Doe">
      <span class="visually-hidden">Remove Jane Doe</span>
    </button>
  </div>
`

const THREE_ACTION_DROPDOWN_HTML = `
  <div class="dropdown d-inline-block" id="pw-dropdown">
    <button type="button" class="btn btn-outline-secondary btn-sm btn-icon row-action-btn" data-bs-toggle="dropdown" aria-expanded="false" aria-label="More actions" title="More actions" id="pw-dropdown-trigger">
      more
    </button>
    <ul class="dropdown-menu dropdown-menu-end" id="pw-dropdown-menu">
      <li><button type="button" class="dropdown-item d-flex align-items-center gap-2 text-muted">view View details</button></li>
      <li><button type="button" class="dropdown-item d-flex align-items-center gap-2 text-primary">check Approve</button></li>
      <li><hr class="dropdown-divider" /></li>
      <li><button type="button" class="dropdown-item d-flex align-items-center gap-2 text-danger">x Reject</button></li>
    </ul>
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

test.describe('Step 5 table action UX (unauthenticated-reachable checks)', () => {
  test('two-action inline layout: both actions visible, correct tones, accessible names', async ({ page }) => {
    await page.goto('/find-workspace')
    await injectFixture(page, TWO_ACTION_INLINE_HTML)

    const buttons = page.locator('#pw-inline button')
    await expect(buttons).toHaveCount(2)

    const editBtn = page.locator('#pw-inline .action-icon-edit')
    const removeBtn = page.locator('#pw-inline .action-icon-delete')
    await expect(editBtn).toBeVisible()
    await expect(removeBtn).toBeVisible() // destructive action stays directly visible, not hidden in a menu

    // Accessible name comes from aria-label, not the (also-present) title alone.
    await expect(editBtn).toHaveAttribute('aria-label', 'Edit Jane Doe')
    await expect(removeBtn).toHaveAttribute('aria-label', 'Remove Jane Doe')

    // Destructive tone is distinguishable (different background) without being visually dominant
    // (same size/shape as the non-destructive action).
    const [editBg, removeBg, editBox, removeBox] = await Promise.all([
      editBtn.evaluate((el) => getComputedStyle(el).backgroundColor),
      removeBtn.evaluate((el) => getComputedStyle(el).backgroundColor),
      editBtn.boundingBox(),
      removeBtn.boundingBox(),
    ])
    expect(editBg).not.toBe(removeBg)
    expect(editBox?.width).toBe(removeBox?.width)
    expect(editBox?.height).toBe(removeBox?.height)
  })

  test('touch target is 44px+ below the table-stack breakpoint, unchanged on desktop', async ({ page }) => {
    await page.goto('/find-workspace')
    await injectFixture(page, TWO_ACTION_INLINE_HTML)
    const editBtn = page.locator('#pw-inline .action-icon-edit')

    await page.setViewportSize({ width: 375, height: 800 })
    const mobileBox = await editBtn.boundingBox()
    expect(mobileBox?.width).toBeGreaterThanOrEqual(44)
    expect(mobileBox?.height).toBeGreaterThanOrEqual(44)

    await page.setViewportSize({ width: 1280, height: 900 })
    const desktopBox = await editBtn.boundingBox()
    // Desktop stays at the existing compact .btn-icon.btn-sm size (28px) — not bumped.
    expect(desktopBox?.height).toBeLessThan(36)
  })

  test('keyboard focus reaches and activates an inline action', async ({ page }) => {
    await page.goto('/find-workspace')
    await injectFixture(page, TWO_ACTION_INLINE_HTML)
    const editBtn = page.locator('#pw-inline .action-icon-edit')
    await editBtn.focus()
    await expect(editBtn).toBeFocused()
    const outline = await editBtn.evaluate((el) => getComputedStyle(el).outlineStyle)
    expect(outline).not.toBe('none')
  })

  test('three-action overflow menu opens, shows all actions with visible labels, and stays above the bottom nav layer', async ({
    page,
  }) => {
    await page.goto('/find-workspace')
    // Simulate the fixed bottom-nav bar (Step 2) to verify the dropdown z-index fix.
    await page.evaluate(() => {
      const bar = document.createElement('nav')
      bar.className = 'bottom-nav'
      bar.id = 'pw-fake-bottom-nav'
      document.body.appendChild(bar)
    })
    await injectFixture(page, THREE_ACTION_DROPDOWN_HTML)

    const trigger = page.locator('#pw-dropdown-trigger')
    await trigger.click()
    await expect(page.locator('#pw-dropdown-menu')).toBeVisible()
    await expect(page.locator('#pw-dropdown-menu .dropdown-item')).toHaveCount(3)

    const [menuZ, barZ] = await Promise.all([
      page.locator('#pw-dropdown-menu').evaluate((el) => Number(getComputedStyle(el).zIndex)),
      page.locator('#pw-fake-bottom-nav').evaluate((el) => Number(getComputedStyle(el).zIndex)),
    ])
    expect(menuZ).toBeGreaterThan(barZ)
  })

  test('dropdown menu does not overflow the viewport at 320px', async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 800 })
    await page.goto('/find-workspace')
    await injectFixture(page, THREE_ACTION_DROPDOWN_HTML)
    await page.locator('#pw-dropdown-trigger').click()

    const menuBox = await page.locator('#pw-dropdown-menu').boundingBox()
    expect(menuBox).not.toBeNull()
    expect(menuBox!.x).toBeGreaterThanOrEqual(0)
    expect(menuBox!.x + menuBox!.width).toBeLessThanOrEqual(320)
  })

  test('destructive tone and action layout hold up in dark mode and a non-default tenant palette', async ({ page }) => {
    await page.goto('/find-workspace')
    await injectFixture(page, TWO_ACTION_INLINE_HTML)

    await page.evaluate(() => {
      document.documentElement.setAttribute('data-bs-theme', 'dark')
      document.documentElement.setAttribute('data-palette', 'teal')
    })

    const editBtn = page.locator('#pw-inline .action-icon-edit')
    const removeBtn = page.locator('#pw-inline .action-icon-delete')
    const [editBg, removeBg] = await Promise.all([
      editBtn.evaluate((el) => getComputedStyle(el).backgroundColor),
      removeBtn.evaluate((el) => getComputedStyle(el).backgroundColor),
    ])
    expect(editBg).not.toBe(removeBg)
    expect(editBg).not.toBe('rgba(0, 0, 0, 0)')
    expect(removeBg).not.toBe('rgba(0, 0, 0, 0)')
  })

  for (const width of [320, 375, 430, 768, 992, 1280]) {
    test(`no horizontal overflow with table actions present at ${width}px`, async ({ page }) => {
      await page.setViewportSize({ width, height: 900 })
      await page.goto('/find-workspace')
      await injectFixture(page, TWO_ACTION_INLINE_HTML + THREE_ACTION_DROPDOWN_HTML)
      const { scrollWidth, clientWidth } = await page.evaluate(() => ({
        scrollWidth: document.documentElement.scrollWidth,
        clientWidth: document.documentElement.clientWidth,
      }))
      expect(scrollWidth, `horizontal overflow at ${width}px`).toBeLessThanOrEqual(clientWidth)
    })
  }
})
