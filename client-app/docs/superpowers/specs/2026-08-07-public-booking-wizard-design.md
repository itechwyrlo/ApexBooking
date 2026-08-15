# Public Booking Wizard — Presentation Refactor

Date: 2026-08-07

## Objective

Refactor the public booking wizard's presentation layer to feel modern, trustworthy, and effortless on mobile, per `Claude/PUBLIC_BOOKING_UI.md` (spec pasted by the user). No booking logic, validation, API contracts, or business rules change. This is styling, layout, and step-presentation only, plus one navigation addition (jump-back-to-edit) that reuses the existing step-history mechanism.

## Current State (baseline)

`PublicBookingPage.tsx` drives a wizard via `usePublicBookingWizard.ts`, stepping through:
`branch → service → staff → schedule → confirm → payment → success`, with `branch`/`staff` auto-skipped when there's only one option. Visuals use a "ticket/stamp" identity (dark header, dashed perforation dividers, mono step counter + single progress bar, stamp badge on success) defined in `src/styles/publicBooking.css`. Data available client-side is limited to what the public API DTOs expose (no business hours, distance, staff rating, service category/icon, or per-date availability) — confirmed by reading `IPublicBranch`, `IPublicService`, `IPublicStaff`, `IPublicSlot` and `publicBookingService.ts`.

## Decisions From Brainstorming

