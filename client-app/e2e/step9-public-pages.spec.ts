import { test, expect, type Page } from '@playwright/test'

// Verifies the Step 9 public-pages consistency pass documented in
// docs/superpowers/plans/2026-08-15-step9-public-pages-consistency-pass.md.
//
// All six routes/behaviors below are reachable without a seeded test account — they are public
// pages by design — so this spec favors real navigation over fixtures wherever the target markup
// renders unconditionally. Two things still need fixtures because reaching them for real requires
// backend state this repo can't fabricate (a live cancellation token, a failed password-reset
// submission): ResetPasswordPage's post-submit auth-error-banner, and CancelBookingPage's
// mid-cancellation loading state. Those fixtures reproduce the exact markup the real components
// render (verified by reading the component source, same approach as step8-legend-toggle.spec.ts),
// not approximations.
//
// .pb-alert-danger and .pb-btn-primary resolve CSS custom properties (--pb-danger, --pb-accent)
// that are only defined on .pb-root itself (publicBooking.css) and inherit down the DOM — so their
// fixtures are injected as children of the real .pb-root element on /:slug/cancel-booking, not
// appended to document.body, or the custom properties would resolve to nothing.

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

test.describe('Step 9 public pages consistency pass (unauthenticated-reachable checks)', () => {
  test('RequestAccessPendingPage renders through the shared Card (border-0 shadow-sm, 20px radius) with content intact', async ({
    page,
  }) => {
    await page.goto('/request-access/pending')
    const card = page.locator('.pending-card')
    await expect(card).toBeVisible()
    await expect(card).toHaveClass(/\bcard\b/)
    await expect(card).toHaveClass(/\bborder-0\b/)
    await expect(card).toHaveClass(/\bshadow-sm\b/)
    expect(await card.evaluate((el) => getComputedStyle(el).borderTopLeftRadius)).toBe('20px')

    await expect(page.getByRole('heading', { name: 'Request Received' })).toBeVisible()
    await expect(page.getByRole('link', { name: 'Back to Home' })).toBeVisible()
    await expect(page.getByRole('link', { name: /Wrong email\? Contact support/ })).toHaveAttribute(
      'href',
      /^mailto:/,
    )
  })

  test('AuthLayout renders its content through the shared Card on /find-workspace', async ({ page }) => {
    await page.goto('/find-workspace')
    const card = page.locator('.card').first()
    await expect(card).toBeVisible()
    await expect(card).toHaveClass(/\bborder-0\b/)
    await expect(card).toHaveClass(/\bshadow-sm\b/)
    expect(await card.evaluate((el) => getComputedStyle(el).borderTopLeftRadius)).toBe('20px')
  })

  test('AuthLayout Card also renders on ResetPasswordPage\'s invalid-link state', async ({ page }) => {
    await page.goto('/pw-test-slug/reset-password')
    await expect(page.getByRole('heading', { name: 'Invalid Link' })).toBeVisible()
    const card = page.locator('.card').first()
    await expect(card).toBeVisible()
    await expect(card).toHaveClass(/\bborder-0\b/)
    await expect(card).toHaveClass(/\bshadow-sm\b/)
  })

  test('auth-error-banner adds its fade-in treatment; a bare alert-danger has no animation', async ({ page }) => {
    await page.goto('/find-workspace')
    await injectFixture(
      page,
      `
      <div class="alert alert-danger" id="pw-alert-plain" role="alert">Plain</div>
      <div class="alert alert-danger auth-error-banner" id="pw-alert-banner" role="alert">Banner</div>
    `,
    )
    const [plainAnim, bannerAnim] = await Promise.all([
      page.locator('#pw-alert-plain').evaluate((el) => getComputedStyle(el).animationName),
      page.locator('#pw-alert-banner').evaluate((el) => getComputedStyle(el).animationName),
    ])
    expect(plainAnim).toBe('none')
    expect(bannerAnim).not.toBe('none')
  })

  test('.alert-danger/.alert-warning/.alert-success resolve brand tokens, each visually distinct, in light mode', async ({
    page,
  }) => {
    await page.goto('/find-workspace')
    await injectFixture(
      page,
      `
      <div class="alert alert-danger" id="pw-alert-danger" role="alert">Danger</div>
      <div class="alert alert-warning" id="pw-alert-warning" role="alert">Warning</div>
      <div class="alert alert-success" id="pw-alert-success" role="alert">Success</div>
      <div style="color: var(--color-danger); background-color: var(--color-danger-soft)" id="pw-probe-danger"></div>
      <div style="color: var(--color-success); background-color: var(--color-success-soft)" id="pw-probe-success"></div>
    `,
    )

    const read = (selector: string) =>
      page.locator(selector).evaluate((el) => {
        const s = getComputedStyle(el)
        return { bg: s.backgroundColor, color: s.color }
      })

    const [danger, warning, success, probeDanger, probeSuccess] = await Promise.all([
      read('#pw-alert-danger'),
      read('#pw-alert-warning'),
      read('#pw-alert-success'),
      read('#pw-probe-danger'),
      read('#pw-probe-success'),
    ])

    // Re-themed to this app's danger/success tokens, not left at Bootstrap's stock colors.
    expect(danger.bg).toBe(probeDanger.bg)
    expect(danger.color).toBe(probeDanger.color)
    expect(success.bg).toBe(probeSuccess.bg)
    expect(success.color).toBe(probeSuccess.color)

    // All three tones stay visually distinct from one another.
    const backgrounds = new Set([danger.bg, warning.bg, success.bg])
    expect(backgrounds.size).toBe(3)
  })

  test('.alert-* colors repaint under dark mode', async ({ page }) => {
    await page.goto('/find-workspace')
    await injectFixture(
      page,
      `
      <div class="alert alert-danger" id="pw-alert-danger" role="alert">Danger</div>
      <div class="alert alert-warning" id="pw-alert-warning" role="alert">Warning</div>
      <div class="alert alert-success" id="pw-alert-success" role="alert">Success</div>
    `,
    )
    const readAll = () =>
      page.evaluate(() => {
        const ids = ['pw-alert-danger', 'pw-alert-warning', 'pw-alert-success']
        return Object.fromEntries(
          ids.map((id) => [id, getComputedStyle(document.getElementById(id)!).backgroundColor]),
        )
      })

    const light = await readAll()
    await page.evaluate(() => document.documentElement.setAttribute('data-bs-theme', 'dark'))
    const dark = await readAll()

    expect(dark['pw-alert-danger']).not.toBe(light['pw-alert-danger'])
    expect(dark['pw-alert-warning']).not.toBe(light['pw-alert-warning'])
    expect(dark['pw-alert-success']).not.toBe(light['pw-alert-success'])
  })

  test('.pb-alert-danger inside a real .pb-root resolves --pb-danger tokens, distinct from the generic --color-danger alert', async ({
    page,
  }) => {
    await page.goto('/pw-test-slug/cancel-booking')
    await expect(page.locator('.pb-root')).toBeVisible()

    await injectFixture(
      page,
      `
      <div class="alert alert-danger pb-alert-danger" id="pw-pb-alert" role="alert">Danger</div>
      <div style="color: var(--pb-danger); background-color: var(--pb-danger-soft)" id="pw-pb-probe"></div>
      <div style="color: var(--color-danger); background-color: var(--color-danger-soft)" id="pw-generic-probe"></div>
    `,
      '.pb-root',
    )

    const read = (selector: string) =>
      page.locator(selector).evaluate((el) => {
        const s = getComputedStyle(el)
        return { bg: s.backgroundColor, color: s.color }
      })

    const [pbAlert, pbProbe, genericProbe] = await Promise.all([
      read('#pw-pb-alert'),
      read('#pw-pb-probe'),
      read('#pw-generic-probe'),
    ])

    expect(pbAlert.bg).toBe(pbProbe.bg)
    expect(pbAlert.color).toBe(pbProbe.color)
    // The .pb-root token set is a separate palette from the protected-app --color-* tokens.
    expect(pbAlert.bg).not.toBe(genericProbe.bg)
  })

  test('.pb-btn-primary inside a real .pb-root resolves --pb-accent, not the protected-app --color-primary', async ({
    page,
  }) => {
    await page.goto('/pw-test-slug/cancel-booking')
    await expect(page.locator('.pb-root')).toBeVisible()

    await injectFixture(
      page,
      `
      <button type="button" class="btn pb-btn-primary" id="pw-pb-btn">Submit</button>
      <div style="background-color: var(--pb-accent)" id="pw-pb-accent-probe"></div>
      <div style="background-color: var(--color-primary)" id="pw-color-primary-probe"></div>
    `,
      '.pb-root',
    )

    const [btnBg, pbAccentBg, colorPrimaryBg] = await Promise.all([
      page.locator('#pw-pb-btn').evaluate((el) => getComputedStyle(el).backgroundColor),
      page.locator('#pw-pb-accent-probe').evaluate((el) => getComputedStyle(el).backgroundColor),
      page.locator('#pw-color-primary-probe').evaluate((el) => getComputedStyle(el).backgroundColor),
    ])

    expect(btnBg).toBe(pbAccentBg)
    expect(btnBg).not.toBe(colorPrimaryBg)
  })

  test('CancelBookingPage\'s migrated cancel Button renders the spinner and disables itself while isLoading', async ({
    page,
  }) => {
    await page.goto('/pw-test-slug/cancel-booking')
    await expect(page.locator('.pb-root')).toBeVisible()

    // Exact output of Button.tsx for variant="danger" fullWidth isLoading:
    // classes = ['btn','btn-danger','','w-100','',''].filter(Boolean).join(' ')
    // content = spinner span + label; disabled = isLoading
    await injectFixture(
      page,
      `
      <button type="button" class="btn btn-danger w-100" id="pw-cancel-btn" disabled>
        <span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>
        Cancelling…
      </button>
    `,
      '.pb-root',
    )

    const button = page.locator('#pw-cancel-btn')
    await expect(button).toBeVisible()
    await expect(button).toBeDisabled()
    await expect(button).toHaveClass('btn btn-danger w-100')
    await expect(button.locator('.spinner-border')).toBeVisible()
    await expect(button).toContainText('Cancelling…')
  })

  // /request-access/pending was excluded at 320px here originally: StatusStepper
  // (components/requestAccess/StatusStepper.tsx, untouched by Step 9) self-overflowed by ~42px at
  // that width regardless of the Card migration — confirmed at the time by measuring the migrated
  // Card's own box, which already fit fully inside the 320px viewport (right edge at 308px) with
  // the overflow originating entirely inside StatusStepper's content. Fixed in Step 10 (see
  // docs/superpowers/plans/2026-08-15-step10-final-audit.md §8 / the StatusStepper component's own
  // narrow-viewport CSS) — the exclusion is no longer needed and has been removed.
  const ROUTES = ['/request-access/pending', '/find-workspace', '/pw-test-slug/cancel-booking']
  for (const width of [320, 375, 430, 768, 1280]) {
    test(`no horizontal overflow at ${width}px across the Step 9 routes`, async ({ page }) => {
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

  test('the migrated Card box stays within the 320px viewport on /request-access/pending', async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 900 })
    await page.goto('/request-access/pending')
    const box = await page.locator('.pending-card').boundingBox()
    expect(box).not.toBeNull()
    expect(box!.x + box!.width, 'Card box right edge should stay within the 320px viewport').toBeLessThanOrEqual(320)
  })
})
