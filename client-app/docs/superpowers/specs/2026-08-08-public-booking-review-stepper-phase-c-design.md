# Public Booking Wizard — Phase C: Your Info, Review, Stepper Polish, Edit-Navigation Fix

**Date:** 2026-08-08
**Status:** Approved
**Scope:** Presentation layer + one contained wizard-state addition (`editReturnTarget`). No API contracts, submission flow, or validation rule changes.

## Context

Third of four phases refactoring the customer-facing public booking wizard. See [Phase A](./2026-08-08-public-booking-cards-phase-a-design.md) (foundation + selection cards) and the Phase B fix (Schedule step segmented time tabs, applied directly per user report rather than as its own spec). Phase D (confirmation rebuild + desktop summary panel) follows this one.

## Findings

- `CustomerInfoStep`'s validation is already exactly what the source prompt asked for: inline, on-blur, matching the auth-page pattern. No change needed there.
- `ReviewStep`'s summary card structure is unchanged per the prompt's own instruction ("it is functionally solid").
- Tracing `selectService` → `selectStaff` → `selectSlot` → `saveContact` in `usePublicBookingWizard.ts`: there is exactly one choke point that transitions into `customerInfo` — `selectSlot`. Every upstream action already correctly forces a re-pick of anything that might be stale (a new service re-fetches staff and re-forces a schedule pick if the auto-skip no longer applies; a new staff member re-forces a schedule pick). This means the "return straight to Review after an edit" fix does not need new invalidation logic — it needs one conditional at the single existing choke point.
- `BookingProgressSteps` has three real gaps: `.pb-stepper-bar` has no transition (instant color swap on completion), completed step circles are plain `<span>`s (not clickable despite looking like they should be), and `.is-done`/`.is-current` render identically apart from checkmark-vs-number.

## Changes

### 1. Your Info — input sizing + focus ring only

`.pb-root` has no `.form-control`/`.form-select` override today, so inputs use raw Bootstrap defaults, including Bootstrap's default blue focus glow — inconsistent with every other interactive element on this page (`.pb-option`, `.pb-slot`, `.pb-calendar-day` all use `:focus-visible` + a `var(--pb-accent)` outline). Adds:
```css
.pb-root .form-control,
.pb-root .form-select {
  min-height: 42px;
  padding: 0.55rem 0.75rem;
  font-size: 0.9375rem;
  border-color: var(--pb-line);
}
.pb-root .form-control:focus,
.pb-root .form-select:focus {
  border-color: var(--pb-accent);
  box-shadow: 0 0 0 0.2rem var(--pb-accent-soft);
}
```

### 2. Review — no structural change

Only its `onEdit` prop wiring changes (see below). Everything else stays as-is.

### 3. Edit-navigation fix

New wizard state field: `editReturnTarget: WizardStep | null` (initial `null`).

New action `editFromReview(target: WizardStep)` — used only by `ReviewStep`'s Edit links (`PublicBookingPage` wires `onEdit={wizard.editFromReview}` instead of `wizard.goToStep`). Behaves like `goToStep` (truncates history, jumps to `target`, `direction: 'backward'`) but also sets `editReturnTarget: 'review'`.

`selectSlot` — the sole choke point into `customerInfo` — gains one check: if `editReturnTarget === 'review'` (contact is guaranteed non-null at this point, since Review can't have rendered without it), it transitions to `review` directly instead of `customerInfo`, and clears `editReturnTarget`. Every other transition (`selectBranch`, `selectService`, `selectStaff`, `saveContact`, `goBack`) is unchanged. `saveContact` also clears `editReturnTarget` defensively, since reaching review through the normal path means any pending edit session is resolved.

`goToStep` itself is unchanged and gains a second caller (see stepper below) — it remains a plain jump with no review-return behavior, since that behavior is specific to editing *from* Review, not general backward navigation.

### 4. Stepper polish (`BookingProgressSteps.tsx` + CSS)

- `.pb-stepper-bar` gains `transition: background-color 0.25s ease` so the connecting line fades in rather than swapping instantly.
- Completed circles (`isDone`) render as real `<button>` elements calling a new `onStepClick(index)` prop — threaded from `PublicBookingPage` (maps index → `wizard.visibleSteps[index]` → `wizard.goToStep`) through `PublicBookingLayout` → `BookingProgressSteps`. Gets a hover scale + soft glow and a `:focus-visible` outline, consistent with the rest of the page's interactive elements.
- `.is-current` gets a distinct ring (`box-shadow` halo in `--pb-accent-soft`) with a subtle pulse animation, separating it visually from `.is-done` beyond just the checkmark-vs-number difference. `prefers-reduced-motion` keeps the static ring but drops the pulse and the hover scale transform.

## Out of Scope (Phase C)

- Confirmation page rebuild, Add-to-Calendar, Get Directions, desktop summary panel — Phase D.
- `PaymentStep` — untouched, as in every prior phase.
- No changes to `initiateBooking`, `publicBookingService.ts`, or any `IPublic*` DTO shape (the new `editReturnTarget` field lives entirely in frontend wizard state, never sent to the backend).
