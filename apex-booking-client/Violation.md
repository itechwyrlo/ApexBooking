# ApexBooking Client — Violation & Redesign Tracker

Work log for resolving all architecture violations and completing the UI redesign. Address one task at a time: present problem → define plan → get approval → implement → verify build → proceed.

---

## Legend

- `[DONE]` — Resolved and verified
- `[NEXT]` — Current task, approved and ready to implement
- `[PENDING]` — Identified, not yet started
- `[BLOCKED]` — Cannot proceed — external dependency listed

---

## Execution Order

```
P-0.1 [DONE]     Dashboard hook + endpoint
P-0.2 [DONE]     useSlots consolidation
P-0.3 [DONE]     Remove X-Tenant from useBookings
P-0.4 [DONE]     Fix VerifyEmailPageComponent
P-0.5 [DONE]     Delete dead code
──────────────────────────────
P-1.1 [DONE]     dashboard/ folder structure
P-1.2 [DONE]     resources/ → staff/ rename (full feature rename + dead file cleanup + header fix)
P-1.3 [DONE]     service/ missing components/
P-1.4 [DONE]     public/ missing components/ + types/
──────────────────────────────
P-2.1 [DONE]     renderField — extract data fetching
P-2.2 [DONE]     PhoneField — extract parsing logic
──────────────────────────────
P-3A  [DONE]     UI — Public zone (landingpage, pricing, request)
P-3B  [DONE]     UI — Auth zone (login, forgot, reset, verify)
P-3C  [DONE]     UI — Tenant Admin zone (dashboard, resources, services, bookings, settings)
P-3D  [DONE]     UI — Customer Booking zone (wizard, register, payment, cancel)
P-3E  [DONE]     UI — Super Admin zone (overview, organizations, requests, gateway)
──────────────────────────────
P-4   [DONE]     Modal system cleanup (FormModal, ConfirmModal, renderField styles)
P-5   [DONE]     Layout cleanup (SuperAdminLayout, Header)
──────────────────────────────
A-1   [DONE]     BaseResponse: remove type definitions + fix axiosInstance
A-2   [DONE]     BaseResponse: fix auth hooks
A-3   [DONE]     BaseResponse: fix booking hooks + useSlots URL + useBookings dead endpoint
A-4   [DONE]     BaseResponse: fix public hook
A-5   [DONE]     BaseResponse: fix service hook + remove X-Tenant + remove dead commented code
A-6   [DONE]     BaseResponse: fix settings hooks
A-7   [DONE]     BaseResponse: fix staff hooks
A-8   [DONE]     BaseResponse: fix superadmin hooks
──────────────────────────────
B-1   [DONE]     useCancellationToken — BaseResponse removal + NoContent handling (URLs confirmed correct)
B-2   [DONE]     BookingsPage — use Table reusable component
──────────────────────────────
C-1   [DONE]     useBookings.getAll — untyped axiosInstance.get (missing PagedResult<TenantBookingsDto>)
C-2   [DONE]     useStaff.getAll — untyped axiosInstance.get (missing PagedResult<StaffDto>)
C-3   [DONE]     useStaffAvailability.getSchedule — untyped axiosInstance.get (missing StaffAvailabilityDto)
C-4   [DONE]     useStaffAvailability.getExceptions — untyped axiosInstance.get (missing PagedResult<AvailabilityException>)
──────────────────────────────
D-1   [DONE]     Booking type ≠ TenantBookingsDto — critical field mismatch (bookingId, guest, scheduledDate)
D-2   [DONE]     AvailabilityException type has extra fields not in StaffAvailabilityExceptionDto
D-3   [DONE]     AvailableSlotsResponse.resourceId ≠ backend AvailableSlotsDto.staffId
──────────────────────────────
E-1   [DONE]     useInitiatePayment — InitiatePaymentResult inline in hook, belongs in bookings/types/
E-2   [DONE]     useMonthlyAvailability — DayAvailabilityDto + MonthlyAvailabilityDto inline in hook
E-3   [DONE]     useSuperAdminLogin — SuperAdminLoginResponseData inline in hook, use AuthResponseData
──────────────────────────────
F-1   [DONE]     useResetPassword — backend returns 204 NoContent, hook destructures result.accessToken (runtime crash)
```

---

## Backend Contract Reference

> Verified facts from backend source code investigation. Do not re-investigate unless code changes.

### axiosInstance behavior (verified from src/services/axiosInstance.ts)
- Response interceptor: `(response) => response.data` — the Axios envelope is unwrapped. `axiosInstance.get<T>()` returns `T` directly, NOT `AxiosResponse<T>`. So `result` IS the response body. Never access `result.data` expecting the response body — `result` already IS the body.
- Error interceptor: rejects with `ApiError { message: string; status?: number; data?: any }`. In catch blocks, use `err.message` — never `err.response?.data?.errors?.[0]?.message`.
- Request interceptor: injects `Authorization: Bearer {token}` from `sessionStorage.getItem('access_token')` automatically. Never add `Authorization` manually.
- `X-Tenant` header: backend ignores it entirely. Never add it.

### Tenant identification
- Backend reads tenant from JWT claim `"tenant_id"` only — via `TenantMiddleware.cs` line 27
- No HTTP header is used. `X-Tenant`, `X-Tenant-Id` are ignored by the backend entirely
- `axiosInstance.ts` already injects `Authorization: Bearer {token}` via request interceptor — no manual header injection needed anywhere

### Slot availability endpoint
- Correct: `GET /api/public/services/{serviceId}/slots`
- Query params: `resourceId` (Guid, optional), `date` (DateOnly), `slug` (string, required)
- Source: `PublicController.cs` line 56

### Verify account endpoint
- Correct endpoint: `GET /api/auth/verify-account` (not `verify-email`)
- Returns: `AccountVerificationResponseDto { url: string; tenantSlug?: string | null }`
- Source: `AuthController.cs` line 31 + `AccountVerificationResponseDto.cs`
- Frontend accesses `data.url` → correct field name under camelCase serialization

### Dashboard endpoint
- `GET /api/dashboard/summary` — **DOES NOT EXIST**
- No `DashboardController` exists anywhere in the backend
- `DashboardStats` interface in `TenantDashboardPage.tsx` is entirely invented — no backend source
- **Backend must create this endpoint before P-0.1 can be implemented**

### Full backend endpoint surface (all controllers)
| Controller | Routes |
|---|---|
| `api/auth` | GET verify-account, POST login, POST forgot-password, POST reset-password, POST refresh, POST logout, POST accept-invitation, POST login/superadmin |
| `api/booking` | GET (list), GET `{id}`, POST, POST `{id}/cancel`, POST `{id}/payment` |
| `api/service` | GET (list), GET `{id}`, POST |
| `api/staff` | GET (list), GET `{staffId}`, POST, PATCH `{staffId}`, PATCH `{staffId}/status`, GET `{staffId}/exceptions`, GET `{staffId}/availability`, POST `{staffId}/exceptions`, DELETE `{staffId}/exceptions/{exceptionId}`, PUT `{staffId}/availability` |
| `api/public` | GET `{slug}`, GET `{slug}/services`, GET `{slug}/services/{id}/resources`, GET `services/{id}/slots`, GET `{slug}/services/{id}/monthly-availability`, GET `{slug}/bookings/{id}`, GET/POST cancellation, POST tenant-requests |
| `api/tenant` | GET profile/{slug}, PUT profile/{slug} |
| `api/setting` | GET tenant, GET payment-gateway, GET payment-policy |
| `api/superadmin` | GET overview, GET/POST organizations, GET organizations/{slug}, POST organizations/{slug}/users, POST organizations/{slug}/users/assign, POST organizations/{slug}/users/{id}/resend-invite, GET/POST payment-gateway, GET tenant-requests, GET tenant-requests/{id}, POST tenant-requests/{id}/approve, POST tenant-requests/{id}/reject |
| `health` | GET |

