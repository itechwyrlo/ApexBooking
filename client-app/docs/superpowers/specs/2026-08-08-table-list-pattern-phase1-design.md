# Table/List Pattern — Phase 1: Services Reference Implementation

**Date:** 2026-08-08
**Status:** Approved
**Scope:** Presentation layer only. No API contracts, DTOs, business logic, or routing behavior changes.

## Context

This is Phase 1 of a four-phase dashboard redesign (see conversation history for the full breakdown):

1. **Phase 1 (this spec):** Shared table/list pattern, built and applied against `ServicesPage`/`ServiceTable` as the reference implementation.
2. **Phase 2:** Shared modal pattern, built and applied against `AddServiceModal`.
3. **Phase 3:** Header bar / profile menu (`Topbar.tsx`).
4. **Phase 4:** Roll Phase 1-3 patterns out to the remaining 7 table components (`TeamMemberTable`, `BranchTable`, `BookingTable`, `TimeOffTable`, `CustomerTable`, `FailedOutboxMessageTable`, `TenantRequestTable`) and remaining modals, across both tenant and superadmin pages.

Each phase gets its own design → implementation cycle. This document covers Phase 1 only.

## Architecture Decision

**Hybrid, not a single generic `DataTable` engine.** `BookingTable` and `FailedOutboxMessageTable` were inspected alongside `ServiceTable` — they have genuinely different shapes (8 columns with conditional status-based action sets vs. a flat error log with a single action). Forcing all 8 tables into one config-driven component now would be a premature abstraction.

Instead, Phase 1 builds two shared primitives that every table's own markup will consume:

- **`RowActions`** — encodes the inline-vs-3-dot-menu rule.
- **`MobileListCard`** — the structured mobile card shell.

Each table keeps writing its own `<table>` (columns, data mapping), but the parts required to be identical everywhere — actions pattern, card shell, icon choices, spacing tokens — are enforced by these shared components rather than copy-pasted per table. Phase 4 becomes "wire each table into these primitives," not "redesign each table."

**Mobile card requires new markup, not more CSS.** The current `.table-stack` mechanism (`theme.css`) flattens every `<td>` into a stacked block via a `::before` label — it cannot produce "badge top-right of name, 3 stats in one compact row" through CSS alone. Each table will render two branches: `<table className="d-none d-md-table">` for desktop, and `<div className="d-md-none">` containing a list of `MobileListCard` for mobile. Only `ServiceTable` changes in Phase 1; the other 7 tables keep their existing `.table-stack` behavior untouched until Phase 4 migrates them individually.

## Desktop Table Treatment

- `<thead>` background tinted with `var(--color-canvas)`.
- Header row height increased vs. data rows; uppercase labels retained; add `letter-spacing: 0.04em` (matching the value already used for mobile data-labels in `theme.css`).
- Numeric columns (Duration, Price, Buffer) right-aligned (`text-end`); text columns left-aligned.
- Data rows: consistent `py-3` cell padding, bottom border between rows, hover background using `var(--color-canvas)`.
- Primary cell (service name: bold name + muted smaller description below) is unchanged — already matches the target pattern.

## `RowActions` Component

New shared component, likely `src/components/common/RowActions.tsx`.

**Props:** a list of actions, each `{ label: string; icon: string; onClick: () => void; destructive?: boolean; disabled?: boolean }`.

**Behavior:**
- If there are ≤2 actions and none are `destructive`: render each inline as an icon-only `Button` (existing `iconOnly` + native `title` tooltip pattern, same as today's Edit button).
- If there are ≥3 actions, or any action is `destructive`: render a single icon-only `Button` with the `more-horizontal` icon that opens a Bootstrap dropdown-menu (same `data-bs-toggle="dropdown"` pattern as `NotificationBell`), listing every action as a `dropdown-item` with icon + label. Destructive actions get a `dropdown-divider` above them and `text-danger` styling, and are never rendered inline regardless of total action count.

Services currently has one action (Edit), so `ServiceTable` renders it inline via `RowActions` — visually unchanged today, but any action added later (to Services or any other table) automatically gets correct treatment.

## `MobileListCard` Component

New shared component, likely `src/components/common/MobileListCard.tsx`.

**Props:** `title`, `subtitle?`, `badge?` (rendered top-right, next to title), `stats?: { label: string; value: string }[]` (rendered as up to 3 compact columns — small label above value), `actions: ReactNode` (a `RowActions` instance, right-aligned).

**Container styling:** padding, `var(--radius-md)` corners, `1px solid var(--color-border)`, `var(--shadow-sm)` — reads as a distinct card object, not a bare stacked block.

Applied to Services: `title` = service name, `subtitle` = description, `badge` = Active/Inactive status, `stats` = [Duration, Price, Buffer], `actions` = the same `RowActions` instance used on desktop.

## Pagination Rework (`src/components/common/Pagination.tsx`)

- Prev/Next become pure icon-only chevron buttons at all breakpoints (drop the current `d-none d-sm-inline` "Previous"/"Next" text) — tooltip/label carried by the existing `title`/`aria-label` attributes.
- Current-page pill: `.page-item.active .page-link` gets `border-radius: 999px` (today's `--bs-pagination-border-radius: var(--radius-sm)` is not a full pill). Other page numbers remain plain muted text buttons. Existing ellipsis-collapsing logic (`buildPageList`) is unchanged.
- **Bug fix:** on mobile, the current implementation shows both the active-page pill (only non-current pages get `d-none d-sm-block`) *and* the "current / total" text simultaneously. Phase 1 fixes this so mobile shows only Prev / "X of Y" / Next — no numbered buttons at all — with ≥44px tap targets, full-width row.
- Page-change transition: the table/card content area fades out and back in over 150-200ms when `pageNumber` changes, skipped under `prefers-reduced-motion` (mirroring the existing `.skeleton` reduced-motion media query in `theme.css`).

## Primary Action Button (Mobile)

**Decision:** full-width "Add Service" button under the page header on mobile, not a floating action button. Rationale: no existing bottom-anchored UI (no bottom nav, no fixed PWA install banner) to route around, avoids introducing new z-index/safe-area handling, and stays maximally unambiguous per the spec's own goal for this button. This becomes the standard for every list page's primary action in Phase 4.

## Motion

- Row hover: CSS `background-color` transition, 120-150ms.
- 3-dot dropdown open: Bootstrap's default dropdown fade, confirmed/tuned to 120-150ms if `theme.css` needs an explicit override.
- Page-change fade: 150-200ms (see Pagination section).
- All of the above respect `prefers-reduced-motion: reduce`.

## Out of Scope (Phase 1)

- `Topbar.tsx` / profile menu / "View Public Booking Page" button — Phase 3. Services page has no such button.
- Breadcrumbs — `Breadcrumb.tsx` already exists and matches the target spec; no page currently needs it since all edits happen in modals, not routed sub-pages.
- Extended-FAB secondary-action pattern — Services has no secondary text-button action to convert; identified case-by-case in Phase 4.
- Modal styling — Phase 2.
- The other 7 table components and their pages (tenant and superadmin) — Phase 4.
- No changes to `serviceService.ts`, `useServices`, `IService`, or any backend endpoint.
