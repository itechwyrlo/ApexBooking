# Header Bar / Profile Menu — Phase 3

**Date:** 2026-08-08
**Status:** Approved
**Scope:** Presentation layer only. No auth flow, routing, or logout behavior changes.

## Context

Phase 3 of the four-phase dashboard redesign (see [Phase 1](./2026-08-08-table-list-pattern-phase1-design.md) and [Phase 2](./2026-08-08-modal-pattern-phase2-design.md)). Targets `src/components/layout/Topbar.tsx`, which is already shared across every authenticated dashboard page.

## Findings

Most of the originally-requested dropdown treatment already exists: `.dropdown-menu` already gets `shadow`, app-wide `--radius-md` corner rounding (via `theme.css`), and a divider between the identity block (email + role) and the actions (Settings, Sign Out). No changes needed there.

**Blocker resolved with the user:** the spec asked the collapsed profile control to show "avatar + first name," but `IUser` (decoded from the JWT) only carries `email`, `roles`, `slug`, `tenantId`, `id` — no name field exists anywhere in the app, and adding one would mean touching the auth token contract, which is out of bounds. Decision: **avatar + chevron only, at every width, no text in the collapsed state.** Full email + role stay inside the dropdown panel (already the case).

## Changes

1. **"View Public Booking Page"** becomes an icon-only external link (`Icon name="globe"`) with `title` + `aria-label="View public booking page"`. Currently hidden below the `sm` breakpoint (`d-none d-sm-inline-flex`) — since it's compact now, it shows at every width instead.
2. **Profile control** drops the `<span>` showing `user?.email` from the collapsed header state entirely (previously visible from `sm` up). Since that text was the button's only accessible name, `aria-label="Account menu"` is added to keep it properly labeled for screen readers once the visible text is gone.

No other header utility buttons need conversion — the sidebar menu toggle, sidebar-collapse toggle, and `NotificationBell` are already icon-only.

## Out of Scope

- Sidebar collapse/expand behavior (explicitly untouched per user instruction from the start of this work).
- Notification dropdown internals (already icon-only, already has elevation/radius).
- Any change to `useAuth`, `IUser`, or the JWT/auth contract.
