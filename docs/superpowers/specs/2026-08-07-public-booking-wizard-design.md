# Public Booking Wizard: UI + Backend Integration

## Context

`BookingsController.cs` already exposes the full flow (now slug-prefixed per the prior routing spec):

- `GET /api/public/{slug}/bookings/branches`
- `GET /api/public/{slug}/bookings/branches/{branchId}/services`
- `GET /api/public/{slug}/bookings/branches/{branchId}/services/{serviceId}/staff`
- `GET /api/public/{slug}/bookings/branches/{branchId}/availability?staffId&serviceId&date`
- `POST /api/public/{slug}/bookings/initiate`
- `GET /api/public/bookings/status/{ticketToken}` (un-prefixed by design, resolves tenant from the signed ticket token)

The route (`/:slug/book`) and the payment-status polling hook (`useBookingPaymentStatus`) already exist from prior work. This spec builds the actual wizard UI and wires it to these five step endpoints plus the existing status poll.

## Decisions (confirmed with user)

- Single-page wizard, step state held in memory (not one route per step). Refreshing restarts the flow.
- Branch step auto-skips when the tenant has exactly one branch. Same reasoning extends to the Staff step: auto-skip when exactly one staff member qualifies for the chosen service at the chosen branch.
- New dedicated public layout (`PublicBookingLayout`) — branded, mobile-first, no dashboard chrome — built from the existing Card/Button/FormGroup primitives, not a new design system.
- Payment: show the PayMongo QR code inline + a "Pay Now" button opening the checkout URL in a new tab. The booking tab stays open and keeps polling via the existing `useBookingPaymentStatus` hook.

## Data layer (LocalFlow)

### Interfaces (`src/interfaces/publicBooking/`)

Mirror the backend DTOs field-for-field (camelCase wire, matching the existing `IPublicBookingStatus` convention):

```ts
// IPublicBranch.ts — mirrors BranchSummary
interface IPublicBranch {
  branchId: string
  branchName: string
  street: string
  barangay: string | null
  city: string
  province: string
  zipCode: string
  timeZoneId: string
}

// IPublicService.ts — mirrors ServiceCatalogSummary
interface IPublicService {
  serviceId: string
  name: string
  description: string | null
  durationMinutes: number
  bufferBeforeMinutes: number
  bufferAfterMinutes: number
  price: number
  currencyCode: string
  minAdvanceBookingHoursOverride: number | null
  isActive: boolean
  createdAt: string
}

// IPublicStaff.ts — mirrors BookableStaffSummary
interface IPublicStaff {
  tenantMemberId: string
  fullName: string
  customJobTitle: string | null
  photoUrl: string | null
}

// IPublicSlot.ts — mirrors AvailableSlotResponse
interface IPublicSlot {
  timeString: string // display value, e.g. "09:00 AM"
  rawTime: string // "HH:mm:ss", submitted as ScheduledStartTime
}

// IInitiateBookingValues.ts — mirrors InitiateBookingCommand
interface IInitiateBookingValues {
  branchId: string
  staffId: string
  serviceId: string
  scheduledDate: string // "yyyy-MM-dd"
  scheduledStartTime: string // "HH:mm:ss", from IPublicSlot.rawTime
  customerFirstName: string
  customerLastName: string
  customerEmail: string
  customerPhone: string
  customerNotes: string | null
}

// IBookingInitiationResult.ts — mirrors BookingInitiationResult
interface IBookingInitiationResult {
  bookingId: string
  bookingReference: string
  requiresPayment: boolean
  amountToPay: number
  payMongoQrCodeUrl: string | null
  payMongoCheckoutUrl: string | null
  ticketToken: string
}
```

### Service (`publicBookingService.ts`)

Adds five functions alongside the existing `getBookingStatus`, all through `publicClient`, all taking `slug` as the first argument:

```ts
getPublicBranches(slug): Promise<IPublicBranch[]>
getPublicServices(slug, branchId): Promise<IPublicService[]>
getPublicStaff(slug, branchId, serviceId): Promise<IPublicStaff[]>
getPublicAvailability(slug, branchId, staffId, serviceId, date): Promise<IPublicSlot[]>
initiateBooking(slug, values): Promise<IBookingInitiationResult>
```

