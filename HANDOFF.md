# ApexBooking — Session Handoff Document

**Last updated:** 2026-05-19  
**Purpose:** Self-contained context for continuing interrupted work. Read this entire file before writing any code.

---

## 1. Project Overview

ApexBooking is a **multitenant staff-management booking platform**.

- Backend: ASP.NET Core, MediatR CQRS, DDD, EF Core, SQL Server
- Frontend: React + TypeScript + Vite + Bootstrap 5
- Solution root: `c:\Users\Wyrlo\Projects\ApexBooking\`
- Frontend root: `c:\Users\Wyrlo\Projects\ApexBooking\apex-booking-client\`
- Architecture rules: `CLAUDE.md` (backend) and `apex-booking-client\CLAUDE.md` (frontend) — read both before generating any code

---

## 2. Backend Architecture — Confirmed Patterns

### Layer Order (dependency flows top to bottom)
```
SharedKernel → GenericRepository → Domain → Persistence → Application → Infrastructure → WebApi
```

### Key Patterns (verified by reading source)

**Domain**
- Aggregates: `Booking`, `Staff`, `Service`, `Tenant`, `User` — all have private constructor + static `Create()` factory
- All state mutations via aggregate methods only (`booking.Confirm()`, `booking.Cancel()`, etc.)
- Business rule violations throw `BusinessRuleBrokenException` immediately
- Child collections exposed as `IReadOnlyCollection` with private backing `List<T>`
- Value object IDs: strongly-typed records — `BookingId`, `StaffId`, `TenantId`, `ServiceId`, etc. in `Domain/ValueObjects/ValueObjectsIdentifier.cs`
- `SlotAvailabilityService` lives in Domain — pure, stateless, takes domain objects
- `PlanLimits` static policy class in `Domain/Policies/`

**Application (CQRS)**
- Commands: `ICommand<TResponse>` or `ICommand` (void), handlers: `ICommandHandler<TCommand, TResponse>`
- Queries: `IQuery<TResponse>`, handlers: `IQueryHandler<TQuery, TResponse>`
- All in `Application/Features/[Domain]/Commands/[Name]/` or `.../Queries/[Name]/`
- Handlers are `internal sealed` classes
- All persistence access via `IUnitOfWork` only — never inject repositories directly
- FluentValidation validators in `Application/Common/Validators/`, run via `ValidationBehavior<TRequest, TResponse>` pipeline
- DTOs in `Application/Dtos/`, mappers in `Application/mapper/` as static extension methods (`entity.ToDto()`)

**Persistence**
- `UnitOfWork` exposes all repositories as lazy properties
- Repositories extend `GenericRepository<TEntity>` and implement domain `IXxxRepository`
- EF Core Fluent API configs in `Persistence/Mappings/`, snake_case columns, value object converters

**Infrastructure**
- Implements abstractions only: email, JWT, cookies, background jobs
- `BookingNotificationService` implements `IBookingNotificationService`
- `JwtTokenService` implements `ITokenService` — RSA-SHA256, JTI blacklisting
- `TrialExpiryJob` — background job for trial management

**WebApi**
- Controllers: thin, only `IMediator.Send()` calls, return raw DTOs or `NoContent()`
- `GlobalExceptionHandler` — sole exception-to-HTTP translation point
- `TenantMiddleware` — sets `TenantId` from JWT `tenant_id` claim only

### Known Boundary Violations (documented — do not expand)
- `User : IdentityUser<Guid>` — Domain depends on ASP.NET Identity
- `IUnitOfWork` exposes `UserManager` / `RoleManager`
- Email HTML inline in `CreateBookingCommandHandler` and `TrialExpiryJob`

### Known Bugs (identified, not yet fixed)
- `CreateStaffHandler:30` — `GetAllAsync()` with no tenant filter (cross-tenant count bug)
- User refresh tokens stored RAW, not hashed (inconsistent with `GuestCancellationToken` which uses SHA-256 hash)
- `StaffController:112` — `BusinessRuleBrokenException` thrown from controller (domain exception in wrong layer)
- `StaffController.GetById` — route param `{resourceId:guid}` but method param `staffId` — binding never resolves

---

## 3. Frontend Architecture — Confirmed Patterns

### Stack
- React 18 + TypeScript + Vite
- Bootstrap 5 (class-based styling, no CSS modules except feature-scoped `.styles.css`)
- FontAwesome icons via `@fortawesome/react-fontawesome`
- `axiosInstance` — single client in `src/services/axiosInstance.ts`, auto-unwraps `response.data`, handles 401 refresh

### Folder Structure
```
src/
  features/[feature]/
    pages/       — route-level components
    components/  — presentational, scoped to feature
    hooks/       — one hook per concern, owns [data, isLoading, error] state
    types/       — TS interfaces mirroring backend DTOs exactly
    schema/      — (optional) form field configs for renderField system
  components/
    layout/      — MainLayout, Sidebar, Header, CustomerLayout, SuperAdminLayout
    ui/          — Alert, Button, Input, Select, StatusPill, Tabs, ConfirmModal, Table, Pagination
  context/
    AuthContext.tsx  — JWT decoded on login, exposes user.role
  services/
    axiosInstance.ts — single axios client
  types/index.ts     — PagedResult<T>, SidebarItem
