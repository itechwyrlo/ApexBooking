import { test, expect } from '@playwright/test'

// Every real table in the app (11 of them, see the Step 4 report's inventory) lives behind
// ProtectedRoute/SuperAdminProtectedRoute — none render on a public page, and this repo still has
// no seeded test account or auth bypass (not created here either). This spec instead verifies the
// shared `.table-stack`/`.table-refined`/`.table-cell-truncate` CSS mechanics directly — the exact
// rules every migrated/kept table relies on — through the real stylesheet on a public page
// (/find-workspace), at the five viewports requested.

const VIEWPORTS = [
  { width: 320, height: 900, label: 'narrow mobile' },
  { width: 375, height: 900, label: 'mobile' },
  { width: 430, height: 900, label: 'large mobile' },
  { width: 768, height: 1024, label: 'tablet' },
  { width: 1280, height: 900, label: 'desktop' },
]

// A representative fixture matching the real markup shape every migrated table uses:
// <table class="table table-stack"> + <thead>/<th> + <td data-label="...">, including a badge, a
// truncated long-text cell, and an actions cell — the specialized cell types called out in the brief.
function tableFixtureHtml(): string {
  return `
    <div class="table-responsive">
      <table class="table table-refined table-stack align-middle mb-0" id="pw-fixture-table">
        <thead>
          <tr class="text-muted small text-uppercase">
            <th scope="col" class="fw-semibold">Service</th>
            <th scope="col" class="fw-semibold text-end">Price</th>
            <th scope="col" class="fw-semibold">Status</th>
            <th scope="col" class="fw-semibold">Reference</th>
            <th scope="col" class="fw-semibold"></th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td data-label="Service">Haircut &amp; Beard Trim</td>
            <td class="text-end" data-label="Price">450.00 PHP</td>
            <td data-label="Status"><span class="badge rounded-pill fw-medium badge-tone-success">Active</span></td>
            <td data-label="Reference">
              <span class="pb-mono small text-truncate d-inline-block table-cell-truncate" id="pw-fixture-truncate" title="pay_1AbCdEfGhIjKlMnOpQrStUvWxYz0123456789">pay_1AbCdEfGhIjKlMnOpQrStUvWxYz0123456789</span>
            </td>
            <td class="text-end" data-label="Actions"><button type="button" class="btn btn-outline-secondary btn-sm btn-icon" aria-label="Edit">e</button></td>
          </tr>
        </tbody>
      </table>
    </div>
  `
}

test.describe('Step 4 responsive table structure (unauthenticated-reachable checks)', () => {
  for (const viewport of VIEWPORTS) {
    test(`table-stack switches correctly at ${viewport.width}px (${viewport.label})`, async ({ page }) => {
      await page.setViewportSize({ width: viewport.width, height: viewport.height })
      await page.goto('/find-workspace')

      await page.evaluate((html) => {
        const wrap = document.createElement('div')
        wrap.id = 'pw-fixture-wrap'
        wrap.innerHTML = html
        document.body.appendChild(wrap)
      }, tableFixtureHtml())

      const isMobileLayout = viewport.width < 768 // matches table-stack's 767.98px breakpoint

      const thead = page.locator('#pw-fixture-table thead')
      const firstCell = page.locator('#pw-fixture-table td[data-label="Service"]').first()

      if (isMobileLayout) {
        // Step 10 (see docs/superpowers/plans/2026-08-15-step10-final-audit.md §B.2): thead is no
        // longer `display: none` at this breakpoint — it stays in the DOM/accessibility tree via a
        // visually-hidden clip technique, so a screen reader still encounters the column names.
        // Playwright's own toBeHidden()/toBeVisible() only check display/visibility/opacity plus a
        // non-zero box, so a 1px-clipped element reads as "visible" to it even though no sighted
        // user can perceive it — asserting the actual clipped geometry directly instead.
        await expect(thead).not.toHaveCSS('display', 'none')
        const theadBox = await thead.boundingBox()
        expect(theadBox, 'thead should still report a (clipped) box, not be display:none').not.toBeNull()
        expect(theadBox!.width, 'thead should be visually clipped to ~0px wide').toBeLessThanOrEqual(1)
        expect(theadBox!.height, 'thead should be visually clipped to ~0px tall').toBeLessThanOrEqual(1)
        await expect(thead).toContainText('Service')

        // data-label content is only meaningful once the cell is a block; also confirms the
        // pseudo-label text itself is present in the accessibility-relevant generated content.
        const beforeContent = await firstCell.evaluate((el) => getComputedStyle(el, '::before').content)
        expect(beforeContent).toContain('Service')
      } else {
        await expect(thead).toBeVisible()
      }

      // No horizontal overflow at any of the five required viewports.
      const { scrollWidth, clientWidth } = await page.evaluate(() => ({
        scrollWidth: document.documentElement.scrollWidth,
        clientWidth: document.documentElement.clientWidth,
      }))
      expect(scrollWidth, `horizontal overflow at ${viewport.width}px`).toBeLessThanOrEqual(clientWidth)

      // The truncated reference cell relaxes its cap below the 768px breakpoint (more room once
      // stacked) and re-caps at 140px on desktop/tablet, where it still competes for column width.
      const truncateMaxWidth = await page.locator('#pw-fixture-truncate').evaluate((el) => getComputedStyle(el).maxWidth)
      if (viewport.width >= 768) {
        expect(truncateMaxWidth).toBe('140px')
      } else {
        expect(truncateMaxWidth).not.toBe('140px')
      }

      // The badge inside a table-stack cell keeps its normal pill shape/color at every width.
      const badgeRadius = await page.locator('#pw-fixture-table .badge-tone-success').evaluate((el) => getComputedStyle(el).borderRadius)
      expect(badgeRadius).not.toBe('0px')

      // The action button stays a real, focusable element regardless of layout.
      const actionButton = page.locator('#pw-fixture-table button[aria-label="Edit"]')
      await expect(actionButton).toBeVisible()
      await actionButton.focus()
      await expect(actionButton).toBeFocused()

      await page.evaluate(() => document.getElementById('pw-fixture-wrap')?.remove())
    })
  }
})
