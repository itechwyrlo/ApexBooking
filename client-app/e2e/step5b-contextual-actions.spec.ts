import { test, expect } from '@playwright/test'

// Verifies the contextual action-presentation rules against fixtures matching RowActions' real
// output for all five representative categories, plus captures screenshots for visual review.
// Real per-table rendering (actual data, actual row counts) is still behind
// ProtectedRoute/SuperAdminProtectedRoute with no seeded test account (none created here) — see
// the report's verification-limitation note. This spec proves the shared rules the report
// documents per table, through the real stylesheet on a public page (/find-workspace).

const TABLE_FIXTURE_HTML = `
<div class="table-responsive">
  <table class="table table-stack align-middle mb-0" id="pw-actions-table">
    <thead>
      <tr class="text-muted small text-uppercase">
        <th scope="col" class="fw-semibold">Category</th>
        <th scope="col" class="fw-semibold">Row</th>
        <th scope="col" class="fw-semibold"></th>
      </tr>
    </thead>
    <tbody>
      <tr id="pw-row-one-action">
        <td data-label="Category">One action</td>
        <td data-label="Row">Downtown Branch (BranchTable-style)</td>
        <td class="text-end" data-label="Actions">
          <div class="table-actions">
            <button type="button" class="btn btn-outline-secondary btn-sm btn-icon action-icon row-action-btn action-icon-edit" aria-label="Edit Downtown Branch" title="Edit Downtown Branch">
              <span class="visually-hidden">Edit Downtown Branch</span>
            </button>
          </div>
        </td>
      </tr>
      <tr id="pw-row-two-action">
        <td data-label="Category">Two actions</td>
        <td data-label="Row">Jane Doe (TeamMemberTable-style)</td>
        <td class="text-end" data-label="Actions">
          <div class="table-actions">
            <button type="button" class="btn btn-outline-secondary btn-sm btn-icon action-icon row-action-btn action-icon-edit" aria-label="Edit Jane Doe" title="Edit Jane Doe">
              <span class="visually-hidden">Edit Jane Doe</span>
            </button>
            <button type="button" class="btn btn-outline-secondary btn-sm btn-icon action-icon row-action-btn action-icon-delete" aria-label="Remove Jane Doe" title="Remove Jane Doe">
              <span class="visually-hidden">Remove Jane Doe</span>
            </button>
          </div>
        </td>
      </tr>
      <tr id="pw-row-three-action">
        <td data-label="Category">Three+ actions</td>
        <td data-label="Row">Acme Salon request (TenantRequestTable-style)</td>
        <td class="text-end" data-label="Actions">
          <div class="dropdown d-inline-block">
            <button type="button" class="btn btn-outline-secondary btn-sm btn-icon row-action-btn" data-bs-toggle="dropdown" aria-expanded="false" aria-label="More actions" title="More actions" id="pw-three-trigger">
              more
            </button>
            <ul class="dropdown-menu dropdown-menu-end" id="pw-three-menu">
              <li><button type="button" class="dropdown-item d-flex align-items-center gap-2 text-muted">View details</button></li>
              <li><button type="button" class="dropdown-item d-flex align-items-center gap-2 text-primary">Approve</button></li>
              <li><hr class="dropdown-divider" /></li>
              <li><button type="button" class="dropdown-item d-flex align-items-center gap-2 text-danger">Reject</button></li>
            </ul>
          </div>
        </td>
      </tr>
      <tr id="pw-row-conditional">
        <td data-label="Category">Conditional (forced overflow at 2)</td>
        <td data-label="Row">Refund #4821, Pending Review (RefundRequestTable-style)</td>
        <td class="text-end" data-label="Actions">
          <div class="dropdown d-inline-block">
            <button type="button" class="btn btn-outline-secondary btn-sm btn-icon row-action-btn" data-bs-toggle="dropdown" aria-expanded="false" aria-label="More actions" title="More actions" id="pw-conditional-trigger">
              more
            </button>
            <ul class="dropdown-menu dropdown-menu-end" id="pw-conditional-menu">
              <li><button type="button" class="dropdown-item d-flex align-items-center gap-2 text-primary">Approve</button></li>
              <li><hr class="dropdown-divider" /></li>
              <li><button type="button" class="dropdown-item d-flex align-items-center gap-2 text-danger">Reject</button></li>
            </ul>
          </div>
        </td>
      </tr>
      <tr id="pw-row-readonly">
        <td data-label="Category">Read-only</td>
        <td data-label="Row">Booking BR-2201 (AdminBookingsPage-style)</td>
      </tr>
    </tbody>
  </table>
</div>
`

