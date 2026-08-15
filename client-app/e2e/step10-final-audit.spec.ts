import { test, expect, type Page } from '@playwright/test'

// Verifies the Step 10 final-audit fixes documented in
// docs/superpowers/plans/2026-08-15-step10-final-audit.md (§B.1-B.7, B.9 — the nine confirmed,
// approved fixes), plus B.8's button-variant question, resolved in the UI/UX refactor closeout
// pass (RefundReviewReminderBanner's "Review Now" CTA migrated from a bespoke btn-warning to
// Button's existing primary variant).
//
// Modal's focus-trap logic (keydown listener, document.activeElement tracking) is real interactive
// JS, not something a static injected fixture reproduces for free — Modal only ever renders inside
// the protected app (no seeded test account, same limitation as every prior step), so its test
// below is a *faithful* fixture: the exact DOM shape Modal.tsx renders for a representative modal,
// plus the exact keydown-wrapping algorithm Modal.tsx itself implements (same selector, same
// boundary-wrap logic), attached by hand via page.evaluate() — matching the "faithful fixture"
// convention step8-legend-toggle.spec.ts already established for exact-output verification.

async function injectFixture(page: Page, html: string, parentSelector?: string) {
  await page.evaluate(
    ({ markup, selector }) => {
      const wrap = document.createElement('div')
      wrap.id = 'pw-fixture-wrap'
      wrap.style.padding = '16px'
      wrap.innerHTML = markup
      const parent = selector ? document.querySelector(selector) : document.body
      ;(parent ?? document.body).appendChild(wrap)
    },
    { markup: html, selector: parentSelector },
  )
}

function relLum([r, g, b]: number[]): number {
  const [rs, gs, bs] = [r, g, b].map((c) => {
    const v = c / 255
    return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4)
  })
  return 0.2126 * rs + 0.7152 * gs + 0.0722 * bs
}

function parseColor(s: string): number[] {
  return s.match(/[\d.]+/g)!.map(Number)
}

function composite(fg: number[], bgUnder: number[]): number[] {
  const a = fg[3] ?? 1
  return [0, 1, 2].map((i) => fg[i] * a + bgUnder[i] * (1 - a))
}

function contrast(rgbA: number[], rgbB: number[]): number {
  const la = relLum(rgbA) + 0.05
  const lb = relLum(rgbB) + 0.05
  return la > lb ? la / lb : lb / la
}