### Decisions made
- `schema/` is a recognized optional feature subfolder — defined in `CLAUDE.md`. Contains `ModelSchema[]` form config only. `types/` holds backend DTO mirrors only. (Decision: Option 1, approved by user, CLAUDE.md already updated.)

---

## BLOCKED

---

### P-0.1 — Dashboard: backend endpoint + frontend hook

**Violations resolved:** V-13 (API call in component), V-12 partial (manual headers in dashboard)

**Status:** Plan approved. Ready to implement. Backend first, then frontend.

---

**BACKEND — 5 files to create** (in `c:\Users\Wyrlo\Projects\ApexBooking`):

| File | Purpose |
|---|---|
| `ApexBooking.Core.Application/Dtos/DashboardSummaryDto.cs` | Main DTO + nested records |
| `ApexBooking.Core.Application/Features/Dashboard/Queries/GetDashboardSummary/GetDashboardSummaryQuery.cs` | Query record |
| `ApexBooking.Core.Application/Features/Dashboard/Queries/GetDashboardSummary/GetDashboardSummaryQueryHandler.cs` | Handler |
| `ApexBooking.Core.Application/mapper/DashboardMappings.cs` | Static mapper extension method |
| `ApexBooking.WebApi/Controllers/DashboardController.cs` | Thin controller |

**Approved DTO shape:**
```csharp
public sealed record DashboardSummaryDto(
    int TodayBookingCount,
    int PendingConfirmationCount,
    decimal RevenueToday,
    int TotalBookingCount,
    IReadOnlyList<DailyRevenueDto> WeeklyRevenue,
    IReadOnlyList<ServiceBreakdownDto> ServiceBreakdown,
    IReadOnlyList<ScheduleItemDto> TodaySchedule
);
public sealed record DailyRevenueDto(string Date, string DayName, decimal Revenue);
public sealed record ServiceBreakdownDto(string ServiceName, int BookingCount, double Percentage);
public sealed record ScheduleItemDto(string GuestName, string ServiceName, string StaffName, string ScheduledStartTime, string ScheduledEndTime, string Status);
```

**Confirmed backend patterns:**
- Query: `sealed record GetDashboardSummaryQuery() : IQuery<DashboardSummaryDto>`
- Handler: `IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>` — injects `IUnitOfWork` + `IUserContextService`
- Tenant isolation: `_contextService.GetCurrentTenantId()` as predicate filter
- Data source: `_unitOfWork.BookingRepository.GetAllAsync(b => b.TenantId == tenantId)` only — no other repositories needed
- Mapper: `public static DashboardSummaryDto ToDashboardSummaryDto(this IEnumerable<Booking> bookings)` in `DashboardMappings.cs`
- Controller: `[Route("api/[controller]")]` · `[HttpGet("summary")]` · `[Authorize]` · `Ok(await _mediator.Send(new GetDashboardSummaryQuery()))`
- Excluded: Avg. Rating (no review domain), "vs last week" comparisons (out of scope)

**FIRST STEP on resume:** Read `ApexBooking.Core.Application/mapper/BookingMappings.cs` to confirm exact Booking domain entity property names before writing mapper or handler. Do NOT assume property names.

---

**FRONTEND — after backend is built and DTO confirmed:**

Files to create:
- `features/dashboard/types/index.ts` — mirror `DashboardSummaryDto` (and nested types) exactly
- `features/dashboard/hooks/useDashboard.ts` — `axiosInstance.get<DashboardSummaryDto>('/dashboard/summary')`

Files to modify:
- `features/dashboard/pages/TenantDashboardPage.tsx` — remove inline `DashboardStats` type, remove direct `axiosInstance` call, consume `useDashboard` hook only

---

## DONE

---

### P-0.2 — Consolidate `useSlots` — delete wrong hooks, clean keeper

**Violations resolved:** V-05 (duplicate hooks), V-12 partial (dead X-Tenant-Id header in resources/useSlots)

**Findings:**
After reading all three hooks and tracing effective URLs through axiosInstance (`baseURL: '/api'`):

| File | Effective URL | Active consumers | Verdict |
|---|---|---|---|
| `features/bookings/hooks/useSlots.ts` | `/api/public/services/{id}/slots` ✓ | `CustomerBookingWizardPage.tsx` | **Kept** |
| `features/slots/hooks/useSlots.ts` | `/api/services/{id}/slots` ✗ | `SlotSelector.tsx` (commented out) | **Deleted** |
| `features/resources/hooks/useSlots.ts` | `/api/service/{id}/slots` ✗ | `SlotAvailabilityPage.tsx` (commented out) | **Deleted** |

`ResourceAvailabilityPage.tsx` uses `useResourceAvailability` — never used `useSlots`. No import updates required.

**Files deleted:** `src/features/resources/hooks/useSlots.ts`, `src/features/slots/hooks/useSlots.ts`
**Files modified:** `src/features/bookings/hooks/useSlots.ts` — dead commented block (lines 57–105) removed; inline task-notes removed
**Build gate:** `tsc --noEmit` — clean, zero errors

---

## PENDING

---

### P-0.3 — Remove dead `X-Tenant` header from `useBookings`

**Violations resolved:** V-12 partial (dead X-Tenant header in bookings hook)

**Problem:**
`features/bookings/hooks/useBookings.ts` line 27 defines `const headers = { 'X-Tenant': tenantSlug }` and passes it to every `axiosInstance` call. The backend ignores this header entirely — tenant is read from JWT claim only.

**Plan:**
1. Remove the `headers` const definition on line 27
2. Remove `{ headers }` from every `axiosInstance.get/post/put/delete` call in the file
3. The axios interceptor already injects `Authorization: Bearer` automatically — no manual headers needed

**Files to modify:** `src/features/bookings/hooks/useBookings.ts`

---

### P-0.4 — Fix `VerifyEmailPageComponent` — raw fetch, wrong endpoint, untyped DTO

**Violations resolved:** V-14 (assumed DTO, raw fetch)

**Problem:**
`features/auth/components/VerifyEmailPageComponent.tsx`:
- Uses raw `fetch('/auth/verify-email?token=...')` instead of `axiosInstance`
- Endpoint is wrong: backend serves `GET /api/auth/verify-account` not `/auth/verify-email`
- Accesses `data.url` without a typed DTO — the field is correct but untyped

**Backend confirmed:**
- Endpoint: `GET /api/auth/verify-account?token={token}`
- Response DTO: `{ url: string; tenantSlug?: string | null }`