1. **Visual theme: Modern Indigo.** Replace the dark ticket-stub identity with a clean, light SaaS look: white/near-white surfaces, a light-gray header band (not near-black), indigo accent, soft box-shadow cards instead of dashed "perforation" borders and rotated stamp badges. Confirmed against a rendered mockup.
2. **Progress indicator: numbered circle stepper.** Connected circles, one per visible step, filled/checked for completed, filled for current, outlined for upcoming. Desktop shows circles + step name labels underneath. Mobile shows circles only (compact, no labels) plus a text line above ("Step 3 of 6 · Staff") so the current step is still named without crowding.
3. **Customer Information and Review are two distinct steps** (not combined, and not merged into Schedule either). This was reconfirmed explicitly by the user after seeing the theme mockups.
4. **Date + Time stay one "Schedule" step** (not split into two), since slots refresh live under the calendar as the date changes — fewer clicks, no round trip.
5. **Calendar for date selection**: custom month-grid component replacing the native `<input type="date">`. Disables past dates, marks today, highlights the selected date. No per-date availability shading — the API has no endpoint for "which dates have openings," so that stays exactly as it works today (pick a date, then see that date's time chips, including a "no open times" message if empty).
6. **Out of scope** (missing backend data or would require new business logic — explicitly called out to the user and not challenged):
   - Branch business hours, contact number, distance, availability indicator.
   - Service category/icon.
   - Staff rating.
   - "No Preference" staff option (availability endpoint requires a specific `staffId`; there's no "any staff" concept server-side).

## Step List (unchanged order, one step split into two)

1. Branch *(skipped if only one branch)*
2. Service
3. Staff *(skipped if only one staff for the chosen service)*
4. Schedule (date + time together)
5. Customer Information — *new, split out of the old combined "Confirm" step*
6. Review Booking — *new; shows the ticket-style summary + entered contact info, with "Edit" links back to any earlier step*
7. Payment *(only when `result.requiresPayment` is true, same as today)*
8. Booking Confirmation (success)

## Visual System (Modern Indigo)

New CSS custom properties on `.pb-root` in `src/styles/publicBooking.css` (replacing the ticket-themed ones):

```
--pb-bg: #ffffff
--pb-surface: #ffffff
--pb-header-bg: #f7f8fb
--pb-ink: #1c2130
--pb-ink-soft: #6b7186
--pb-accent: #3454d1
--pb-accent-soft: #e8ecfb
--pb-line: #e9ebf1
--pb-danger: #b3261e
--pb-danger-soft: #fbeaea
```

- Header band becomes `--pb-header-bg` (light gray) with dark text — not the near-black band used today.
- Cards (`.pb-option`, the review/success summary card) use `border: 1px solid var(--pb-line)` plus a subtle `box-shadow: 0 1px 3px rgba(20,25,40,.06)` instead of the dashed "perforation" divider and rotated stamp badge.
- Selected/hover/focus-visible states keep the same interaction model as today (border + tint on hover, 2px accent outline on focus-visible, filled tint when `.is-selected`), just recolored to the indigo accent.
- Drop `.pb-perforation`, `.pb-stamp-badge`, and the serif display font pairing; keep one clean sans-serif family throughout (matches "Modern Indigo" mockup). Success step keeps a simple checkmark badge (circle + check icon) instead of the rotated ink-stamp.
- `prefers-reduced-motion` handling for hover/step transitions carries over unchanged.

## Progress Indicator (`BookingProgressSteps.tsx`)

Props change from `{ currentIndex, total }` to `{ currentIndex, total, stepLabel }` (add the current step's display name, e.g. "Staff", "Schedule", "Review"), since the label is now the point of the redesign.

- Renders `total` circles connected by short horizontal bars.
  - Completed steps: filled accent circle with a checkmark.
  - Current step: filled accent circle with its step number.
  - Upcoming steps: outlined circle, muted number.
  - Connecting bars: accent color up through the current step, muted after.
- Desktop (`≥576px`): step name shown under each circle (small caps label), so the whole path is visible at a glance.
- Mobile (`<576px`): circle row only (labels hidden to avoid crowding at 6 steps), with a text line above reading `Step {n} of {total} · {stepLabel}` — same information, different layout.
- `role="progressbar"` / `aria-valuenow` / `aria-valuemin` / `aria-valuemax` carried over from the current implementation.

`PublicBookingLayout.tsx` and `PublicBookingPage.tsx` pass through a human-readable label per step (a small `STEP_LABELS: Record<WizardStep, string>` map colocated with `VISIBLE_STEP_ORDER` in the hook file).

## Wizard State (`usePublicBookingWizard.ts`)

- `WizardStep` gains `'customerInfo'` and `'review'`, drops `'confirm'`.
- `VISIBLE_STEP_ORDER` becomes `['branch', 'service', 'staff', 'schedule', 'customerInfo', 'review']` (payment/success stay outside the "visible progress" set, exactly as `confirm` did today).
- State gains `contact: IBookingContactValues | null` to hold the customer-info form values between steps.
- `selectSlot` now advances to `'customerInfo'` instead of `'confirm'`.
- New action `saveContact(values: IBookingContactValues)`: stores `contact` in state, advances to `'review'`, pushes `'customerInfo'` onto `history`.
- `submit()` keeps its current async logic (calls `initiateBooking`, handles `requiresPayment`, sets `submitError`) but reads contact fields from `state.contact` instead of taking them as a parameter — called from the Review step's "Confirm Booking" button with no arguments needed beyond what's already in state.
- New action `goToStep(target: WizardStep)`: finds `target` in `history`, truncates `history` to everything before it, and sets `step: target`. This powers "Edit" links on the Review step — jumping to an earlier step and re-selecting there behaves exactly like it does today (re-fetches services/staff/slots as needed), so no new business logic is introduced, only a new way to reach an already-visited step.
- `BACK_ELIGIBLE_STEPS` in `PublicBookingPage.tsx` becomes `{'service', 'staff', 'schedule', 'customerInfo', 'review'}`.

## New/Changed Components

- **`BookingCalendar.tsx`** *(new, `src/components/publicBooking/`)* — month-grid date picker. Props: `value: string`, `onChange: (date: string) => void`, `minDate: string`. Prev/next month controls, past dates disabled, today marked, selected date highlighted with accent fill + outline (not color-only — also gets a distinct border weight). Arrow-key navigation between day cells, Enter/Space selects, matches existing focus-visible treatment.
- **`ScheduleStep.tsx`** — swaps the native `<input type="date">` for `<BookingCalendar>`; time-chip grid below is unchanged in behavior, restyled to the new palette.
- **`CustomerInfoStep.tsx`** *(new, replaces the form half of `ConfirmStep.tsx`)* — same fields (first/last name, email, phone, notes) and same validation (`validate()` moves here unchanged), button reads "Continue" and calls `saveContact(values)` instead of submitting the booking.
- **`ReviewStep.tsx`** *(new, replaces the summary half of `ConfirmStep.tsx`)* — ticket-style summary (service, staff, branch, date, time, price) plus the entered contact details, each group with an "Edit" link calling `goToStep(...)` to the relevant step. The Branch and Staff edit links only render when that step wasn't auto-skipped (`!branchStepSkipped`, `!staffStepSkipped`), matching the existing skip-aware back-navigation. Primary button "Confirm Booking" calls `submit()`; `isSubmitting`/`submitError` now live here (moved from the old `ConfirmStep`).
- **`ConfirmStep.tsx`** — deleted, replaced by the two components above.
- **`BranchStep.tsx` / `ServiceStep.tsx` / `StaffStep.tsx`** — no structural change; recolor to the new palette, confirm 44px-minimum touch targets, add `aria-live="polite"` on the loading/empty-state text so screen readers announce when services/staff finish loading.
- **`PaymentStep.tsx` / `SuccessStep.tsx`** — recolor to the new palette; success's rotated stamp badge becomes a plain checkmark badge; add `aria-live="polite"` around the payment-status waiting text.
- **`BookingProgressSteps.tsx`** — rewritten per the Progress Indicator section above.
- **`PublicBookingLayout.tsx`** — header band restyled to `--pb-header-bg`; passes `stepLabel` through to `BookingProgressSteps`.

## Accessibility Notes

- Keyboard: calendar cells and circle-stepper are both reachable and operable via keyboard; existing tab order through option cards/buttons is unchanged.
- Screen readers: `aria-live="polite"` added at the three async loading/empty-state spots (services, staff, slots) and around submit errors, which the current implementation doesn't announce.
- Contrast: indigo accent (#3454d1) on white and white-on-indigo both meet WCAG AA for text/icon use at the sizes used here.
- Selection is never color-only: selected cards/slots also get a border-weight and fill change; the calendar's selected day gets a filled circle, not just a color shift.

## Files Touched

```
src/styles/publicBooking.css                          (rewritten palette + component styles)
src/hooks/usePublicBookingWizard.ts                    (step enum, contact state, saveContact, goToStep, submit signature)
src/layouts/PublicBookingLayout.tsx                    (header restyle, stepLabel passthrough)
src/pages/public/PublicBookingPage.tsx                 (BACK_ELIGIBLE_STEPS, new step branches, stepLabel wiring)
src/components/publicBooking/BookingProgressSteps.tsx  (rewritten: circle stepper)
src/components/publicBooking/BookingCalendar.tsx       (new)
src/components/publicBooking/ScheduleStep.tsx          (use BookingCalendar)
src/components/publicBooking/CustomerInfoStep.tsx      (new, replaces half of ConfirmStep)
src/components/publicBooking/ReviewStep.tsx            (new, replaces half of ConfirmStep)
src/components/publicBooking/ConfirmStep.tsx           (deleted)
src/components/publicBooking/BranchStep.tsx            (recolor + aria-live + touch target check)
src/components/publicBooking/ServiceStep.tsx           (recolor + aria-live + touch target check)
src/components/publicBooking/StaffStep.tsx             (recolor + aria-live + touch target check)
src/components/publicBooking/PaymentStep.tsx           (recolor + aria-live)
src/components/publicBooking/SuccessStep.tsx           (recolor, stamp badge → checkmark badge)
```

## Explicitly Not Changing

- `publicBookingService.ts` and all public API calls/endpoints.
- `IPublicBranch`, `IPublicService`, `IPublicStaff`, `IPublicSlot`, `IBookingContactValues`, `IBookingInitiationResult` — no new fields.
- Validation rules in `CustomerInfoStep` (moved verbatim from `ConfirmStep`).
- Booking submission logic, payment-status polling (`useBookingPaymentStatus`), skip-logic for single branch/staff.