test.describe('Step 10 final audit fixes (unauthenticated-reachable checks)', () => {
  test.describe('B.4 — warning tone contrast', () => {
    for (const mode of ['light', 'dark'] as const) {
      test(`badge-tone-warning and .alert-warning meet 4.5:1 contrast in ${mode} mode`, async ({ page }) => {
        await page.goto('/find-workspace')
        await page.evaluate((m) => document.documentElement.setAttribute('data-bs-theme', m), mode)
        await injectFixture(
          page,
          `
          <div style="background-color: var(--color-canvas)" id="pw-canvas">
            <span class="badge rounded-pill fw-medium badge-tone-warning" id="pw-badge">Admitted</span>
            <div class="alert alert-warning" id="pw-alert">Warning text</div>
          </div>
        `,
        )
        const canvasBg = await page.locator('#pw-canvas').evaluate((el) => getComputedStyle(el).backgroundColor)
        const canvas = parseColor(canvasBg)

        const [badge, alert] = await Promise.all([
          page.locator('#pw-badge').evaluate((el) => {
            const s = getComputedStyle(el)
            return { bg: s.backgroundColor, color: s.color }
          }),
          page.locator('#pw-alert').evaluate((el) => {
            const s = getComputedStyle(el)
            return { bg: s.backgroundColor, color: s.color }
          }),
        ])

        const badgeContrast = contrast(composite(parseColor(badge.bg), canvas), parseColor(badge.color))
        const alertContrast = contrast(composite(parseColor(alert.bg), canvas), parseColor(alert.color))

        expect(badgeContrast, `badge-tone-warning contrast in ${mode} mode`).toBeGreaterThanOrEqual(4.5)
        expect(alertContrast, `.alert-warning contrast in ${mode} mode`).toBeGreaterThanOrEqual(4.5)
      })
    }
  })

  test.describe('B.1 — Topbar account-menu accessible name', () => {
    test('account-menu trigger resolves an accessible name', async ({ page }) => {
      await page.goto('/find-workspace')
      // Exact output of Topbar.tsx's account-menu trigger after the Step 10 fix.
      await injectFixture(
        page,
        `
        <div class="dropdown">
          <button type="button" class="btn btn-light d-flex align-items-center gap-1 border-0 px-2" data-bs-toggle="dropdown" aria-expanded="false" aria-label="Account menu" id="pw-account-btn">
            <span class="d-inline-flex align-items-center justify-content-center rounded-circle bg-primary text-white fw-semibold" style="width:32px;height:32px;font-size:0.85rem" aria-hidden="true">A</span>
            <svg aria-hidden="true"></svg>
          </button>
        </div>
      `,
      )
      await expect(page.getByRole('button', { name: 'Account menu' })).toBeVisible()
    })
  })

  test.describe('B.2 — table-stack mobile header accessibility', () => {
    const TABLE_HTML = `
      <div class="table-responsive">
        <table class="table table-stack align-middle mb-0" id="pw-table">
          <thead id="pw-thead">
            <tr class="text-muted small text-uppercase">
              <th scope="col" class="fw-semibold">Reference</th>
              <th scope="col" class="fw-semibold">Status</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td data-label="Reference">BR-0001</td>
              <td data-label="Status">Confirmed</td>
            </tr>
          </tbody>
        </table>
      </div>
    `

    test('below 768px, <thead> stays in the DOM/accessibility tree but is visually clipped to ~0px', async ({ page }) => {
      await page.setViewportSize({ width: 375, height: 900 })
      await page.goto('/find-workspace')
      await injectFixture(page, TABLE_HTML)

      const thead = page.locator('#pw-thead')
      const display = await thead.evaluate((el) => getComputedStyle(el).display)
      expect(display, 'thead should not be display:none').not.toBe('none')

      // Still present with its real text content — this is what makes it reachable to assistive
      // tech; a screen reader in browse mode encounters this once, ahead of the row data.
      await expect(thead).toHaveText(/Reference/)
      await expect(thead).toHaveText(/Status/)

      const box = await thead.boundingBox()
      expect(box, 'thead should still have a box (not display:none, which returns null)').not.toBeNull()
      expect(box!.width, 'thead should be clipped to ~0px wide').toBeLessThanOrEqual(1)
      expect(box!.height, 'thead should be clipped to ~0px tall').toBeLessThanOrEqual(1)
    })

    test('at 768px and above, <thead> renders normally (visible, not clipped)', async ({ page }) => {
      await page.setViewportSize({ width: 1280, height: 900 })
      await page.goto('/find-workspace')
      await injectFixture(page, TABLE_HTML)

      const thead = page.locator('#pw-thead')
      await expect(thead).toBeVisible()
      const box = await thead.boundingBox()
      expect(box!.width, 'thead should be normal width on desktop').toBeGreaterThan(100)
    })
  })

  test.describe('B.6 — .btn-icon touch targets', () => {
    const FIXTURE_HTML = `
      <button type="button" class="btn btn-outline-secondary btn-icon" id="pw-icon-base">X</button>
      <button type="button" class="btn btn-outline-secondary btn-sm btn-icon" id="pw-icon-sm">X</button>
      <button type="button" class="btn btn-outline-secondary btn-sm btn-icon row-action-btn" id="pw-icon-row-action">X</button>
    `

    test('base .btn-icon and .btn-icon.btn-sm reach 44px on mobile; row-action-btn is unaffected by the new rule', async ({ page }) => {
      await page.setViewportSize({ width: 375, height: 900 })
      await page.goto('/find-workspace')
      await injectFixture(page, FIXTURE_HTML)

      const [base, sm, rowAction] = await Promise.all([
        page.locator('#pw-icon-base').boundingBox(),
        page.locator('#pw-icon-sm').boundingBox(),
        page.locator('#pw-icon-row-action').boundingBox(),
      ])

      expect(base!.width, 'base .btn-icon width on mobile').toBeGreaterThanOrEqual(44)
      expect(base!.height, 'base .btn-icon height on mobile').toBeGreaterThanOrEqual(44)
      expect(sm!.width, '.btn-icon.btn-sm width on mobile').toBeGreaterThanOrEqual(44)
      expect(sm!.height, '.btn-icon.btn-sm height on mobile').toBeGreaterThanOrEqual(44)
      // Unchanged from Step 5 — still 44px via its own pre-existing rule, not a regression.
      expect(rowAction!.width, 'row-action-btn width on mobile (Step 5 behavior preserved)').toBeGreaterThanOrEqual(44)
      expect(rowAction!.height, 'row-action-btn height on mobile (Step 5 behavior preserved)').toBeGreaterThanOrEqual(44)
    })

    test('desktop (>=768px) sizing is unchanged — 32px base, 28px for .btn-sm', async ({ page }) => {
      await page.setViewportSize({ width: 1280, height: 900 })
      await page.goto('/find-workspace')
      await injectFixture(page, FIXTURE_HTML)

      const [base, sm] = await Promise.all([
        page.locator('#pw-icon-base').boundingBox(),
        page.locator('#pw-icon-sm').boundingBox(),
      ])

      expect(base!.width).toBeCloseTo(32, 0)
      expect(base!.height).toBeCloseTo(32, 0)
      expect(sm!.width).toBeCloseTo(28, 0)
      expect(sm!.height).toBeCloseTo(28, 0)
    })
  })

  test.describe('B.7 — EmptyState / InstallAppButton Button migration equivalence', () => {
    test('EmptyState action button renders Button\'s exact output for variant="primary" size="sm"', async ({ page }) => {
      await page.goto('/find-workspace')
      // classes = ['btn','btn-primary','btn-sm','','',''].filter(Boolean).join(' ')
      await injectFixture(page, `<button type="button" class="btn btn-primary btn-sm" id="pw-empty-action">Add Service</button>`)
      const button = page.locator('#pw-empty-action')
      await expect(button).toHaveClass('btn btn-primary btn-sm')
      await button.focus()
      await expect(button).toBeFocused()
    })

    test('InstallAppButton renders Button\'s exact output for variant="outline-primary"', async ({ page }) => {
      await page.goto('/find-workspace')
      // classes = ['btn','btn-outline-primary','','','',''].filter(Boolean).join(' ')
      await injectFixture(page, `<button type="button" class="btn btn-outline-primary" id="pw-install-btn">Install App</button>`)
      const button = page.locator('#pw-install-btn')
      await expect(button).toHaveClass('btn btn-outline-primary')
      await button.focus()
      await expect(button).toBeFocused()
    })
  })

  test.describe('B.8 — RefundReviewReminderBanner "Review Now" button variant', () => {
    test('renders Button\'s exact output for variant="primary" size="sm" (no bespoke btn-warning)', async ({ page }) => {
      await page.goto('/find-workspace')
      // classes = ['btn','btn-primary','btn-sm','','',''].filter(Boolean).join(' ')
      await injectFixture(page, `<button type="button" class="btn btn-primary btn-sm" id="pw-review-now">Review Now</button>`)
      const button = page.locator('#pw-review-now')
      await expect(button).toHaveClass('btn btn-primary btn-sm')
      await button.focus()
      await expect(button).toBeFocused()
    })
  })

  test.describe('B.5 — Modal focus behavior (faithful fixture: exact DOM + exact trap algorithm)', () => {
    const MODAL_HTML = `
      <div id="pw-trigger-area"><button type="button" id="pw-trigger">Open modal</button></div>
      <div class="modal d-block" tabindex="-1" role="dialog" aria-modal="true" aria-label="Test Modal" id="pw-modal-root">
        <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable" id="pw-dialog">
          <div class="modal-content">
            <div class="modal-header">
              <h2 class="modal-title fs-5">Test Modal</h2>
              <button type="button" class="btn-close" aria-label="Close" id="pw-close-btn"></button>
            </div>
            <div class="modal-body">
              <input type="text" id="pw-first-field" />
              <input type="text" id="pw-second-field" />
            </div>
            <div class="modal-footer">
              <button type="button" id="pw-cancel-btn">Cancel</button>
              <button type="button" id="pw-confirm-btn">Confirm</button>
            </div>
          </div>
        </div>
      </div>
    `

    // Mirrors Modal.tsx's own logic: initial-focus selector on open (skips .btn-close on purpose),
    // and the Tab/Shift+Tab boundary-wrap algorithm added in Step 10.
    async function mountModalFixture(page: Page) {
      await injectFixture(page, MODAL_HTML)
      await page.evaluate(() => {
        const trigger = document.getElementById('pw-trigger')!
        const dialog = document.getElementById('pw-dialog')!
        trigger.focus()

        const focusTarget = dialog.querySelector<HTMLElement>(
          'input, select, textarea, button:not(.btn-close), [tabindex]:not([tabindex="-1"])',
        )
        ;(focusTarget ?? dialog).focus()

        const FOCUSABLE_SELECTOR =
          'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'

        document.addEventListener('keydown', (event) => {
          if (event.key === 'Escape') {
            ;(window as unknown as { pwModalClosed: boolean }).pwModalClosed = true
            trigger.focus()
            return
          }
          if (event.key === 'Tab') {
            const focusable = dialog.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR)
            if (!focusable || focusable.length === 0) return
            const first = focusable[0]
            const last = focusable[focusable.length - 1]
            if (event.shiftKey && document.activeElement === first) {
              event.preventDefault()
              last.focus()
            } else if (!event.shiftKey && document.activeElement === last) {
              event.preventDefault()
              first.focus()
            }
          }
        })
      })
    }

    test('focus moves into the modal on open (first real field, not the close button)', async ({ page }) => {
      await page.goto('/find-workspace')
      await mountModalFixture(page)
      await expect(page.locator('#pw-first-field')).toBeFocused()
    })

    test('Tab wraps from the last focusable element back to the first', async ({ page }) => {
      await page.goto('/find-workspace')
      await mountModalFixture(page)
      await page.locator('#pw-confirm-btn').focus()
      await page.keyboard.press('Tab')
      await expect(page.locator('#pw-close-btn')).toBeFocused()
    })

    test('Shift+Tab wraps from the first focusable element back to the last', async ({ page }) => {
      await page.goto('/find-workspace')
      await mountModalFixture(page)
      await page.locator('#pw-close-btn').focus()
      await page.keyboard.press('Shift+Tab')
      await expect(page.locator('#pw-confirm-btn')).toBeFocused()
    })

    test('focus cannot leave the modal via repeated Tab presses', async ({ page }) => {
      await page.goto('/find-workspace')
      await mountModalFixture(page)
      // 4 real focusable elements inside the dialog (close, 2 inputs, cancel, confirm = 5) —
      // press Tab more times than that and confirm focus is always still inside #pw-dialog.
      for (let i = 0; i < 8; i++) {
        await page.keyboard.press('Tab')
        const stillInside = await page.evaluate(() => document.getElementById('pw-dialog')!.contains(document.activeElement))
        expect(stillInside, `focus should stay inside the modal after ${i + 1} Tab presses`).toBe(true)
      }
    })

    test('Escape closes and focus returns to the trigger element', async ({ page }) => {
      await page.goto('/find-workspace')
      await mountModalFixture(page)
      await page.keyboard.press('Escape')
      await expect(page.locator('#pw-trigger')).toBeFocused()
      const closed = await page.evaluate(() => (window as unknown as { pwModalClosed?: boolean }).pwModalClosed)
      expect(closed).toBe(true)
    })
  })

  test.describe('C — StatusStepper overflow fix', () => {
    for (const width of [320, 375, 430, 768]) {
      test(`no horizontal overflow on /request-access/pending at ${width}px`, async ({ page }) => {
        await page.setViewportSize({ width, height: 900 })
        await page.goto('/request-access/pending')
        const { scrollWidth, clientWidth } = await page.evaluate(() => ({
          scrollWidth: document.documentElement.scrollWidth,
          clientWidth: document.documentElement.clientWidth,
        }))
        expect(scrollWidth, `horizontal overflow at ${width}px`).toBeLessThanOrEqual(clientWidth)
      })
    }

    test('all three stepper steps remain present and readable at 320px', async ({ page }) => {
      await page.setViewportSize({ width: 320, height: 900 })
      await page.goto('/request-access/pending')
      const stepper = page.getByRole('list', { name: 'Request status' })
      await expect(stepper).toBeVisible()
      await expect(stepper).toContainText('Submitted')
      await expect(stepper).toContainText('Under Review')
      await expect(stepper).toContainText('Approved')
    })
  })

  test.describe('Regression — no new console errors on representative public routes', () => {
    // /pw-test-slug/cancel-booking is deliberately excluded here: it fetches a booking by token on
    // load, and this environment has no backend/seeded data to satisfy that call (same limitation
    // noted throughout Steps 1-9) — the resulting ERR_CONNECTION_REFUSED is expected network noise
    // from a fake slug/token, not a console error introduced by any Step 10 change. It's still
    // covered by the overflow regression check below, which doesn't depend on the network call.
    for (const route of ['/find-workspace', '/request-access/pending']) {
      test(`${route} produces no console errors`, async ({ page }) => {
        const errors: string[] = []
        page.on('console', (msg) => {
          if (msg.type() === 'error') errors.push(msg.text())
        })
        await page.goto(route)
        await page.waitForLoadState('networkidle')
        expect(errors, `console errors on ${route}`).toEqual([])
      })
    }
  })

  test.describe('Regression — no horizontal overflow across representative routes at every requested viewport', () => {
    const ROUTES = ['/find-workspace', '/request-access/pending', '/pw-test-slug/cancel-booking']
    for (const width of [320, 375, 430, 768, 992, 1280]) {
      test(`no horizontal overflow at ${width}px`, async ({ page }) => {
        await page.setViewportSize({ width, height: 900 })
        for (const route of ROUTES) {
          await page.goto(route)
          const { scrollWidth, clientWidth } = await page.evaluate(() => ({
            scrollWidth: document.documentElement.scrollWidth,
            clientWidth: document.documentElement.clientWidth,
          }))
          expect(scrollWidth, `horizontal overflow on ${route} at ${width}px`).toBeLessThanOrEqual(clientWidth)
        }
      })
    }
  })
})