**Plan:**
1. Add to `features/auth/types/index.ts`: `AccountVerificationResponseDto { url: string; tenantSlug?: string | null }`
2. Create `features/auth/hooks/useVerifyEmail.ts`:
   - Uses `axiosInstance.get<AccountVerificationResponseDto>('/auth/verify-account', { params: { token } })`
   - Returns `{ data, isLoading, error }`
3. Update `VerifyEmailPageComponent.tsx` to consume `useVerifyEmail` hook — remove raw `fetch`, remove direct `data.url` access without type

**Files to create:** `src/features/auth/hooks/useVerifyEmail.ts`
**Files to modify:** `src/features/auth/types/index.ts`, `src/features/auth/components/VerifyEmailPageComponent.tsx`

---

### P-0.5 — Delete all dead code

**Violations resolved:** V-06, V-07, V-08, V-09

**Items to delete:**

| File | Violation | Action |
|---|---|---|
| `features/bookings/hooks/useBookings.ts` lines 153–304 | V-06 | Delete the commented-out block only — keep active code above |
| `features/resources/pages/SlotAvailabilityPage.tsx` | V-07 | Delete entire file — entirely commented out |
| `features/slots/components/SlotSelector.tsx` | V-08 | Delete entire file — entirely commented out |
| `features/resources/schema/resourceSchema.ts` | V-09 | Delete entire file — entirely commented out |

**Plan:** Delete each item above. No refactoring. No rewrites. Just deletion. Rewrite from scratch during Phase 3 if needed.

---

### P-1.1 — `dashboard/` feature folder structure

**Violations resolved:** V-01

**Problem:** `features/dashboard/` contains only `TenantDashboardPage.tsx` at the root — no subdirectories.

**Plan:**
1. Create `features/dashboard/pages/`
2. Create `features/dashboard/components/`
3. Create `features/dashboard/hooks/` (will hold `useDashboard.ts` once P-0.1 is unblocked)
4. Create `features/dashboard/types/`
5. Move `TenantDashboardPage.tsx` into `pages/`
6. Update import in `AppRouter.tsx`

**Files to create:** folder structure only (+ move one file)
**Files to modify:** `src/routes/AppRouter.tsx`

---

### P-1.2 — `resources/` missing `components/` folder

**Violations resolved:** V-02

**Plan:** Create `src/features/resources/components/` folder. No files moved yet — population happens in P-3C during the UI redesign of ResourcesPage.

---

### P-1.3 — `service/` missing `components/` folder

**Violations resolved:** V-03

**Plan:** Create `src/features/service/components/` folder. Population happens in P-3C.

---

### P-1.4 — `public/` missing `components/` and `types/` folders

**Violations resolved:** V-04

**Plan:** Create `src/features/public/components/` and `src/features/public/types/`. Population happens in P-3A.

---

### P-2.1 — `renderField.tsx` — extract data fetching, remove inline styles

**Violations resolved:** V-10, V-17

**Problem:**
- Lines 44–64: `useEffect` with conditional API calls inside a shared presentational component
- Lines 22–26, 219–220, 284, 333: Hardcoded inline style values

**Plan:**
1. Read `renderField.tsx` in full before starting
2. Remove `useEffect` and all API call logic (lines 44–64)
3. Add prop: `resolvedOptions?: OptionItem[]` — callers must pre-load options and pass them in
4. Identify all features that use `dataSource.mode === 'remote'` → those feature hooks must load options before opening the modal
5. Replace all inline style objects with Bootstrap utility classes:
   - `fontSize: "13px"` → `small`
   - `fontWeight: 600` → `fw-semibold`
   - `marginBottom: "6px"` → `mb-1`
   - `maxHeight: 220` → CSS variable in `index.css`
   - `zIndex: 1000` → Bootstrap modal z-index class or `index.css`
   - `minHeight: "42px"` → Bootstrap form-control default height

**Files to modify:** `src/components/ui/modal/renderField.tsx`
**Files possibly affected:** any feature hook that uses `dataSource.mode === 'remote'` (must be identified during implementation)

---

### P-2.2 — `PhoneField.tsx` — extract phone parsing to utility

**Violations resolved:** V-11

**Problem:** `PhoneField.tsx` lines 50–75 contain regex matching, country code extraction, and sorting algorithm inside a shared component. Shared components must be presentation-only.

**Plan:**
1. Read `PhoneField.tsx` in full before starting
2. Create `src/utils/phoneUtils.ts` — move all parsing, regex, and sorting logic there
3. `PhoneField.tsx` imports from `phoneUtils` — component renders only

**Files to create:** `src/utils/phoneUtils.ts`
**Files to modify:** `src/components/ui/PhoneField.tsx`

---

### P-3A — UI Redesign: Public Zone

**Violations resolved:** V-19

**Pages:** `features/public/pages/landingpage.tsx`, `PricingPage.tsx`, `RequestAccessPage.tsx`

**Rules:**
- Bootstrap grid only: `container > row > col-*`
- Bootstrap spacing scale 0–5 only
- No `style={}` attributes
- No custom CSS variables inside component files — move to `index.css :root` only
- Card pattern: `card border shadow-sm rounded p-4`

**LandingPage changes:**
- Remove custom CSS variables (`--apex-blue`, `--apex-text`, etc.) from lines 87–99 — move any needed to `index.css :root`
- Replace all `style={}` objects (lines 146, 298+) with Bootstrap utility classes
- Enforce `container > row > col` layout throughout

**PricingPage + RequestAccessPage:** Audit and apply Bootstrap form and card standards.

**New components (in `features/public/components/`):** identify during implementation

---

### P-3B — UI Redesign: Auth Zone

**Pages:** `LoginPage`, `ForgotPasswordPage`, `ResetPasswordPage`, `VerifyEmailPage`, `VerifyEmailNoticePage`, `EmailVerificationPage`

**Standard layout for all auth pages:**
```
container
  row justify-content-center
    col-12 col-sm-10 col-md-6 col-lg-5
      card border-0 shadow-sm p-4 p-md-5
        h4 (page title)
        form (Bootstrap form)
          label above every input
          validation below every input (from hook error state only)
        btn-primary w-100 (with Bootstrap spinner when loading)
```

**Rules:**
- Loading state: Bootstrap spinner inside button + `disabled` attribute
- Error state: `div.alert.alert-danger` below form (not inside inputs)
- No inline styles anywhere

---

### P-3C — UI Redesign: Tenant Admin Zone

**Violations resolved:** V-18 (BookingCalendar inline fontSize)

**Pages:** `TenantDashboardPage`, `ResourcesPage`, `ResourceAvailabilityPage`, `ServicesPage`, `BookingsPage`, `SettingsPage`

**Standard admin page layout:**
```
container-fluid px-3 px-md-4 py-4
  row mb-4 align-items-center
    col
      h5.fw-bold (page title)
      small.text-muted (subtitle)
    col-auto
      btn.btn-primary (primary action, if applicable)
  row
    col-12
      (content)
```

**Per page:**

