# Theme Color & Dark/Light Mode

## Context

`BusinessProfile` ([BusinessProfile.cs](../../../ApexBooking.Core.Domain/Entities/BusinessProfile.cs))
currently has no color/theme fields at all. `theme.css` defines one flat set of
`--color-primary*` custom properties with no dark-mode variant, no `data-bs-theme`, no
`prefers-color-scheme` — dark mode doesn't exist anywhere in this app today. The tenant's
`Plan` (`SubscriptionPlanType`: `Basic`/`Professional`/`Enterprise`) isn't exposed to the
frontend anywhere — not in the JWT, not in any response DTO — despite this feature needing
it in multiple places.

## Decisions (confirmed with user)

- One shared brand color per tenant, applied consistently to both the dashboard and the
  public booking page — not two independent settings.
- Color is chosen from a **small curated palette catalog**, not an open color
  picker — each palette ships with a pre-verified light *and* dark variant together, so no
  color combination reaches production untested for dark-mode legibility.
- Light/dark mode is **two separate settings**, not one:
  - **Public booking page**: tenant-wide, Owner-configured — a deliberate branding choice,
    not the visitor's system preference.
  - **Dashboard**: **personal**, per logged-in user, stored in `localStorage` only (no
    backend persistence — no user-preferences storage exists anywhere in this app today,
    and a UI preference doesn't justify adding one; accepted trade-off: doesn't follow the
    user across devices).
- **Plan gating**: color palette selection is available on every plan. Both dark/light
  capabilities (public page *and* dashboard) require `Professional` or `Enterprise` — on
  `Basic`, neither surface can ever be dark, regardless of any stale local state.

## Backend design (ApexBooking)

### Palette catalog — static, not database-driven

A small fixed set of palette IDs (`"indigo"`, `"teal"`, `"rose"`, `"amber"`, `"forest"`,
`"slate"` — exact hex values for each variant chosen at implementation time using the same
color-design process the existing indigo brand color followed). The catalog itself lives
as **static CSS** (see Frontend section) — the backend only ever stores/validates a
`ThemePaletteId` string against the known set of IDs, never transmits actual color values.
This keeps the wire payload tiny and means a palette's exact colors can be refined later
without any backend/API change.

### `BusinessProfile`

Two new fields, alongside the existing `BusinessName`/`Logo`/`BusinessType`/`Description`:

```csharp
public string ThemePaletteId { get; private set; } = "indigo"; // default matches today's brand color
public bool PublicPageDarkMode { get; private set; } = false;

public void UpdateAppearance(string themePaletteId, bool publicPageDarkMode, bool tenantCanUseDarkMode)
{
    if (!KnownPaletteIds.Contains(themePaletteId))
        throw new BusinessRuleBrokenException("Unrecognized theme palette.");

    // Defense in depth — the UI already hides this control on Basic plan, but the
    // handler passes the tenant's actual plan through rather than trusting the client.
    if (publicPageDarkMode && !tenantCanUseDarkMode)
        throw new BusinessRuleBrokenException("Dark mode requires the Professional plan or above.");

    ThemePaletteId = themePaletteId;
    PublicPageDarkMode = publicPageDarkMode;
}
```

Separate from the existing `UpdateDetails(name, description, logoUrl)` — different UI page
("Appearance" vs. "Business Profile"), same underlying entity/table row (no new migration
table needed, just two new nullable-with-default columns).

### New command/query: `Features/Tenancy/{Commands,Queries}/Appearance/`

- `GetAppearanceQuery` → `AppearanceDto(string ThemePaletteId, bool PublicPageDarkMode, SubscriptionPlanType Plan)`.
  Includes `Plan` so the dashboard shell can gate the personal toggle and the Appearance
  page can gate/disable the public-page switch — one fetch covers both.
- `UpdateAppearanceCommand(string ThemePaletteId, bool PublicPageDarkMode) : ICommand` —
  handler loads `tenant.Plan`, calls `tenant.BusinessProfile.UpdateAppearance(..., tenantCanUseDarkMode: tenant.Plan != SubscriptionPlanType.Basic)`.

### New anonymous endpoint for the public page

`GET /api/public/{slug}/theme` → `PublicThemeDto(string ThemePaletteId, bool IsDarkMode)`.
Deliberately minimal and separate from business-name/banner/logo data (that's the
already-flagged, separate future "public page branding" project) — this endpoint exists
only to answer "what color/mode should this page render in," fetched once at wizard mount
alongside (not instead of) the existing `getPublicBranches` call.

### `TenantController.cs`

```csharp
[HttpGet("appearance")]
[ProducesResponseType(typeof(AppearanceDto), StatusCodes.Status200OK)]
public async Task<IActionResult> GetAppearance() { ... }

[HttpPut("appearance")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> UpdateAppearance([FromBody] UpdateAppearanceCommand command) { ... }
```

