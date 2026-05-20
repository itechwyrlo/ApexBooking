# UI Redesign Tracker — ApexBooking Client

---

## Source of Truth

All UI must match the design defined in the single-file HTML prototype.
The prototype lives outside the React project and was provided directly by the user.
Key design classes extracted from it:

- `.apex-table`, `.apex-table th`, `.apex-table td`, `.apex-table tr:last-child td`, `.apex-table tbody tr:hover`
- `.apex-row-clickable`, `.apex-ref`
- `.apex-pagination`, `.apex-pagination-info`
- `.apex-pill` and its variants (already correct before this work began)

---

## Completed Work

### Phase 1 — Table and Pagination Shared Components

**Status: Done**

#### New files created
- `src/components/ui/table/table.styles.css`
  - `.apex-table` — replaces Bootstrap `table table-hover align-middle`
  - `.apex-table th` — 10px uppercase, gray-500, gray-100 background, bottom border
  - `.apex-table td` — 10px padding, gray-800, vertically centered, bottom border
  - `.apex-table tr:last-child td` — removes bottom border on last row
  - `.apex-table tbody tr:hover` — gray-100 hover
  - `.apex-row-clickable` — cursor pointer for clickable rows
  - `.apex-ref` — monospace, 11px, bold, primary color for reference columns
  - `.apex-table-desktop` / `.apex-table-mobile` — responsive toggle via media queries (breakpoint: 767.98px)
  - `.apex-booking-card`, `.apex-booking-card-row`, `.apex-booking-card-label`, `.apex-booking-card-value` — mobile card styles

- `src/components/ui/pagination/Pagination.styles.css`
  - `.apex-pagination` — flex row, space-between, border-top, padding-top
  - `.apex-pagination-info` — 12px, gray-500
  - Chevron icon sizing (10px) via class selector

#### Modified — shared components
- `src/components/ui/table/table.tsx`
  - `table.table-hover.align-middle` → `apex-table`
  - `thead.table-light` → `thead` (styled via CSS)
  - `th.border-0.text-secondary.fw-semibold` → `th` (styled via CSS)
  - `cursor-pointer` → `apex-row-clickable`
  - Added `renderCellContent()` helper to avoid duplicating cell logic across both views
  - Desktop table wrapped in `div.apex-table-desktop` (hidden below 768px)
  - Added `div.apex-table-mobile` card list (hidden above 768px) — renders each row as `.apex-booking-card` with label/value pairs per column, no header; selection columns skipped; action button columns rendered without a label

- `src/components/ui/pagination/Pagination.tsx`
  - Removed `onPageSizeChange` prop and page size dropdown entirely
  - Info text changed from `"Showing page X of Y • Z total"` to `"Showing X–Y of Z"`
  - Numbered page buttons using Bootstrap `pagination pagination-sm`
  - Prev/Next use `fas fa-chevron-left/right` (FontAwesome, already loaded globally)
  - Windowed page number logic: shows all pages if ≤ 7, otherwise first/ellipsis/window/ellipsis/last

#### Modified — index.css
- Added imports for both new stylesheets under the component imports block:
  - `@import './components/ui/table/table.styles.css';`
  - `@import './components/ui/pagination/Pagination.styles.css';`

#### Modified — consuming pages (prop cleanup only, no logic change)
- `src/features/staff/pages/StaffPage.tsx`
  - `pageSize` state + `handlePageSizeChange` removed
  - Module-level `const PAGE_SIZE = 10` added
  - All `pageSize` references updated to `PAGE_SIZE`
  - `useEffect` dependency array cleaned (`pageSize` removed)
  - `onPageSizeChange` prop removed from `<Pagination>`

- `src/features/service/pages/ServicesPage.tsx`
  - Same treatment as StaffPage

- `src/features/bookings/pages/BookingsPage.tsx`
  - `onPageSizeChange={() => {}}` no-op removed from `<Pagination>`

---

## Pending Work

### Phase 2 — Superadmin Pages: Migrate to Shared Table Component

**Status: Not started**

**Scope:** Two superadmin pages bypass the shared `Table` component entirely.
They use raw HTML `<table class="table table-hover align-middle">` markup inline,
which means they do not pick up the `.apex-table` redesign automatically.

#### Files to migrate

**`src/features/superadmin/pages/SuperAdminOverviewPage.tsx`**
- Contains a raw `<div class="table-responsive"><table class="table table-hover align-middle">` block
- Renders a list of organizations: Organization (name + email), Booking URL, Users, Status, Actions
- Status badge uses inline `statusBadge()` helper with Bootstrap `.badge` classes — keep as-is (not a BookingStatus, different domain)
- Avatar initials logic (`orgInitials`, `avatarColor`) stays in the page, passed via column `render`
- Does not use `Pagination` — data is loaded all at once from `overview.organizations[]`
- Migration: replace raw table with `<Table data={...} columns={...} getRowId={...} />`

**`src/features/superadmin/pages/TenantRequestsPage.tsx`**
- Contains a raw `<div class="table-responsive"><table class="table table-hover align-middle">` block
- Renders access requests: Business (name + email), Plan, Status, Submitted date
- Row click selects a request and opens a detail panel — use `onRowClick` prop on `<Table>`
- Selected row highlight (`.table-active`) — currently done via `className` on `<tr>`; after migration this becomes row-click state only (detail panel opening handles the visual feedback)
- Does not use `Pagination` — all requests are loaded at once per tab filter
- Migration: replace raw table with `<Table data={...} columns={...} getRowId={...} onRowClick={...} />`

#### Rules to follow during Phase 2
- Do not change hook logic (`useSuperAdminOrganizations`, `useTenantRequests`)
- Do not change DTO structure
- Do not add API calls
- Columns must use existing `render` functions — no new helpers unless purely presentational
- Status badges in superadmin pages use Bootstrap `.badge` directly (not `StatusPill`) — this is intentional, different status domain
- After migration, verify both pages compile and render correctly

---

## Architecture Reference

- Shared UI components: `src/components/ui/`
- Styles registered globally via: `src/index.css` (all component CSS goes through `@import` here)
- Table component: `src/components/ui/table/table.tsx` — generic, accepts `Column<T>[]`
- Pagination component: `src/components/ui/pagination/Pagination.tsx` — accepts `currentPage`, `totalPages`, `pageSize`, `totalItems`, `onPageChange`
- Column types: `DataColumn<T>`, `ActionColumn<T>`, `SelectionColumn` — defined in `src/components/ui/table/types.ts`
- Design system rules: `ui_design_system.md`, `ui_kit_rules.md`, `ui_redesign_skill.md`