| Page | Key changes |
|---|---|
| `TenantDashboardPage` | Stat cards: `card border shadow-sm rounded p-4`; grid `col-6 col-md-4 col-xl-3`; skeleton screen component; unblocked by P-0.1 |
| `ResourcesPage` | Page header + Bootstrap table; FormModal with resourceSchema; ResourceSkeleton; empty state |
| `ResourceAvailabilityPage` | Fix hook (correct useSlots from P-0.2); Bootstrap calendar/slot grid |
| `ServicesPage` | Same pattern as ResourcesPage; ServiceSkeleton; empty state |
| `BookingsPage` | Bootstrap table; status as `badge bg-*`; dates via `utils/timeFormat.ts` |
| `SettingsPage` | Section-per-card: each settings group in `card p-4 mb-4` |

**Fix in this phase:** `BookingCalendar.tsx` line 90 — `fontSize: 15` → `small` CSS class

**Skeleton screen rule:** Each page gets its own skeleton component in its feature `components/` folder. CSS animation only. No third-party skeleton libraries.

---

### P-3D — UI Redesign: Customer Booking Zone

**Pages:** `CustomerBookingWizardPage`, `CustomerRegisterPage`, `PaymentSuccessPage`, `CancelBookingPage`

| Page | Key changes |
|---|---|
| `CustomerBookingWizardPage` | WizardProgressBar → Bootstrap step indicator; fix BookingCalendar inline style |
| `CustomerRegisterPage` | Bootstrap form layout — same auth pattern as P-3B |
| `PaymentSuccessPage` | Centered: `container > row justify-content-center > col-md-6 > card text-center p-5` |
| `CancelBookingPage` | Centered confirm card; `btn-danger` + `btn-secondary` |

---

### P-3E — UI Redesign: Super Admin Zone

**Violations resolved:** V-16 (SuperAdminLayout + Header hardcoded colors)

**Layout fixes (must happen before redesigning pages):**

`SuperAdminLayout.tsx`:
- Line 19: `backgroundColor: '#f8f9fa'` → `bg-light` class
- Lines 49, 62: `#6c757d`, `#adb5bd` → `text-secondary`, `text-muted`

`Header.tsx`:
- Line 65: `borderColor: 'rgba(0,0,0,0.08)'` → `border-bottom` class

**Pages:** `SuperAdminOverviewPage`, `OrganizationDetailPage`, `NewOrganizationPage`, `TenantRequestsPage`, `SuperAdminPaymentGatewayPage`

All pages use the same standard admin page layout defined in P-3C.

---

### P-4 — Modal system cleanup

**Violations resolved:** V-15, V-17 (inline styles in modal components)

**FormModal.tsx** (lines 52–56):
- Remove `background: "rgba(15, 23, 42, 0.45)"`
- Remove `backdropFilter: "blur(6px)"`
- Use Bootstrap modal's native backdrop — no custom overlay styles

**ConfirmModal.tsx** (line 20):
- Remove `backgroundColor: 'rgba(0,0,0,0.5)'`
- Use Bootstrap modal native backdrop

**renderField.tsx** — inline styles already handled in P-2.1

---

### P-5 — Layout cleanup

**Violations resolved:** V-16 (if not already handled in P-3E)

Note: P-3E addresses SuperAdminLayout and Header. P-5 is a verification pass to confirm all layout components are clean. If P-3E fully resolves V-16, P-5 closes automatically.

---

## Typing & DTO Violation Index

| Violation | Description | Resolved in |
|---|---|---|
| V-T1 | useBookings.getAll untyped — PagedResult<TenantBookingsDto> missing | C-1 |
| V-T2 | useStaff.getAll untyped — PagedResult<StaffDto> missing | C-2 |
| V-T3 | useStaffAvailability.getSchedule untyped — StaffAvailabilityDto missing | C-3 |
| V-T4 | useStaffAvailability.getExceptions untyped — PagedResult<AvailabilityException> missing | C-4 |
| V-T5 | Booking type ≠ TenantBookingsDto field names (critical) | D-1 |
| V-T6 | AvailabilityException extra fields not in StaffAvailabilityExceptionDto | D-2 |
| V-T7 | AvailableSlotsResponse.resourceId ≠ AvailableSlotsDto.staffId | D-3 |
| V-T8 | InitiatePaymentResult inline in hook file | E-1 |
| V-T9 | DayAvailabilityDto + MonthlyAvailabilityDto inline in hook file | E-2 |
| V-T10 | SuperAdminLoginResponseData inline in hook, duplicates AuthResponseData | E-3 |
| V-T11 | useResetPassword accesses result.accessToken on 204 NoContent response | F-1 |

---

## Violation Index (for reference)

| Violation | Resolved in |
|---|---|
| V-01 Feature structure: dashboard | P-1.1 |
| V-02 Feature structure: resources | P-1.2 |
| V-03 Feature structure: service | P-1.3 |
| V-04 Feature structure: public | P-1.4 |
| V-05 Duplicate useSlots hooks | P-0.2 |
| V-06 Dead code: useBookings commented block | P-0.5 |
| V-07 Dead code: SlotAvailabilityPage.tsx | P-0.5 |
| V-08 Dead code: SlotSelector.tsx | P-0.5 |
| V-09 Dead code: resourceSchema.ts | P-0.5 |
| V-10 renderField data fetching | P-2.1 |
| V-11 PhoneField parsing logic | P-2.2 |
| V-12 Wrong/dead API headers (all) | P-0.1 + P-0.2 + P-0.3 |
| V-13 API call in TenantDashboardPage | P-0.1 |
| V-14 Raw fetch + assumed DTO in VerifyEmail | P-0.4 |
| V-15 Hardcoded styles: modals | P-4 |
| V-16 Hardcoded styles: layout components | P-3E + P-5 |
| V-17 Hardcoded styles: renderField | P-2.1 |
| V-18 Hardcoded style: BookingCalendar | P-3C |
| V-19 Hardcoded styles: landingpage | P-3A |
| V-20 BaseResponse type definition in src/types + auth/types | A-1 |
| V-21 axiosInstance uses BaseResponse for refresh + error parsing | A-1 |
| V-22 All 23 hooks use result.isSuccess / result.data / result.errors (BaseResponse pattern) | A-2 → A-8 |
| V-23 useSlots malformed URL (missing leading /) | A-3 |
| V-24 useBookings.getCustomerBooking calls non-existent endpoint | A-3 |
| V-25 useCancellationToken wrong endpoint sub-paths | B-1 |
| V-26 useServices X-Tenant header + dead commented block | A-5 |
| V-27 BookingsPage uses raw HTML table instead of Table component | B-2 |

---

## NEW PENDING

---

### A-1 — BaseResponse: remove type definitions + fix axiosInstance

**Violations resolved:** V-20, V-21

**Root cause:** Backend removed all `BaseResponse<T>` wrappers. Success responses are now raw DTOs only. Error responses are `{ message: string; errorCode?: string }`. The frontend `BaseResponse<T>` definition and every type that extends it are now incorrect mirrors.

**Problem — src/types/index.ts:**
- Lines 12–20: `BaseResponse<T>` interface defines `isSuccess`, `data`, `errors[]` — backend no longer returns this shape
- Must be deleted entirely. `Error` interface (lines 22–26) referencing the old `errors[]` shape must also be removed or replaced
- `PagedResult<T>` (lines 29–32: `{ data: T[], total: number }`) is still valid — keep it