Both inherit the class-level `ManagementOnly` policy (Owner+Admin) — consistent with
other business-settings actions; Business Profile's own settings page is Owner-only via
its nav-item role gate, so I'd mirror that here (Owner-only nav entry) even though the
policy itself would technically also allow Admin.

New action on `BookingsController.cs`'s public surface:

```csharp
[HttpGet("/api/public/{slug}/theme")]
[AllowAnonymous]
public async Task<IActionResult> GetPublicTheme() { ... }
```

## Frontend design (LocalFlow)

### Palette catalog as static CSS — `styles/theme.css`

```css
[data-palette="teal"] {
  --color-primary: #0d9488;
  --color-primary-strong: #0f766e;
  --color-primary-soft: rgba(13, 148, 136, 0.08);
  --color-primary-soft-strong: rgba(13, 148, 136, 0.14);
}
[data-palette="teal"][data-theme="dark"] {
  --color-primary: #2dd4bf;
  --color-primary-strong: #5eead4;
  --color-primary-soft: rgba(45, 212, 191, 0.16);
  --color-primary-soft-strong: rgba(45, 212, 191, 0.24);
  /* + surface/background/text tokens for dark mode generally */
}
```

One block per palette × mode. `[data-palette="indigo"]` (no `[data-theme]` qualifier)
matches today's existing `:root` values exactly, so the default tenant experience is
visually unchanged. A parallel matching TS catalog (`config/themePalettes.ts`) holds
`{ id, name, swatchColor }` for the picker UI — small, deliberately duplicated source of
truth between CSS (rendering) and TS (picker), acceptable for a fixed set this size.

### Dashboard: `layouts/DashboardLayout.tsx`

Fetches `GetAppearance` once at mount (new `useAppearance` hook, same shape as
`useBusinessProfile`). Sets `document.documentElement.dataset.palette = appearance.themePaletteId`.
For `data-theme`: reads the personal `localStorage` preference **only if** `appearance.plan !== 'Basic'`;
otherwise forces `'light'` regardless of any stored value (handles the downgrade case —
plan changed since the preference was saved).

### `components/layout/Topbar.tsx`

New icon-only toggle button, same style/position as the existing sidebar-collapse and
public-page-link buttons, rendered only when `appearance.plan !== 'Basic'`:

```tsx
{appearance.plan !== 'Basic' && (
  <Button
    variant="outline-secondary"
    icon={isDark ? 'sun' : 'moon'}
    iconOnly
    aria-label={isDark ? 'Switch to light mode' : 'Switch to dark mode'}
    onClick={toggleDashboardTheme}
  >
    {isDark ? 'Switch to light mode' : 'Switch to dark mode'}
  </Button>
)}
```

`sun.svg`/`moon.svg` icons don't exist yet in `public/assets/icons/` — added, matching the
existing single-color stroke style. `toggleDashboardTheme` flips `document.documentElement.dataset.theme`
and persists to `localStorage`.

### New Settings page: `pages/booking/settings/AppearanceSettingsPage.tsx`

Added to `SETTINGS_NAV_ITEMS` (`{ label: 'Appearance', href: 'settings/appearance' }`),
Owner-only via the existing "Settings" parent nav-item role gate. Palette swatch picker
(always enabled) + public-page dark/light switch (disabled with an upgrade nudge when
`plan === 'Basic'`).

### Public booking wizard

`usePublicBookingWizard.ts`'s mount effect adds a `getPublicTheme(slug)` call alongside
the existing branches fetch, applying `data-palette`/`data-theme` to `PublicBookingLayout`'s
root element (or `document.documentElement`, scoped since the public page and dashboard
never render in the same page load — no attribute collision risk).

## Non-goals

No banner image, staff-photo styling, or business-name/logo display on the public page —
that's the separate, already-flagged "public booking page branding" project. No
per-tenant custom/arbitrary colors (curated catalog only). No cross-device sync for the
personal dashboard preference. No change to `BusinessProfile.UpdateDetails` or its
existing settings page.

## Testing

- `UpdateAppearanceCommand` on a `Basic`-plan tenant with `PublicPageDarkMode: true` →
  rejected, even if called directly (not just hidden in UI).
- Public theme endpoint for a tenant that never configured appearance → returns the
  `"indigo"` default, `IsDarkMode: false`.
- Dashboard: tenant downgraded from Professional to Basic, browser still has
  `localStorage` dark preference from before → dashboard renders light, toggle hidden.
- Palette switch on the Appearance page reflects on the *same* Owner's dashboard
  immediately (next `DashboardLayout` mount / navigation), not just for other users after
  their next login.
