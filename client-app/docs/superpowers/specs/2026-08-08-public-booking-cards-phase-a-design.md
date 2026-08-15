# Public Booking Wizard — Phase A: Foundation + Selection Cards

**Date:** 2026-08-08
**Status:** Approved
**Scope:** Presentation layer only. No changes to booking logic, availability data, submission flow, or API contracts.

## Context

First of four phases refactoring the customer-facing public booking wizard (separate initiative from the four-phase dashboard redesign):

1. **Phase A (this spec):** Foundation (tokens, transitions) + selection cards (Branch, Service, Staff steps).
2. Phase B: Schedule step (calendar + time-slot segmented tabs).
3. Phase C: Your Info, Review, stepper polish, and the edit-navigation fix (editing from Review should return straight to Review).
4. Phase D: Confirmation page rebuild (checkmark animation, Add-to-Calendar, Get Directions) + desktop summary panel.

## Findings

The existing wizard (`src/components/publicBooking/`, `src/styles/publicBooking.css`, `src/hooks/usePublicBookingWizard.ts`) is considerably more built-out than a typical redesign starting point:

- The forward-compatible color token system already exists: `--pb-accent`, `--pb-accent-soft`, `--pb-accent-hover`, etc., scoped under `.pb-root`. No new token work needed.
- Staff photos already work end-to-end (`IPublicStaff.photoUrl`, with an initials-avatar fallback in `StaffStep`) — not hypothetical, already live.
- `usePublicBookingWizard` already computes `visibleSteps` dynamically (respecting `branchStepSkipped`/`staffStepSkipped`), and the stepper already renders from that list, not a hardcoded array.
- A step-enter transition already exists (`.pb-step-enter`, fade + `translateY(6px)`, reduced-motion aware).

**Blocker resolved with the user:** the source prompt asked for category filter chips (e.g. Hair, Beard, Combos) once a location has 8-10+ services. No category concept exists anywhere in the app — `IPublicService` has only `serviceId, name, description, durationMinutes, price, currencyCode`, and grepping the whole frontend for "category" returns nothing. Adding one requires a new data model field end-to-end (service form + public DTO), which is out of bounds for a presentation-only refactor. **Decision: skip category filtering entirely for now.** If a location ever needs it, that's a separate future project once a category field exists.

## Changes

### 1. Selection card treatment (`.pb-option`, shared by `BranchStep`, `ServiceStep`, `StaffStep`)

All three steps already render through the same `.pb-option` class, so this is one CSS change applied everywhere at once:
- Left-border accent: transparent by default, `2-3px solid var(--pb-accent)` on hover, stays filled when `.is-selected`.
- Hover lift: `transform: translateY(-2px)` combined with the existing shadow increase.
- Selected state gets a small checkmark badge in the card's top-right corner, in addition to the existing background tint/border-color change — selection state no longer relies on tint alone.
- `prefers-reduced-motion` drops the lift transform, keeps the border/checkmark state changes (which aren't motion).

### 2. Image/icon slot for Service and Branch cards

`IPublicService` and `IPublicBranch` carry no photo field today (only `IPublicStaff` does). `ServiceStep` and `BranchStep` get a reserved leading icon container — mirroring the circular slot `StaffStep` already uses for its avatar/initials — filled with a branded placeholder icon until a real image field is added. Purely structural; no data source changes.

### 3. Hero/header banner slot

`PublicBookingLayout`'s header band (`src/layouts/PublicBookingLayout.tsx`) is currently text-only (brand name + step progress) against the flat `--pb-header-bg` color. It gets an optional banner-image container that defaults to today's solid-color treatment when no image exists — same reserve-the-slot approach as the cards, ready for a future upload without another layout change.

### 4. Direction-aware step transition

`.pb-step-enter` upgrades from fade + `translateY(6px)` to fade + `translateX(10-15px)`: slides in from the right when the wizard advances, from the left when going back. Requires a small addition to `usePublicBookingWizard` — a `direction: 'forward' | 'backward'` value set by the forward-moving actions (`selectBranch`, `selectService`, `selectStaff`, `selectSlot`, `saveContact`, `submit`) versus the backward ones (`goBack`, `goToStep`) — the hook doesn't track this today. `prefers-reduced-motion` drops the slide and keeps only the fade, consistent with every other reduced-motion rule already in this app.

## Out of Scope (Phase A)

- Category filter chips (see Blocker above — skipped entirely).
- Schedule step — Phase B.
- Review step, wizard stepper visual polish, and the edit-from-Review navigation fix — Phase C.
- Confirmation page rebuild, Add-to-Calendar, Get Directions, desktop summary panel — Phase D.
- `PaymentStep` — not mentioned by the source prompt; left untouched throughout all four phases unless the user asks otherwise.
- No changes to `publicBookingService.ts`, any `IPublic*` interface's data fields, or backend endpoints.