**Problem — src/features/auth/types/index.ts:**
- `AuthResponse extends BaseResponse<AuthResponseData>` (line 13) — FORBIDDEN. Replace with raw: `AuthResponseData` directly (no wrapper)
- `EmailVerificationResponse extends BaseResponse<EmailVerificationData>` (line 20) — FORBIDDEN. Remove (already superseded by `AccountVerificationResponseDto` on line 34)
- `AccountVerificationResponse extends BaseResponse<AccountVerificationResponseData>` (line 45) — FORBIDDEN. Remove (superseded by `AccountVerificationResponseDto`)
- `ForgotPasswordResponse extends BaseResponse<ForgotPasswordResponseData>` (line 57) — FORBIDDEN. `ForgotPasswordResponseData { message: string }` looks invented — verify with backend; likely backend returns 200 with no body or a raw message
- `ResetPasswordResponse extends BaseResponse<ResetPasswordResponseData>` (line 74) — FORBIDDEN. Replace with raw `ResetPasswordResponseData` directly
- `RefreshTokenResponse extends BaseResponse<RefreshTokenResponseData>` (line 86) — FORBIDDEN. Replace with raw `RefreshTokenResponseData`
- `PaginatedResponse<T> extends BaseResponse<T[]>` (line 92) — FORBIDDEN. Remove entirely; use `PagedResult<T>` from `src/types/index.ts` for paginated results

**Problem — src/services/axiosInstance.ts:**
- Line 4: `import type { BaseResponse } from '../types'` — imports the forbidden type
- Line 3: `import type { RefreshTokenResponse } from '../features/auth/types'` — `RefreshTokenResponse extends BaseResponse<...>` — forbidden; must change to raw `RefreshTokenResponseData`
- Lines 64–78: token refresh handler checks `refreshResponse.isSuccess && refreshResponse.data?.accessToken` — with BaseResponse removed, the raw response is `RefreshTokenResponseData = { accessToken, userId, tenantId }`, so check becomes direct field access: `refreshResponse.accessToken`
- Lines 88–94: error handler casts to `BaseResponse` and reads `serverData?.errors?.[0]?.message` — backend now returns `{ message, errorCode? }` directly, so error handler must read `serverData?.message` instead

**Plan:**
1. Remove `BaseResponse<T>` from `src/types/index.ts` (lines 12–20). Remove `Error` interface (lines 22–26).
2. In `src/features/auth/types/index.ts`: remove all `extends BaseResponse<X>` response types. Keep only raw data interfaces. Remove `PaginatedResponse<T>`.
3. In `src/services/axiosInstance.ts`:
   - Change `import type { RefreshTokenResponse }` to `import type { RefreshTokenResponseData }`
   - Remove `import type { BaseResponse }`
   - Fix refresh handler: `refreshResponse.accessToken` instead of `refreshResponse.isSuccess && refreshResponse.data?.accessToken`
   - Fix error handler: `serverData?.message` instead of `serverData?.errors?.[0]?.message`
4. Run `tsc --noEmit` — will fail until all hooks are also fixed; track remaining errors

**Files to modify:** `src/types/index.ts`, `src/features/auth/types/index.ts`, `src/services/axiosInstance.ts`

---

### A-2 — BaseResponse: fix auth hooks

**Violations resolved:** V-22 (auth subset)

**Background:** With BaseResponse removed, every hook that checks `result.isSuccess` is broken. The axiosInstance error interceptor already throws `ApiError` on failure — so failed requests go to the `catch` block automatically. Success = raw DTO returned directly. No `isSuccess` check needed anywhere.

**Pattern change for ALL hooks:**

OLD (broken):
```ts
const result = await axiosInstance.post<BaseResponse<SomeDto>>('/endpoint', data)
if (!result.isSuccess) {
  setError(result.errors?.[0]?.message ?? 'Failed')
  return
}
const value = result.data?.field
```

NEW (correct):
```ts
const result = await axiosInstance.post<SomeDto>('/endpoint', data)
// No isSuccess check — failure goes to catch automatically
const value = result.field  // direct field access
```

**Files to fix:**

`features/auth/hooks/useLogin.ts`:
- Remove `import type { AuthResponse }`; use raw `AuthResponseData`
- `result.isSuccess` check (line 30) → remove
- `result.data?.accessToken` (line 35) → `result.accessToken`
- `result.data?.tenantSlug` (line 36) → `result.tenantSlug`
- Error from catch block only; `result.errors` access → remove

`features/auth/hooks/useForgotPassword.ts`:
- Read file first to confirm exact BaseResponse usage
- Remove `isSuccess` check; error from catch only
- Backend likely returns 200 with empty body or `{ message: string }` — verify exact shape

`features/auth/hooks/useResetPassword.ts`:
- Read file first to confirm exact BaseResponse usage
- Remove `isSuccess` check; replace `result.data?.accessToken` with `result.accessToken` etc.

**Files to modify:** `src/features/auth/hooks/useLogin.ts`, `src/features/auth/hooks/useForgotPassword.ts`, `src/features/auth/hooks/useResetPassword.ts`

**Note:** `useVerifyEmail.ts` and `useLogout.ts` also appeared in the grep — read them before implementing to confirm whether they use the BaseResponse pattern.

---

### A-3 — BaseResponse: fix booking hooks + useSlots URL + useBookings dead endpoint

**Violations resolved:** V-22 (booking subset), V-23, V-24

**Files to fix:**

`features/bookings/hooks/useSlots.ts` (V-23 + BaseResponse):
- Line 22: URL `public/services/${serviceId}/slots` → `/public/services/${serviceId}/slots` (add leading `/`)
- Lines 26–33: Remove `result.isSuccess` check and `result.data` / `result.errors` access
- Backend returns `AvailableSlotsResponse` directly; set `setSlots(result)` not `setSlots(result.data)`

`features/bookings/hooks/useBookings.ts` (V-22 + V-24):
- `getCustomerBooking` function (lines 42–57): calls `GET /booking/customer/${user?.id}` — this endpoint does not exist in the backend. Remove this function entirely. Remove `customerBookings` state. Remove `CustomerBooking` type import if unused.
- `create` (line 65): `result.isSuccess` check → remove; error from catch only
- `update` (lines 90–91): same — remove isSuccess check
- `cancel` (lines 115–116): same — remove isSuccess check
- `getAll` (line 34): `result.data ?? []` — if backend returns `Booking[]` directly (not wrapped), change to `result ?? []`. Confirm backend paginated shape first.

`features/bookings/hooks/useInitiatePayment.ts`:
- Read file first; confirm BaseResponse usage; remove isSuccess / result.data / result.errors pattern

`features/bookings/hooks/useMonthlyAvailability.ts`:
- Read file first; confirm BaseResponse usage; remove pattern

**Files to modify:** `src/features/bookings/hooks/useSlots.ts`, `src/features/bookings/hooks/useBookings.ts`, `src/features/bookings/hooks/useInitiatePayment.ts`, `src/features/bookings/hooks/useMonthlyAvailability.ts`
**Files to delete:** Remove `getCustomerBooking` and `customerBookings` from `useBookings.ts`

---

### A-4 — BaseResponse: fix public hook

**Violations resolved:** V-22 (public subset)

`features/public/hooks/useTenantRequest.ts`:
- Read file first; confirm BaseResponse usage
- Backend `POST /api/public/tenant-requests` returns raw DTO or 200/201 with no body — verify shape
- Remove `isSuccess` check; error from catch only

