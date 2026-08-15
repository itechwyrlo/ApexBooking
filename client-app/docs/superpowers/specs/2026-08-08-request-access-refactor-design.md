# Request Access Page Refactor — Design Spec

Status: Approved (source: user-supplied refactor prompt + brainstorming session,
this document is the resolved technical spec)
Scope: `RequestAccessPage` and its immediate supporting pieces only. No changes
to `RequestAccessPendingPage`'s content, submit logic, validation *rules*
(only their presentation), field names, `IRequestAccessFormValues`, or the
`requestAccess` service call.

## Source of Truth

Content/interaction requirements come from the user-supplied refactor prompt
for this task. This document records the technical decisions needed to turn
that prompt into code, including two points where the prompt's literal ask
doesn't match the app's actual data and was resolved with the user before
writing this spec.

## Deviations From the Literal Prompt

1. **Business type scope.** The prompt describes the chip selector matching
   the landing page's 4-vertical Live/Coming-Soon list (Salon, Barbershop
   live; Clinic, Fitness coming soon). That list is marketing-only. This
   form's `businessType` field is backed by the real `BusinessType` enum
   (`src/types/BusinessType.ts`) with 8 values: BarberShop, Salon, Spa,
   Clinic, FitnessStudio, AutoRepair, RetailService, Other. Confirmed with
   the user: the chip selector shows **all 8** real values, styled like the
   landing page's card pattern (icon + label) but with **no** Live/Coming
   Soon badge — every option here is a genuine, currently-supported signup
   path, so badging any of them "coming soon" would misrepresent the
   product. Narrowing to 4 would silently remove real signup options.
2. **Plan price.** The prompt asks the plan summary card to show "plan
   name, price, and features." `IPricingPlan` has no `price` field anywhere
   in the app — the pricing section itself has never displayed a number.
   Confirmed with the user: the card shows the plan's existing one-line
   `description` in place of a price, rather than fabricating one.

## 1. Page Shell

`RequestAccessPage` stops rendering inside the shared `AuthLayout` (which
puts a brand-illustration panel on the left at `lg`+ — designed for short
forms like Login, and directly competes for space with this page's own
two-column plan/form split). It gets a minimal shell instead: a slim top
bar (`/favicon.svg` + "ApexBooking" wordmark, linking to `/`) over a
`var(--color-canvas)` background, then the page content below.
`RequestAccessPendingPage` is unaffected — it keeps `AuthLayout` since it's
a short, centered confirmation message that fits that pattern fine.

## 2. Plan Summary Card

New `src/components/requestAccess/PlanSummaryCard.tsx`. Looks up the full
plan object from `PRICING_PLANS` by the `?plan=` query param (same lookup
`RequestAccessPage` already does today) and renders: plan name, description,
and the first 2-3 entries of `plan.features`. A "Change plan" text link
(`<Link to="/#pricing">`) sits in the card header — this is a real
`<a>`-backed navigation (not a JS scroll-only helper) since it's leaving the
page.

- **Mobile (<768px):** compact strip above the form. Collapsed by default —
  shows name, description, "Change plan" link, and a `chevron-down` toggle.
  Tapping the strip (not the "Change plan" link) expands it in place to
  reveal the feature list; the chevron rotates 180°.
- **Desktop (≥768px):** `position: sticky; top: 96px` in a left column
  (`col-md-4`), always expanded (no collapse affordance shown), form fields
  in the right column (`col-md-8`).

If the plan lookup fails (no valid `?plan=`), the existing redirect-to-
pricing `useEffect` in `RequestAccessPage` already handles that case
unchanged — `PlanSummaryCard` can assume a valid plan when it renders.

## 3. Section Structure

`Business Details` and `Owner Details` group headers become a small
component, `src/components/requestAccess/FormSectionHeader.tsx`: icon (16px,
reusing existing `business-profile.svg` / `user-check.svg`) + label text, an
`<hr>` divider directly beneath. Field order is unchanged: Business Details
(Business Name, Business Type, Slug) then Owner Details (First Name, Last
Name, Email).

## 4. Business Type Selector

New `src/components/requestAccess/BusinessTypeSelector.tsx`: a grid of
selectable chip-cards, one per `BUSINESS_TYPE_OPTIONS` entry (unchanged
config — still all 8). Each chip: icon + label, built on the existing `Card`
component. Selected state: `border-color: var(--color-primary)` +
`background: var(--color-primary-soft)`. Keyboard/click behavior: each chip
is a real `<button type="button">` (not a styled `<label>`/hidden radio) so
existing `handleFieldChange('businessType', value)` wiring plugs in
directly — no change to how the value is stored or validated.

Layout: `row row-cols-2 row-cols-md-4 g-2`.

