# Service Providers Multi-Select — Design Spec

**Date:** 2026-08-08
**Status:** Approved
**Scope:** Presentation layer only. No API contracts, DTOs, or backend behavior change.

## Problem

`ServicesPage` currently exposes staff assignment through a separate "Manage Team" button that opens `ManageServiceStaffModal`, a standalone checklist dialog. This is a second modal, a second entry point, and a second mental model for what is really just another attribute of a service. It also means staff can't be assigned while creating a new service — only after it exists.

## Goal

Fold staff/service-provider assignment into the existing Add/Edit Service modal (`AddServiceModal`) as a single "Service Providers" field: a multi-select combobox with chips and search. Remove the separate Manage Team entry point entirely.

## Component: `MultiSelectCombobox`

New reusable component at `src/components/common/MultiSelectCombobox.tsx`. Built from existing Bootstrap primitives already used in the app (same `dropdown` / `dropdown-menu` pattern as `NotificationBell`), so it inherits existing dropdown styling (shadow, radius, border) from `theme.css` with no new CSS.

**Props:**
```ts
interface IMultiSelectOption {
  value: string
  label: string
  sublabel?: string | null
}

interface IMultiSelectComboboxProps {
  options: IMultiSelectOption[]
  selectedValues: string[]
  onChange: (values: string[]) => void
  placeholder?: string
  searchPlaceholder?: string
  emptyMessage?: string
  isLoading?: boolean
  disabled?: boolean
}
```

**Toggle control:** styled like a `form-control`, `role="button"`, `tabIndex={0}`, `data-bs-toggle="dropdown"`, `data-bs-auto-close="outside"` (so it stays open while checking multiple boxes). Renders:
- Selected items as removable pill chips (`badge rounded-pill`), each with a small `×` (`btn-close`) that calls `stopPropagation()` so removing a chip doesn't toggle the dropdown.
- A muted placeholder when nothing is selected.
- A trailing chevron (reuse `Icon name="chevron-down"`).
- `onKeyDown`: Enter/Space triggers a click (keyboard-accessible open, since this is a `div` not a native `button` — required so real `<button>` chip-remove controls can nest inside without invalid HTML).

**Panel (`dropdown-menu w-100 p-0`):**
- Search input pinned at top (`input-group input-group-sm`, search icon), autofocused via the `shown.bs.dropdown` DOM event, value reset via `hidden.bs.dropdown`. `onKeyDown` suppresses Enter so it can't submit the parent `<form>`.
- Scrollable checklist below (`max-height: 260px; overflow-y: auto`). Each row is a `<label className="dropdown-item">` wrapping a checkbox + `label` (fw-medium) + optional `sublabel` (`text-muted small`) — same visual row `ManageServiceStaffModal` uses today.
- Filtering is case-insensitive against `label` + `sublabel`.
- States: `isLoading` → skeleton rows (`Skeleton`); no options at all → `emptyMessage`; options exist but none match search → "No matches found."

## Integration into `AddServiceModal`

New `FormSection title="Service Providers"` added after "Booking Rules", before the modal's form actions. Description: "Choose which team members can perform this service."

**Data source differs by mode:**
- **Edit mode:** `useServiceStaff(service.id)` — the same hook `ManageServiceStaffModal` already uses. It returns every active team member with an `isAssigned` flag; this becomes both the option list and the initial selection (`staff.filter(s => s.isAssigned).map(s => s.tenantMemberId)`). No API change.
- **Create mode:** no service exists yet, so the per-service staff endpoint can't be called. Uses `useTeamMembers({ pageSize: 200 })` filtered client-side to `status === 'Active'` as the option list; initial selection is empty.

Selection state (`selectedStaffIds: string[]`) resets whenever the modal opens or the underlying staff data for the current service finishes loading (mirrors the existing `values` reset effect keyed on `[isOpen, service]`).

## Save Behavior

The assign/unassign endpoints remain per-member (`assignStaffToService`, `unassignStaffFromService`) — no bulk endpoint exists and none is being added. After the service record itself is created/updated successfully:

- **Create mode:** call `assignStaffToService(newServiceId, id)` for every id in `selectedStaffIds`.
- **Edit mode:** diff `selectedStaffIds` against the initially-loaded assigned set:
  - `added = selectedStaffIds.filter(id => !initialStaffIds.includes(id))` → `assignStaffToService`
  - `removed = initialStaffIds.filter(id => !selectedStaffIds.includes(id))` → `unassignStaffFromService`
  - Run all calls via `Promise.all`.

If the service save succeeds but the staff-sync calls fail, show a distinct toast: "Service saved, but updating service providers failed. Please try again." The modal still closes and `onSaved()` still fires (the service itself did save) — the failure is scoped to the provider assignment only.

## Cleanup

- `ServiceTable.tsx`: remove the "Manage Team" button and the `onManageStaff` prop. Only the Edit action remains.
- `ServicesPage.tsx`: remove `staffModalService` state, the `ManageServiceStaffModal` import/usage, and the `onManageStaff` prop passed to `ServiceTable`.
- Delete `src/components/services/ManageServiceStaffModal.tsx` (fully superseded).
- `useServiceStaff` hook is kept — it's now consumed by `AddServiceModal` in edit mode.

## Out of Scope

- No changes to `serviceService.ts`, `IStaffAssignment`, `IService`, or any backend endpoint.
- No bulk assign/unassign endpoint is introduced; the diff-and-call approach preserves the existing per-member contract.
- No change to how staff are assigned/removed elsewhere in the app (e.g. branch/team pages).