**Files to modify:** `src/features/public/hooks/useTenantRequest.ts`

---

### A-5 — BaseResponse: fix service hook + remove X-Tenant + remove dead commented code

**Violations resolved:** V-22 (service subset), V-26

`features/service/hooks/useServices.ts`:
- Line 14: `const headers = { 'X-Tenant': tenantSlug }` → remove (backend ignores header; tenant from JWT)
- Remove `headers` from all `axiosInstance` calls (lines 20, 37, 55, 73)
- `getAll` (line 24): `result.data ?? []` and `result.total ?? 0` — `PagedResult<Service>` shape `{ data: T[], total: number }` should be correct if backend returns that shape; verify. If backend returns `{ data: Service[], total: number }` for paginated lists, keep these accesses.
- `create` (line 38): `result.isSuccess` check → remove; error from catch only
- `update` (line 55): same
- `deactivate` (line 73): same
- Lines 99–196: dead commented-out code block → delete entirely

**Files to modify:** `src/features/service/hooks/useServices.ts`

---

### A-6 — BaseResponse: fix settings hooks

**Violations resolved:** V-22 (settings subset)

`features/settings/hooks/useTenantSettings.ts`:
- Lines 15–22: `BaseResponse<TenantSettingsDto>` → change to `TenantSettingsDto` direct; remove `isSuccess` check, `result.data` → `result`
- Lines 34–43: same for update

`features/settings/hooks/usePaymentGateway.ts`, `usePaymentPolicy.ts`, `useTenantProfile.ts`:
- Read each file first; confirm BaseResponse usage
- Apply same pattern: remove `isSuccess` check, access fields directly on result

**Files to modify:** `src/features/settings/hooks/useTenantSettings.ts`, `src/features/settings/hooks/usePaymentGateway.ts`, `src/features/settings/hooks/usePaymentPolicy.ts`, `src/features/settings/hooks/useTenantProfile.ts`

---

### A-7 — BaseResponse: fix staff hooks

**Violations resolved:** V-22 (staff subset)

`features/staff/hooks/useStaff.ts`:
- Line 18: `result.data ?? []` — confirm backend list shape; if `Staff[]` directly then `result ?? []`
- Lines 32–33, 50–51, 68–69: `isSuccess` checks on create/update/deactivate → remove; error from catch only

`features/staff/hooks/useStaffAvailability.ts`:
- Read file first; confirm BaseResponse usage (grep showed `res.isSuccess` pattern)
- Remove isSuccess checks; access fields directly

**Files to modify:** `src/features/staff/hooks/useStaff.ts`, `src/features/staff/hooks/useStaffAvailability.ts`

---

### A-8 — BaseResponse: fix superadmin hooks

**Violations resolved:** V-22 (superadmin subset)

Seven hooks, all using `BaseResponse<T>` pattern:

`useAcceptInvitation.ts`, `useCreateOrganization.ts`, `useOrganizationDetail.ts`, `usePlatformPaymentGateway.ts`, `useSuperAdminLogin.ts`, `useSuperAdminOrganizations.ts`, `useTenantRequests.ts`

For each:
- Read file first to confirm exact usage
- Replace `axiosInstance.get<BaseResponse<X>>` with `axiosInstance.get<X>`
- Remove all `result.isSuccess` checks
- Change `result.data` / `result.data!` field access to direct `result` field access
- Remove `result.errors?.[0]?.message` — errors go to catch block

`useOrganizationDetail.ts` special case (lines 48–53): updates local state with `result.data` after createUser/assignUser — must change to `result` (the raw returned DTO).

**Files to modify:** `src/features/superadmin/hooks/useAcceptInvitation.ts`, `src/features/superadmin/hooks/useCreateOrganization.ts`, `src/features/superadmin/hooks/useOrganizationDetail.ts`, `src/features/superadmin/hooks/usePlatformPaymentGateway.ts`, `src/features/superadmin/hooks/useSuperAdminLogin.ts`, `src/features/superadmin/hooks/useSuperAdminOrganizations.ts`, `src/features/superadmin/hooks/useTenantRequests.ts`

---

## TYPING & DTO VIOLATIONS (new pending)

---

### C-1 — useBookings.getAll: Missing type parameter (returns `any`)

**Violations resolved:** V-T1

**File:** `src/features/bookings/hooks/useBookings.ts` line 25

**Problem:** `axiosInstance.get('/booking', { params })` has no generic type parameter. The result is typed as `any`. Accessing `result.data` and using the value as `Booking[]` provides no type safety.

**Backend confirmed:** `GET /api/booking` returns `PagedResult<TenantBookingsDto>` (see `GetBookingsQueryHandler`). `PagedResult<T>` shape: `{ data: T[], total: number }` (camelCase).

**Blocked by D-1:** `Booking` frontend type does not match `TenantBookingsDto`. C-1 must be fixed **after** D-1 defines the correct DTO type. The type param must be `PagedResult<TenantBookingsDto>` once D-1 is resolved.

---

### C-2 — useStaff.getAll: Missing type parameter (returns `any`)

**Violations resolved:** V-T2

**File:** `src/features/staff/hooks/useStaff.ts` line 15

**Problem:** `axiosInstance.get('/staff', { params })` has no generic type parameter. Result is `any`. Accesses `result.data ?? []` and `result.total ?? 0` without type safety.

**Backend confirmed:** `GET /api/staff` returns `PagedResult<StaffDto>`. Frontend `StaffDto` type in `staff/types/index.ts` matches the backend record (`id`, `firstName`, `lastName`, `email`, `contactNumber`, `description`, `capacity`, `isActive`, `createdAt`, `updatedAt`). ✓

**Plan:**
1. Add `import type { PagedResult } from '../../../types'` to `useStaff.ts`
2. Change `axiosInstance.get('/staff', ...)` → `axiosInstance.get<PagedResult<StaffDto>>('/staff', ...)`
3. No other changes needed — existing `result.data` and `result.total` accesses are correct

**Files to modify:** `src/features/staff/hooks/useStaff.ts`

---

### C-3 — useStaffAvailability.getSchedule: Missing type parameter (returns `any`)

**Violations resolved:** V-T3

**File:** `src/features/staff/hooks/useStaffAvailability.ts` line 21

**Problem:** `axiosInstance.get(\`/staff/${staffId}/availability\`)` has no type parameter. Accesses `res.schedules` from an untyped result.

**Backend confirmed:** `GET /api/staff/{staffId}/availability` returns `StaffAvailabilityDto { staffId: Guid, schedules: DayScheduleDto[] }`. Frontend already has `DaySchedule` type in `staff/types/index.ts` that matches `DayScheduleDto`. ✓ But there is no `StaffAvailabilityDto` wrapper type on the frontend yet.

**Plan:**
1. Add `StaffAvailabilityDto` interface to `src/features/staff/types/index.ts`: `{ staffId: string; schedules: DaySchedule[] }`
2. Import it in `useStaffAvailability.ts`
3. Change `axiosInstance.get(...)` → `axiosInstance.get<StaffAvailabilityDto>(\`/staff/${staffId}/availability\`)`
4. `res.schedules` access is then correctly typed