Four new icons needed (matching the existing `viewBox="0 0 24 24" ...
stroke="#475569" stroke-width="1.75"` single-color-stroke style used
everywhere else in `public/assets/icons/`): `spa.svg`, `auto-repair.svg`,
`retail-service.svg`, `other.svg`. The other four
(`barbershop.svg`/`salon.svg`/`clinic.svg`/`fitness.svg`) already exist from
the landing page work and are reused as-is.

## 5. Name Fields

No structural change. The existing `row` / `col-sm-6` pair already stacks
full-width below 576px and sits side-by-side at 576px+, which already
satisfies "stack on mobile, 2-column from tablet up, build mobile-first."

## 6. Inline Validation

- **Slug:** validates on every keystroke (already wired via
  `handleFieldChange`). Non-empty + matching `SLUG_PATTERN` → small green
  `check-circle` icon + "Looks good" fades in under the field (150ms
  opacity). Non-empty + not matching → small red `x-circle` icon + the
  *specific* broken rule, determined by checking sub-conditions in this
  order (the field's `onChange` already lowercases input via the existing
  `e.target.value.toLowerCase()` call, so an uppercase-specific message
  would be unreachable and isn't included): contains a character outside
  `[a-z0-9-]` → "Only lowercase letters, numbers, and hyphens are allowed";
  starts or ends with `-` → "Can't start or end with a hyphen."
  (`SLUG_PATTERN` itself is unchanged — this is a separate presentation-only
  helper that explains *why* it failed.) Empty → existing static helper
  text, unchanged.
- **Email:** unchanged validation timing (on blur) and unchanged error
  copy/placement — only gains the same 150ms opacity fade-in when the error
  first appears.
- **Empty-after-submit-attempt:** once a submit attempt has set `touched`
  for all fields (existing `ALL_FIELDS_TOUCHED` mechanism, unchanged), any
  required field that is still empty additionally gets
  `border-start border-danger border-3` on its input, on top of the
  existing red asterisk and `is-invalid` state. Fields the user has actually
  filled in (even if invalid for another reason, like a malformed email)
  don't get this — it's specifically for "you skipped this."

## 7. Buttons

"Request Access" is unchanged (primary, full-width, existing loading
state). "Back to Login" changes from `Button variant="outline-secondary"`
to `Button variant="link"`, placed on its own line below the primary
button (not side-by-side) — same `to="/find-workspace"` destination,
unchanged.

## 8. Progress Indicator

Included — this page genuinely sits between a plan-selection step (the
landing page's pricing section) and a confirmation step
(`RequestAccessPendingPage`), which is exactly the condition the prompt
gates this on. New `src/components/requestAccess/ProgressTracker.tsx`:
plain text "Plan · Details · Confirm", current step (`Details`) shown in
`var(--color-primary)` with a bottom border; the other two steps in
`var(--color-muted)`. No icons, no numbered circles, no interactivity — the
first and third steps aren't links (Plan is already completed by the time
this page loads; Confirm doesn't exist yet).

## 9. Motion

Reuses the existing landing-page system as-is — no new animation
infrastructure. `Reveal` (`src/components/common/Reveal.tsx`) wraps the
plan summary card and each form section (`FormSectionHeader` + its fields
as one `Reveal` block), staggered via the existing `delayStep` prop.
`Reveal`'s IntersectionObserver-based trigger already fires immediately for
above-the-fold content, giving the "fade-up on mount" effect the prompt
asks for without a separate mechanism. Validation icon/message fade-ins are
a plain CSS `opacity` transition (150ms, no `transform`), so they're
inherently `prefers-reduced-motion`-safe without a media-query override
(nothing to strip).

## Non-Goals

No changes to `IRequestAccessFormValues`, `validate()`'s actual rules,
`requestAccess()` / `authService.ts`, routing paths, or
`RequestAccessPendingPage`'s content or logic. No changes to
`BUSINESS_TYPE_OPTIONS`, `PRICING_PLANS`, or the `BusinessType/
SubscriptionPlanType` enums — this is presentation-layer only.

## File Summary

New files:
- `src/components/requestAccess/PlanSummaryCard.tsx`
- `src/components/requestAccess/FormSectionHeader.tsx`
- `src/components/requestAccess/BusinessTypeSelector.tsx`
- `src/components/requestAccess/ProgressTracker.tsx`
- `src/components/requestAccess/SlugValidationHint.tsx` (the green-check /
  specific-red-rule helper described in §6)
- `public/assets/icons/spa.svg`, `auto-repair.svg`, `retail-service.svg`,
  `other.svg`
- `src/styles/requestAccess.css` (new styles: sticky panel, chip grid,
  collapse/expand, validation fade-ins, progress tracker underline)

Modified files:
- `src/pages/RequestAccessPage.tsx` (layout rewrite; field logic/state
  unchanged)
- `src/main.tsx` (import the new stylesheet)