```

### Auth Context
- `useAuth()` returns `{ user, isAuthenticated, tenantSlug, ... }`
- `user.role` typed as `'TenantAdmin' | 'Manager' | 'Staff' | 'customer' | 'superadmin'`
- Token stored in `sessionStorage.access_token`

### Hook Pattern (consistent across all features)
```ts
const [items, setItems] = useState<T[]>([]);
const [isLoading, setIsLoading] = useState(false);
const [error, setError] = useState<string | null>(null);
// async functions return boolean (true = success)
```

### API Calls Pattern
```ts
// axiosInstance already unwraps response.data
const result = await axiosInstance.get<T>('/endpoint');
// result IS the DTO, not wrapped in { data: ... }
```

### `ConfirmModal` Props (current)
```ts
{ isOpen, title, message, onConfirm, onCancel }
// confirm button is hardcoded "Delete" — needs confirmLabel prop added
```

### `Button` Component Variants
`variant: 'primary' | 'secondary' | 'danger'` — no `success` variant exists yet

### Existing Settings Types (relevant)
```ts
// src/features/settings/types/index.ts
type BookingConfirmationMode = 'Automatic' | 'Manual';  // already exists

interface TenantSettingsDto {
  bookingConfirmationMode: BookingConfirmationMode;
  // ... other fields
}
```

### Existing Booking Type (relevant)
```ts
// src/features/bookings/types/index.ts
interface Booking {
  bookingId: string;
  confirmationMode: string;  // BUG: should be BookingConfirmationMode, currently loose string
  status: BookingStatus;
  // ... other fields
}
type BookingStatus = 'PendingPayment' | 'Pending' | 'Confirmed' | 'Cancelled' | 'Completed' | 'NoShow';
```

---

## 4. The Feature Being Implemented

### Business Context
When `TenantSettings.BookingConfirmationMode = Manual`:
- Customer submits booking → status = `Pending`
- Customer receives "pending approval" email (NOT "confirmed")
- Staff/Admin sees booking in list with `Pending` status
- Staff/Admin clicks booking → drawer opens → "Confirm Booking" button is active
- Staff/Admin confirms → backend transitions status to `Confirmed`
- Customer receives "booking confirmed" email with cancel link

### Backend — ALREADY COMPLETED ✅

All backend changes are done and compiled (0 errors). Files modified:

| File | Change |
|---|---|
| `Domain/Services/Notification/Bookings/IBookingNotificationService.cs` | Added `SendPendingApprovalEmailAsync(booking, businessName, ct)` |
| `Infrastructure/ExternalServices/BookingNotificationService/BookingNotificationService.cs` | Implemented `SendPendingApprovalEmailAsync` — email says "pending approval", no cancel button |
| `Application/Features/Bookings/Commands/CreateBooking/CreateBookingCommandHandler.cs` | Conditional email: `Pending` status → pending approval email; otherwise → confirmation email |
| `Application/Features/Bookings/Commands/ConfirmBooking/ConfirmBookingCommand.cs` | NEW — `record ConfirmBookingCommand(Guid BookingId) : ICommand` |
| `Application/Features/Bookings/Commands/ConfirmBooking/ConfirmBookingCommandHandler.cs` | NEW — loads booking (tenant-scoped), calls `booking.Confirm(userId)`, generates new cancellation token, saves, sends confirmation email |
| `WebApi/Controllers/BookingController.cs` | Added `POST /api/booking/{bookingId}/confirm` → `[Authorize]` → `NoContent()` |

### Frontend — NOT YET STARTED ❌

**Exact files to touch (4 files):**

#### File 1: `src/components/ui/modal/ConfirmModal.tsx`
Add `confirmLabel?: string` prop (default `'Confirm'`). Use it for the confirm button text instead of hardcoded "Delete".

Current confirm button:
```tsx
<button className="btn btn-danger" onClick={onConfirm}>Delete</button>
```
After:
```tsx
<button className="btn btn-danger" onClick={onConfirm}>{confirmLabel ?? 'Confirm'}</button>
```

Also update `ConfirmModalProps` type to include `confirmLabel?: string`.

#### File 2: `src/features/bookings/hooks/useBookings.ts`
Add `confirm` function:
```ts
const confirm = useCallback(async (bookingId: string): Promise<boolean> => {
  setIsLoading(true);
  setError(null);
  try {
    await axiosInstance.post(`/booking/${bookingId}/confirm`);
    await getAll();
    return true;
  } catch (err: any) {
    setError(err?.message || 'Failed to confirm booking.');
    return false;
  } finally {
    setIsLoading(false);
  }
}, [getAll]);
// Add confirm to the return object
```

#### File 3: `src/features/bookings/components/BookingDetailDrawer.tsx`
- Add `onConfirm: (booking: Booking) => void` to `Props` interface
- Import `useAuth` from `../../../context/AuthContext`
- Inside component, call `const { user } = useAuth()`
- Determine if current user can confirm:
  ```ts
  const canConfirm = user?.role === 'TenantAdmin' || user?.role === 'Manager' || user?.role === 'Staff';
  ```
- Determine button state:
  ```ts
  const confirmable = booking.status === 'Pending';
  ```
- Render "Confirm Booking" button in the footer action area (alongside or above the cancel button):
  ```tsx
  {canConfirm && (
    <button
      type="button"
      className="btn btn-success w-100"
      onClick={() => onConfirm(booking)}
      disabled={!confirmable}
    >
      Confirm Booking
    </button>
  )}
  ```
  - When `confirmable` is false (status is not Pending) → button is rendered but `disabled` (greyed out)
  - When `canConfirm` is false (customer role) → button not rendered at all

- Also update `isCancellable` to exclude `Pending` from cancellable statuses OR keep it cancellable — this is a product decision. Based on the user's statement "no cancellation button as it is not approved yet", update `isCancellable`:
  ```ts
  const isCancellable = (status: BookingStatus): boolean =>
    status !== 'Pending' &&        // <-- add this
    status !== 'Cancelled' &&
    status !== 'Completed' &&
    status !== 'NoShow';
  ```

**IMPORTANT:** The drawer's footer currently has the cancel button in its own `div.p-4.border-top`. With the confirm button added, restructure that footer section to show both buttons when applicable. Confirm on top, cancel below (or side by side). Keep consistent spacing.

#### File 4: `src/features/bookings/pages/BookingsPage.tsx`
- Destructure `confirm` from `useBookings()`
- Add state for the confirm flow:
  ```ts
  const [showConfirmApproval, setShowConfirmApproval] = useState(false);
  const [targetConfirmBooking, setTargetConfirmBooking] = useState<Booking | null>(null);
  ```
- Add `openConfirm` callback (mirrors existing `openCancel`):
  ```ts
  const openConfirm = useCallback((booking: Booking) => {
    setTargetConfirmBooking(booking);
    setSelectedBooking(null);
    setShowConfirmApproval(true);
  }, []);
  ```
- Add `handleConfirm` async handler:
  ```ts
  const handleConfirm = async () => {
    if (!targetConfirmBooking) return;
    const ok = await confirm(targetConfirmBooking.bookingId);
    if (ok) {
      setShowConfirmApproval(false);
      setTargetConfirmBooking(null);
      setSuccessMessage('Booking confirmed.');
    }
  };
  ```
- Pass `onConfirm={openConfirm}` to `<BookingDetailDrawer>`
- Add a second `<ConfirmModal>` for the approval action:
  ```tsx
  <ConfirmModal
    isOpen={showConfirmApproval}
    title="Confirm Booking"
    message={`Confirm booking for "${targetConfirmBooking?.guest?.firstName ?? 'this client'}"?`}
    confirmLabel="Confirm Booking"
    onConfirm={handleConfirm}
    onCancel={() => { setShowConfirmApproval(false); setTargetConfirmBooking(null); }}
  />
  ```

---

## 5. Decision Already Made by User

- "No cancellation button as it is not approved yet" → `Pending` status bookings should NOT show the cancel button in `BookingDetailDrawer`
- The Confirm button is always visible to staff/admin roles, disabled when booking is not `Pending`
- `ConfirmModal.confirmLabel` prop needed so the confirm modal doesn't say "Delete"

---

## 6. Continuation Prompt

See section below — copy and use as the opening message in the new session.

---

## 7. Self-Prompt for New Session

```
Read the file c:\Users\Wyrlo\Projects\ApexBooking\HANDOFF.md completely before doing anything else.

Once read, implement the frontend changes described in Section 4 under "Frontend — NOT YET STARTED". 
The backend is already done. Do not touch any backend files.

Implement in this order:
1. ConfirmModal.tsx — add confirmLabel prop
2. useBookings.ts — add confirm function
3. BookingDetailDrawer.tsx — add Confirm Booking button with role check and disabled state
4. BookingsPage.tsx — wire up confirm flow

Before writing any code, re-read the relevant files using the Read tool to get current state.
Do not guess file contents. Follow the architecture rules in CLAUDE.md and apex-booking-client/CLAUDE.md.
Request approval before executing if anything in the handoff document is unclear.
```