**Files to modify:** `src/features/staff/types/index.ts`, `src/features/staff/hooks/useStaffAvailability.ts`

---

### C-4 — useStaffAvailability.getExceptions: Missing type parameter (returns `any`)

**Violations resolved:** V-T4

**File:** `src/features/staff/hooks/useStaffAvailability.ts` line 34

**Problem:** `axiosInstance.get(\`/staff/${staffId}/exceptions\`, { params })` has no type parameter. Accesses `res.data ?? []` from an untyped result.

**Backend confirmed:** `GET /api/staff/{staffId}/exceptions` returns `PagedResult<StaffAvailabilityExceptionDto>`.

**Blocked by D-2:** `AvailabilityException` frontend type has extra fields not in the backend DTO. Resolve D-2 first to clean up the type, then apply the type parameter here.

**Plan (after D-2):**
1. Add `import type { PagedResult } from '../../../types'` to `useStaffAvailability.ts`
2. Change `axiosInstance.get(...)` → `axiosInstance.get<PagedResult<AvailabilityException>>(...)`

**Files to modify:** `src/features/staff/hooks/useStaffAvailability.ts`

---

### D-1 — `Booking` type does not match `TenantBookingsDto` — critical field mismatch

**Violations resolved:** V-T5

**Severity:** CRITICAL — `BookingsPage.tsx` renders undefined for customer name, email, and schedule because the field names are wrong.

**Backend confirmed (`TenantBookingsDto` from mapper):**
```
bookingId, bookingReference, serviceId, serviceName, resourceId, resourceName,
guest: { guestId, firstName, lastName, email, phone } | null,
scheduledDate, scheduledStartTime, scheduledEndTime, durationMinutes,
status, confirmationMode, priceSnapshot, currencyCode,
customerNotes, cancellationReason, cancelledAt, createdAt
```

**Frontend `Booking` type — fields that are WRONG:**
| Frontend field | Reality | Fix |
|---|---|---|
| `id` | backend sends `bookingId` | rename to `bookingId` |
| `customerName` | does not exist — backend sends `guest.firstName + ' ' + guest.lastName` | remove; add nested `guest` |
| `customerEmail` | does not exist — backend sends `guest.email` | remove; add nested `guest` |
| `startTime` | does not exist — backend sends `scheduledDate` (DateOnly) + `scheduledStartTime` (TimeOnly) | remove; add separate fields |
| `endTime` | does not exist — backend sends `scheduledEndTime` (TimeOnly) | remove; add `scheduledEndTime` |
| `totalPrice` | does not exist — backend sends `priceSnapshot` | remove (already have `priceSnapshot`) |
| `notes` | does not exist — backend sends `customerNotes` | rename to `customerNotes` |
| `updatedAt` | does not exist in `TenantBookingsDto` | remove |
| `tenantId` | does not exist in `TenantBookingsDto` | remove |

**Missing from frontend type:**
- `guest: GuestDto | null` (needs `GuestDto { guestId, firstName, lastName, email, phone | null }`)
- `scheduledDate: string` (DateOnly serialized as "YYYY-MM-DD")
- `scheduledStartTime: string` (TimeOnly serialized as "HH:mm:ss")
- `scheduledEndTime: string`
- `durationMinutes: number`
- `confirmationMode: string`
- `customerNotes: string | null`
- `cancellationReason: string | null`
- `cancelledAt: string | null`

**Plan:**
1. In `features/bookings/types/index.ts`:
   - Add `GuestDto` interface: `{ guestId: string; firstName: string; lastName: string; email: string; phone: string | null }`
   - Replace `Booking` interface entirely with fields matching `TenantBookingsDto`
2. In `features/bookings/hooks/useBookings.ts`:
   - Add `PagedResult<Booking>` type param (C-1)
   - Change `setBookings(result.data ?? [])` — `result.data` access remains correct
3. In `features/bookings/pages/BookingsPage.tsx`:
   - Fix all field accesses: `b.id` → `b.bookingId`, `b.customerName` → `b.guest?.firstName + ' ' + b.guest?.lastName`, `b.customerEmail` → `b.guest?.email`, `b.startTime` / `b.endTime` → `b.scheduledDate` / `b.scheduledStartTime` / `b.scheduledEndTime`, `b.notes` → `b.customerNotes`
   - Update `openEdit` to read from correct fields
   - Update `getStatusBadgeClass` for `PendingPayment` and `NoShow` cases (already handled)
4. Run `tsc --noEmit`

**Files to modify:** `src/features/bookings/types/index.ts`, `src/features/bookings/hooks/useBookings.ts`, `src/features/bookings/pages/BookingsPage.tsx`

---

### D-2 — `AvailabilityException` has extra fields not in `StaffAvailabilityExceptionDto`

**Violations resolved:** V-T6

**File:** `src/features/staff/types/index.ts`

**Backend confirmed (`StaffAvailabilityExceptionDto`):** `id`, `exceptionDate`, `exceptionType`, `startTime`, `endTime`, `note` — 6 fields only.

**Frontend `AvailabilityException` extra fields that do NOT exist in backend response:**
- `staffId` — not in DTO
- `createdAt` — not in DTO
- `updatedAt` — not in DTO

Components accessing `exception.staffId`, `exception.createdAt`, or `exception.updatedAt` get `undefined` at runtime.

**Plan:**
1. Remove `staffId`, `createdAt`, `updatedAt` from `AvailabilityException` in `features/staff/types/index.ts`
2. Check for any component usage of those fields and remove them
3. Run `tsc --noEmit` — TypeScript will surface any remaining usages

**Files to modify:** `src/features/staff/types/index.ts` (and any components that access removed fields)

---

### D-3 — `AvailableSlotsResponse.resourceId` ≠ backend `AvailableSlotsDto.staffId`

**Violations resolved:** V-T7

**File:** `src/features/bookings/types/index.ts`

**Backend confirmed (`AvailableSlotsDto`):** field name is `staffId` (type `Guid?`, camelCase: `staffId`).

**Frontend `AvailableSlotsResponse`:** has `resourceId: string | null` — this field is ALWAYS `undefined` at runtime because the backend sends `staffId`, not `resourceId`.

**Plan:**
1. Rename `resourceId` to `staffId` in `AvailableSlotsResponse` in `features/bookings/types/index.ts`
2. Find all consumers of `AvailableSlotsResponse.resourceId` and update them to `staffId`
3. Run `tsc --noEmit`

**Files to modify:** `src/features/bookings/types/index.ts`, and any consumers of `resourceId` on slots response (identify during implementation)

---

### E-1 — `useInitiatePayment`: Inline DTO type belongs in `features/bookings/types/`

**Violations resolved:** V-T8

**File:** `src/features/bookings/hooks/useInitiatePayment.ts` lines 4–8

**Problem:** `InitiatePaymentResult { approvalUrl, gatewayTransactionId, bookingReference }` is defined inline. CLAUDE.md: types defined in hooks violate the types/ separation rule.

**Backend confirmed:** Matches `InitiatePaymentDto` exactly (camelCase serialization). Fields correct. ✓

**Plan:**
1. Move `InitiatePaymentResult` to `src/features/bookings/types/index.ts`
2. Import it in `useInitiatePayment.ts`

**Files to modify:** `src/features/bookings/types/index.ts`, `src/features/bookings/hooks/useInitiatePayment.ts`

