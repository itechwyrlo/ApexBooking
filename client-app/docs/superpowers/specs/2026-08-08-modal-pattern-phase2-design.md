# Modal Pattern — Phase 2: AddServiceModal Reference Implementation

**Date:** 2026-08-08
**Status:** Approved
**Scope:** Presentation layer only. No API contracts, DTOs, business logic, or validation rule changes.

## Context

Phase 2 of the four-phase dashboard redesign (see [2026-08-08-table-list-pattern-phase1-design.md](./2026-08-08-table-list-pattern-phase1-design.md) for the full breakdown and Phase 1's implementation):

1. Phase 1 (done): Shared table/list pattern, applied to `ServicesPage`/`ServiceTable`.
2. **Phase 2 (this spec):** Shared modal pattern, applied to `AddServiceModal`.
3. Phase 3: Header bar / profile menu (`Topbar.tsx`).
4. Phase 4: Roll out to remaining tables and modals, tenant and superadmin.

## Scope Insight

`Modal.tsx` (`src/components/common/Modal.tsx`) is already the single shared component consumed by every modal in the app: `AddServiceModal`, `BranchModal`, `EditTeamMemberModal`, `AddTeamMemberModal`, `RequestTimeOffModal`, `NewWalkInModal`, `CancelBookingModal`. Unlike Phase 1's per-table migration, changes to `Modal.tsx` and its supporting CSS apply app-wide immediately — there is no separate "Phase 4 rollout" needed for the base shell (spacing, input sizing, open/close transition, scroll-aware header border, focus management).

What stays scoped to individual forms and is *not* automatic:
- The toggle-reveal field pattern — opt-in per field via a new `CollapsibleField` component. Only `AddServiceModal`'s existing "Override minimum advance booking hours" field adopts it in this phase.
- Tabs — `BranchModal` already hand-rolls a `nav nav-tabs` block for its Profile/Hours tabs. This phase extracts that into a shared `ModalTabs` component but does not retrofit `BranchModal` (Phase 4) or add tabs to `AddServiceModal` (not needed).
- Secret-field visibility toggle — built as a new `PasswordInput` component; no dashboard modal currently has a password/API-key field, so nothing consumes it yet.

## `Modal.tsx` Changes

These affect every existing modal in the app immediately.

**Open/close transition.** Today `Modal` does `if (!isOpen) return null` — an instant unmount with no way to animate the close. This changes to local state that keeps the component mounted briefly during close so a reverse transition can play, then unmounts:
- Dialog: `opacity` + `transform: scale(0.96 → 1)`, 200ms ease-out.
- Backdrop: opacity fade over the same duration, in sync with the dialog.
- `prefers-reduced-motion: reduce` skips the transform/opacity animation — the modal snaps open/closed instantly.

**Scroll-aware header border.** The `modal-body` gets an `onScroll` handler that adds a class once `scrollTop > 0`; that class is what applies the border under the header, replacing today's always-on `border-bottom` on `.modal-header`. The footer's existing top border (`.modal-form-actions`) is unchanged in mechanism, but padding is bumped slightly for clearer breathing room from the modal edge and from the body content above (spec's ≥16-24px bar).

**Focus management.** On open, focus moves into the dialog (the first focusable element, or the dialog container itself as a fallback). On close, focus returns to whatever element triggered the open (e.g. the row's Edit button) — neither is handled today.

## Input Sizing

New rule scoped to `.modal-body .form-control, .modal-body .form-select`: explicit `min-height: 40px` and tuned padding/font-size for a consistent ~40-44px dashboard input scale. Scoped to modals only — non-modal forms (auth pages, public booking wizard) keep their own sizing and are untouched.

## `CollapsibleField` Component

New component at `src/components/common/CollapsibleField.tsx`. Uses the CSS-grid `grid-template-rows: 0fr → 1fr` technique (plus opacity) to animate to natural content height without JS measurement — avoids layout thrash and works with dynamic content. Respects `prefers-reduced-motion`.

**Props:** `{ isOpen: boolean; children: ReactNode }`.

**Applied to:** `AddServiceModal`'s "Minimum Advance Booking (hours)" field. Today it stays rendered and merely gets `disabled` when the "Override minimum advance booking hours" toggle is off. It changes to: hidden entirely (zero height, not just grayed) when the toggle is off, and animates open when switched on.

## `ModalTabs` Component

New component at `src/components/common/ModalTabs.tsx`, extracted from `BranchModal`'s existing inline `nav nav-tabs` markup (formalizing an existing pattern, not inventing a new one).

**Props:** `{ tabs: { id: string; label: string }[]; activeTab: string; onChange: (id: string) => void }`. Active tab gets an underline/filled indicator; inactive tabs are muted. Tab content swap fades in over 150ms.

Not wired into `AddServiceModal` (no tabs needed) or `BranchModal` (Phase 4 rollout) in this phase — built and ready for both.

## `PasswordInput` Component

New component at `src/components/common/PasswordInput.tsx`, wrapping the `.password-input-wrap` / `.password-toggle` CSS pattern that already exists in `auth.css` (globally imported, so already available everywhere) and is currently hand-rolled independently on `SuperAdminLoginPage`, `FindWorkspacePage`, and `ResetPasswordPage`. Formalizes it into a reusable component with an eye/eye-slash toggle (existing `Icon` assets) for future secret/API-key fields in dashboard modals. No current consumer — built ahead of need per the spec's explicit "every secret field gets this" rule.

## Focus / Hover Verification

`theme.css`'s existing `:focus-visible` rule (`.btn`, `.form-control`, `.form-select`, `.sidebar-link`, `.tab-link`, `.dropdown-item`, `a`) already covers Phase 1's new components — `RowActions` (built from `.btn`/`.dropdown-item`) and `MultiSelectCombobox` (its toggle carries the `form-control` class). No gaps found; no new global focus rules needed for Phase 2's new components (`CollapsibleField`'s children are ordinary form fields already covered, `ModalTabs`' buttons will use `.tab-link` conventions to inherit the existing rule, `PasswordInput`'s input/toggle button are ordinary `.form-control`/`button` elements).

## Out of Scope (Phase 2)

- Retrofitting `BranchModal`'s tabs to use `ModalTabs`, or its Province→City→Barangay cascading selects to use `CollapsibleField` — Phase 4.
- Wiring `PasswordInput` into any real field — no current consumer exists.
- `Topbar.tsx` / profile menu — Phase 3.
- The other 6 modals (`EditTeamMemberModal`, `AddTeamMemberModal`, `RequestTimeOffModal`, `NewWalkInModal`, `CancelBookingModal`) — they inherit the `Modal.tsx`-level changes automatically but are not otherwise touched.
- No changes to `serviceService.ts`, form validation logic, or any backend endpoint.