### Wizard hook (`hooks/usePublicBookingWizard.ts`)

Owns the whole flow's state so `PublicBookingPage.tsx` and step components stay presentation-only:

- `step`: `'branch' | 'service' | 'staff' | 'schedule' | 'confirm' | 'payment' | 'success'`
- Selections: `branch`, `service`, `staff`, `date`, `slot`
- Per-step fetched lists + loading/error state
- `goNext()` / `goBack()` — `goNext()` from Branch auto-advances past Staff too if the fetched staff list resolves to exactly one entry (same skip logic, applied once the list is known rather than guessed upfront)
- `submit(contactValues)` → calls `initiateBooking`, stores the `IBookingInitiationResult`, transitions to `payment` or `success` based on `requiresPayment`
- On mount, fetches branches; if exactly one, auto-selects it and starts the visible flow at Service.

## Components

### `layouts/PublicBookingLayout.tsx`

Centered single-column container, LocalFlow logo linking nowhere actionable (not a dashboard), and a step-progress indicator. Progress indicator length adapts to whichever steps are actually visible (Branch/Staff may be skipped).

### `components/publicBooking/`

- `BookingProgressSteps.tsx` — the step indicator, takes the current step + the resolved visible-step list.
- `BranchStep.tsx` — list of branches as selectable cards (name + address).
- `ServiceStep.tsx` — list of services (name, duration, price).
- `StaffStep.tsx` — list of staff (photo, name, job title).
- `ScheduleStep.tsx` — native `<input type="date" min={today}>`, then a grid of time buttons from the fetched slots for that date.
- `ConfirmStep.tsx` — read-only summary of branch/service/staff/date/time + contact form (first name, last name, email, phone, optional notes) + Submit. Client-side validation mirrors `InitiateBookingCommand`'s implicit requirements (all contact fields required except notes).
- `PaymentStep.tsx` — renders the QR code image + "Pay Now" button (new tab) when `requiresPayment`; internally uses `useBookingPaymentStatus(ticketToken)` and calls a `onConfirmed` callback once status leaves `PendingPayment`.
- `SuccessStep.tsx` — booking reference + recap, for both the no-payment-required and payment-confirmed paths.

### `pages/public/PublicBookingPage.tsx`

Replaces the current placeholder. Reads `slug` from the route param, drives `usePublicBookingWizard(slug)`, renders `PublicBookingLayout` wrapping whichever step component is active. A top-level error state (e.g. invalid slug → 400 from the branches call) replaces the whole wizard with a friendly "This booking page isn't available" message instead of a broken step.

## Error handling

- Each step's fetch failure shows an inline retry state within that step (matches existing dashboard hook conventions: `{ data, isLoading, error, refetch }`), except the initial branches fetch, which is treated as page-level (no tenant context = nothing else can work).
- `initiateBooking` failure (e.g. slot taken between availability check and submit) surfaces via a toast-equivalent inline alert on the Confirm step, and does **not** advance the step — customer can pick a different time.

## Out of scope

- Payment-required policy display before Confirm (e.g. "a deposit is required") — `ServiceCatalogSummary`/`IPublicService` doesn't carry payment-policy info; the customer only learns payment is required after submitting. Adding a pre-submit indicator would need a new public endpoint exposing `PaymentPolicy` and is a separate follow-up if wanted.
- Guest booking cancellation/rescheduling UI — not part of this flow.
- Any change to the backend — this is a pure frontend integration against the existing five endpoints + the existing status poll.

## Testing

- Manual: run against a seeded tenant with multiple branches and one with a single branch, confirm the branch-skip behaves; a service with only one qualified staff member, confirm the staff-skip behaves; a service requiring payment vs. one that doesn't, confirm both the Payment and direct-Success paths; submit with an already-taken slot (book the same slot twice) to confirm the Confirm-step error path.