---

### E-2 — `useMonthlyAvailability`: Inline DTO types belong in `features/bookings/types/`

**Violations resolved:** V-T9

**File:** `src/features/bookings/hooks/useMonthlyAvailability.ts` lines 4–13

**Problem:** `DayAvailabilityDto` and `MonthlyAvailabilityDto` are defined inline in the hook. CLAUDE.md: types defined in hooks violate the types/ separation rule.

**Backend confirmed:** Match `MonthlyAvailabilityDto` and `DayAvailabilityDto` exactly (camelCase). ✓

**Plan:**
1. Move both interfaces to `src/features/bookings/types/index.ts`
2. Import them in `useMonthlyAvailability.ts`

**Files to modify:** `src/features/bookings/types/index.ts`, `src/features/bookings/hooks/useMonthlyAvailability.ts`

---

### E-3 — `useSuperAdminLogin`: Inline type duplicates `AuthResponseData`

**Violations resolved:** V-T10

**File:** `src/features/superadmin/hooks/useSuperAdminLogin.ts` lines 6–8

**Problem:** `SuperAdminLoginResponseData { accessToken: string }` is defined inline. Backend `POST /auth/login/superadmin` returns `AuthResponseDto` which is identical in shape to `AuthResponseData` in `features/auth/types/index.ts`.

**Plan:**
1. Remove inline `SuperAdminLoginResponseData`
2. Import `AuthResponseData` from `../../auth/types`
3. Use `AuthResponseData` as the type parameter on `axiosInstance.post`

**Files to modify:** `src/features/superadmin/hooks/useSuperAdminLogin.ts`

---

### F-1 — `useResetPassword`: Backend returns 204 NoContent — hook crashes on result.accessToken

**Violations resolved:** V-T11

**Severity:** HIGH — Runtime crash on successful password reset.

**File:** `src/features/auth/hooks/useResetPassword.ts` lines 32–38

**Backend confirmed:** `POST /api/auth/reset-password` → `return NoContent()` (204). No response body. Confirmed from `AuthController.cs` line 63.

**Problem:** Hook does:
```ts
const result = await axiosInstance.post<ResetPasswordResponseData>('/auth/reset-password', ...)
const { accessToken } = result   // ← result is undefined (204 no body) → runtime crash
```
When 204 is returned, the axios response interceptor unwraps `response.data` which is an empty string or undefined. Destructuring `const { accessToken } = undefined` throws a `TypeError` at runtime.

The current logic tries to auto-login after reset (navigate to `/dashboard` if `accessToken` exists). This is architecturally wrong — the backend explicitly returns 204 to signal success with no auth state change.

**Plan:**
1. Remove generic type parameter from `axiosInstance.post` call (or use `void`)
2. Remove `const result = await ...` — just `await axiosInstance.post(...)`
3. Remove `const { accessToken } = result` and all branches that read `result`
4. After successful `await`, always show success message and navigate to `/login` after a delay
5. `ResetPasswordResponseData` type in `features/auth/types/index.ts` is now unused — remove it

**Files to modify:** `src/features/auth/hooks/useResetPassword.ts`, `src/features/auth/types/index.ts`

---

## BLOCKED (new)

---

### B-1 — useCancellationToken: Fix BaseResponse pattern + NoContent handling (URLs now confirmed)

**Violations resolved:** V-25

**URLs confirmed from `PublicController.cs`:**
- `GET /api/public/cancellation/validate?token=X` — frontend URL `/public/cancellation/validate?token=...` ✓ CORRECT
- `POST /api/public/cancellation/cancel` — frontend URL `/public/cancellation/cancel` ✓ CORRECT

**Backend response shapes confirmed:**
- `GET validate` → returns raw `CancellationTokenValidationDto` (not wrapped) — on failure, throws exception (caught as ApiError)
- `POST cancel` → returns `NoContent()` (204) — no body, no `isSuccess` to check

**Problem in current hook:**
- Validate: uses `axiosInstance.get<BaseResponse<CancellationTokenValidation>>(...)` — wrong; should be `CancellationTokenValidation` directly. Error state detection is currently via `result.errors[0].message` string matching — with the raw DTO, exceptions go to catch block instead.
- Cancel: uses `axiosInstance.post<BaseResponse<boolean>>(...)` and checks `result.isSuccess` — wrong; backend returns 204. Should just `await axiosInstance.post(...)` and return `true` on success, `false` in catch.

**Error state determination (post-fix):**
Since the backend throws business exceptions for expired/used/invalid tokens (`EnsureNotUsed`, `EnsureNotExpired`, `NotFoundException`), the ApiError in the catch block will have an `errorCode` or `message`. The state transitions (`used`, `expired`, `invalid`) must now be determined from `err.message` or `err.data?.errorCode` in the catch block.

**Plan:**
1. Remove `import type { BaseResponse } from '../../../types'`
2. Validate function: change type param to `CancellationTokenValidation`; access `result.bookingId` etc. directly; move error state logic to catch block using `err.message` matching
3. Cancel function: remove `BaseResponse<boolean>` type param; change to `await axiosInstance.post('/public/cancellation/cancel', { token })`; return `true` on success, catch sets error and returns `false`
4. Run `tsc --noEmit`

**Files to modify:** `src/features/bookings/hooks/useCancellationToken.ts`

---

## NEW PENDING (component usage)

---

### B-2 — BookingsPage: use Table reusable component

**Violations resolved:** V-27

**Problem:** `features/bookings/pages/BookingsPage.tsx` line 262 renders a raw `<table className="table table-hover align-middle mb-0">` with manual `<thead>`, `<tbody>`, `<tr>`, `<td>` structure. The reusable `Table` component exists at `src/components/ui/table/table.tsx` and is used correctly in other feature pages.

**Plan:**
1. Read `BookingsPage.tsx` in full to understand the current table columns and row rendering
2. Read `src/components/ui/table/table.tsx` to confirm its column/data API
3. Replace the raw `<table>` with `<Table>` component using the confirmed API
4. Ensure row actions (cancel button) are handled via column renderers
5. Run `tsc --noEmit`

**Files to modify:** `src/features/bookings/pages/BookingsPage.tsx`

---

## Notes

- **Approval protocol:** State task → define plan → get explicit approval → implement → verify → proceed to next
- **Build gate:** Run `npm run build` or type-check after each task before proceeding to the next
- **Blocked item:** P-0.1 skipped — waiting for backend to create `GET /api/dashboard/summary`. Resume P-0.1 when backend endpoint is confirmed and DTO fields are provided.
- **Current task:** P-0.1 approved and ready to implement. Resume by reading `ApexBooking.Core.Application/mapper/BookingMappings.cs` to confirm Booking entity property names, then implement the 5 backend files in the order listed in the P-0.1 section above. Frontend hook follows after backend is built.
- **Commented-out routes in AppRouter.tsx:** `CustomerProtectedRoute`, `CustomerLayout`, `CustomerProfilePage`, `CustomerBookingsPage`, `PublicTenantPage` — do not activate these during this redesign. Out of scope.
- **Known architecture decisions:** `schema/` folder is formally recognized in `CLAUDE.md` as an optional feature subfolder for `ModelSchema[]` form config.