async function injectFixture(page: import('@playwright/test').Page) {
  await page.evaluate((html) => {
    const wrap = document.createElement('div')
    wrap.id = 'pw-fixture-wrap'
    wrap.style.padding = '16px'
    wrap.innerHTML = html
    document.body.appendChild(wrap)
  }, TABLE_FIXTURE_HTML)
}

test.describe('Step 5 (revised) contextual action presentation', () => {
  test('one-action row: direct single icon button, right-aligned, no visible "Actions" label', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 900 })
    await page.goto('/find-workspace')
    await injectFixture(page)

    const row = page.locator('#pw-row-one-action')
    await expect(row.locator('.table-actions button')).toHaveCount(1)
    await expect(row.locator('.dropdown')).toHaveCount(0)

    const labelDisplay = await row.locator('td').last().evaluate((el) => getComputedStyle(el, '::before').display)
    expect(labelDisplay).toBe('none')

    const textAlign = await row.locator('td').last().evaluate((el) => getComputedStyle(el).textAlign)
    expect(textAlign).toBe('right')
  })

  test('two-action row: both direct, destructive tone distinguishable, not hidden', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 900 })
    await page.goto('/find-workspace')
    await injectFixture(page)

    const row = page.locator('#pw-row-two-action')
    await expect(row.locator('.table-actions button')).toHaveCount(2)
    await expect(row.locator('.dropdown')).toHaveCount(0)

    const [editBg, deleteBg] = await Promise.all([
      row.locator('.action-icon-edit').evaluate((el) => getComputedStyle(el).backgroundColor),
      row.locator('.action-icon-delete').evaluate((el) => getComputedStyle(el).backgroundColor),
    ])
    expect(editBg).not.toBe(deleteBg) // distinguishable
  })

  test('three-action row: overflow menu, all three visible with text once opened', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 900 })
    await page.goto('/find-workspace')
    await injectFixture(page)

    const row = page.locator('#pw-row-three-action')
    await expect(row.locator('.table-actions')).toHaveCount(0)
    await expect(row.locator('.dropdown')).toHaveCount(1)

    await page.locator('#pw-three-trigger').click()
    await expect(page.locator('#pw-three-menu .dropdown-item')).toHaveCount(3)
  })

  test('conditional row: forced overflow at exactly 2 actions (RefundRequestTable exception)', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 900 })
    await page.goto('/find-workspace')
    await injectFixture(page)

    const row = page.locator('#pw-row-conditional')
    // Unlike the two-action row above, this one uses the overflow menu even at 2 actions.
    await expect(row.locator('.table-actions')).toHaveCount(0)
    await expect(row.locator('.dropdown')).toHaveCount(1)

    await page.locator('#pw-conditional-trigger').click()
    await expect(page.locator('#pw-conditional-menu .dropdown-item')).toHaveCount(2)
  })

  test('read-only row: no action area at all', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 900 })
    await page.goto('/find-workspace')
    await injectFixture(page)

    const row = page.locator('#pw-row-readonly')
    await expect(row.locator('.table-actions, .dropdown')).toHaveCount(0)
    await expect(row.locator('td')).toHaveCount(2) // just the two data cells, no action cell
  })

  test('screenshot: mobile (375px) — all five categories stacked', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 1000 })
    await page.goto('/find-workspace')
    await injectFixture(page)
    await page.locator('#pw-fixture-wrap').screenshot({ path: 'e2e/screenshots/step5-mobile-375.png' })
  })

  test('screenshot: desktop (1280px) — all five categories in a conventional table', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 700 })
    await page.goto('/find-workspace')
    await injectFixture(page)
    await page.locator('#pw-fixture-wrap').screenshot({ path: 'e2e/screenshots/step5-desktop-1280.png' })
  })

  test('screenshot: mobile with the three-action overflow menu open (edge clipping check)', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 1000 })
    await page.goto('/find-workspace')
    await injectFixture(page)
    await page.locator('#pw-three-trigger').click()
    await expect(page.locator('#pw-three-menu')).toBeVisible()
    await page.locator('#pw-fixture-wrap').screenshot({ path: 'e2e/screenshots/step5-mobile-overflow-open.png' })
  })
})
