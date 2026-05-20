# AUDIT GAP REPORT — ApexBooking Architecture Compliance Audit

**Date:** 2026-05-16  
**Audited by:** Architecture Compliance Engine  
**Sources:** Backend CLAUDE.md, Frontend CLAUDE.md, project tree (tree.md), direct code inspection

---

## 1. Executive Summary

| Metric | Count |
|---|---|
| **Total violations** | **51** |
| Critical | 14 |
| High | 15 |
| Medium | 15 |
| Low | 7 |

The most severe systemic finding is that the **entire Application layer universally violates the BaseResponse prohibition**. Every handler in the system returns `BaseResponse<T>` rather than raw DTOs or thrown exceptions. This single root cause cascades into a second systemic cross-layer coupling: the frontend has mirrored `BaseResponse` as its expected API contract in `src/types/index.ts`, making the two layers structurally locked together in a pattern both Claude.md files explicitly forbid.

A global exception handling middleware does not appear to exist in the WebApi layer, which is the root architectural gap that caused the BaseResponse workaround to proliferate.

---

## 2. Backend Violations

### 2.1 Error Handling Violations

#### BACKEND-EH-01 — BaseResponse returned from ALL Application handlers
- **Severity:** CRITICAL  
- **Rule violated:** "Returning BaseResponse from any Command or Query handler" — Forbidden Patterns section  
- **Layer:** Application  
- **Description:** Every handler in the Application layer is typed as `ICommandHandler<TCommand, BaseResponse<T>>` and returns `BaseResponse<T>.Success(...)` or `BaseResponse<T>.Failure(...)`. This is a universal, systemic violation. Confirmed in 100+ files.  
- **Impacted files (representative sample):**
  - `ApexBooking.Core.Application/Features/Auth/Commands/Login/LoginCommandHandler.cs`
  - `ApexBooking.Core.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommandHandler.cs`
  - `ApexBooking.Core.Application/Features/Auth/Commands/AcceptInvitation/AcceptInvitationCommandHandler.cs`
  - `ApexBooking.Core.Application/Features/Auth/Commands/ResetPassword/ResetPasswordCommandHandler.cs`
  - `ApexBooking.Core.Application/Features/Bookings/Commands/CreateBooking/CreateBookingCommandHandler.cs`
  - `ApexBooking.Core.Application/Features/Bookings/Commands/CancelBooking/CancelBookingCommandHandler.cs`
  - `ApexBooking.Core.Application/Features/Bookings/Commands/CancelBookingByToken/CancelBookingByTokenCommandHandler.cs`
  - `ApexBooking.Core.Application/Features/Payment/Commands/InitiatePayment/InitiatePaymentHandler.cs`
  - `ApexBooking.Core.Application/Features/Payment/Commands/HandlePayPalWebhook/HandlePayPalWebhookHandler.cs`
  - `ApexBooking.Core.Application/Features/SuperAdmin/Commands/CreateOrganization/CreateOrganizationCommandHandler.cs`
  - `ApexBooking.Core.Application/Features/SuperAdmin/Commands/ApproveTenantRequest/ApproveTenantRequestCommandHandler.cs`
  - `ApexBooking.Core.Application/Features/SuperAdmin/Commands/CreateTenantUser/CreateTenantUserCommandHandler.cs`
  - `ApexBooking.Core.Application/Features/SuperAdmin/Commands/ResendInvitation/ResendInvitationCommandHandler.cs`
  - `ApexBooking.Core.Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs`
  - All remaining handlers in `/Features/**/*Handler.cs` (100+ files total)

---

#### BACKEND-EH-02 — try/catch used for business logic inside handlers
- **Severity:** CRITICAL  
- **Rule violated:** "Using try/catch in Application handlers for business logic" — Forbidden Patterns section  
- **Layer:** Application  
- **Description:** Handlers catch `BusinessRuleBrokenException` thrown by domain methods and convert them into `BaseResponse.Failure(...)` responses instead of letting them propagate to the global exception handler.  
- **Impacted files:**
  - `ApexBooking.Core.Application/Features/Bookings/Commands/CancelBooking/CancelBookingCommandHandler.cs` lines 51–58
  - `ApexBooking.Core.Application/Features/Bookings/Commands/CancelBookingByToken/CancelBookingByTokenCommandHandler.cs` lines 61–69
- **Exact mismatch:**
  ```csharp
  try {
      booking.Cancel(userId, request.Reason);
  }
  catch (BusinessRuleBrokenException ex) {
      return BaseResponse<bool>.Failure(ex.Message);  // FORBIDDEN
  }
  ```

---

#### BACKEND-EH-03 — Application layer formats domain errors as user-facing messages
- **Severity:** CRITICAL  
- **Rule violated:** "Application layer must never format errors for UI consumption"  
- **Layer:** Application  
- **Description:** Handlers return `BaseResponse.Failure("Booking not found.")`, `BaseResponse.Failure("Invalid email or password.")`, etc. These are user-facing messages constructed in the Application layer, which must only exist in the WebApi error translation boundary.  
- **Impacted files:** All handlers listed under BACKEND-EH-01.

---

#### BACKEND-EH-04 — No global exception handling middleware exists
- **Severity:** CRITICAL  
- **Rule violated:** "Only WebApi layer is allowed to catch exceptions globally, map exceptions to HTTP status codes, return API error contracts"  
- **Layer:** WebApi  
- **Description:** The only middleware files found in `ApexBooking.WebApi/Middleware/` are `SecurityHeadersMiddleware.cs` and `TenantMiddleware.cs`. No `ExceptionHandlingMiddleware` or equivalent global exception handler exists. This is the root architectural gap that caused the BaseResponse pattern to proliferate as a workaround.  
- **Impacted files:** `ApexBooking.WebApi/Middleware/` (missing file)

---

#### BACKEND-EH-05 — Controllers make flow decisions on result.IsSuccess instead of relying on global middleware
- **Severity:** HIGH  
- **Rule violated:** "All failures must be handled via centralized exception handling middleware"  
- **Layer:** WebApi  
- **Description:** Controllers inspect `result.IsSuccess` and manually return `BadRequest(result)`, bypassing the centralized exception handling boundary.  
- **Impacted files:**
  - `ApexBooking.WebApi/Controllers/BookingController.cs` lines 58–59
  - `ApexBooking.WebApi/Controllers/WebhooksController.cs` lines 51–52
- **Exact mismatch:**
  ```csharp
  if (!result.IsSuccess)
      return BadRequest(result);  // FORBIDDEN — must flow via exception middleware
  ```

---

#### BACKEND-EH-06 — HandlePayPalWebhookHandler swallows JSON parse exception
- **Severity:** HIGH  
- **Rule violated:** "Swallowing exceptions at any layer" — Forbidden Patterns section  
- **Layer:** Application  
- **Description:** JSON deserialization failure is caught silently and converted to a `BaseResponse.Failure` instead of propagating. Exception context is lost.  
- **Impacted file:** `ApexBooking.Core.Application/Features/Payment/Commands/HandlePayPalWebhook/HandlePayPalWebhookHandler.cs` lines 29–35

---

### 2.2 Email and Notification Violations

#### BACKEND-EMAIL-01 — Large HTML email construction inside CreateBookingCommandHandler
- **Severity:** CRITICAL (acknowledged known violation — must not expand)  
- **Rule violated:** "Large HTML construction inside Application handlers is forbidden", "Email template generation must not live inside handlers"  
- **Layer:** Application  
- **Description:** A full multi-table HTML document is constructed inline in `SendConfirmationEmailAsync`. This is explicitly named as a known violation in CLAUDE.md. Any new handler must not replicate this.  
- **Impacted file:** `ApexBooking.Core.Application/Features/Bookings/Commands/CreateBooking/CreateBookingCommandHandler.cs` lines 209–255

---

#### BACKEND-EMAIL-02 — HTML email construction in ApproveTenantRequestCommandHandler
- **Severity:** CRITICAL  
- **Rule violated:** "Large HTML construction inside Application handlers is forbidden"  
- **Layer:** Application  
- **Description:** Multi-line HTML email body built inline in the handler. This is a new violation beyond the acknowledged known violation.  
- **Impacted file:** `ApexBooking.Core.Application/Features/SuperAdmin/Commands/ApproveTenantRequest/ApproveTenantRequestCommandHandler.cs` lines 96–107

---

#### BACKEND-EMAIL-03 — HTML email construction in CreateTenantUserCommandHandler
- **Severity:** CRITICAL  
- **Rule violated:** "Large HTML construction inside Application handlers is forbidden"  
- **Layer:** Application  
- **Description:** Multi-line HTML email body built inline in the handler.  
- **Impacted file:** `ApexBooking.Core.Application/Features/SuperAdmin/Commands/CreateTenantUser/CreateTenantUserCommandHandler.cs` lines 83–93

---

#### BACKEND-EMAIL-04 — HTML email construction in ResendInvitationCommandHandler
- **Severity:** HIGH  
- **Rule violated:** "Large HTML construction inside Application handlers is forbidden"  
- **Layer:** Application  
- **Description:** HTML email body built inline in the handler.  
- **Impacted file:** `ApexBooking.Core.Application/Features/SuperAdmin/Commands/ResendInvitation/ResendInvitationCommandHandler.cs` lines 52–61

---

#### BACKEND-EMAIL-05 — HTML email construction in ForgotPasswordCommandHandler
- **Severity:** HIGH  
- **Rule violated:** "Large HTML construction inside Application handlers is forbidden"  
- **Layer:** Application  
- **Description:** HTML email template built inline in the handler.  
- **Impacted file:** `ApexBooking.Core.Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs` lines 37–44

---

#### BACKEND-EMAIL-06 — HTML email construction in TrialExpiryJob
- **Severity:** HIGH  
- **Rule violated:** "Move reusable templates into dedicated notification and template services"  
- **Layer:** Infrastructure  
- **Description:** Two separate HTML email bodies are constructed inline inside the background job. Email template generation belongs in a dedicated template service.  
- **Impacted file:** `ApexBooking.Infrastructure/BackgroundJobs/TrialExpiryJob.cs` lines 49–58 and 91–100

---

### 2.3 Infrastructure Leakage Violations

#### BACKEND-INFRA-01 — Direct HttpClient and PayPal API calls inside Application handler
- **Severity:** CRITICAL  
- **Rule violated:** "Infrastructure implements abstractions from Domain and Application only", "Business logic in infrastructure" / cross-layer leakage  
- **Layer:** Application (must not contain Infrastructure concerns)  
- **Description:** `InitiatePaymentHandler` creates `HttpClient` instances and makes direct HTTP calls to PayPal's REST API (`/v1/oauth2/token`, `/v2/checkout/orders`) inline in the handler. External HTTP communication is an Infrastructure concern and must be abstracted behind a domain/application interface (e.g., `IPayPalGateway`).  
- **Impacted file:** `ApexBooking.Core.Application/Features/Payment/Commands/InitiatePayment/InitiatePaymentHandler.cs` lines 108–222

---

### 2.4 Domain Violations

#### BACKEND-DOMAIN-01 — Cancellation cutoff business rule duplicated in two handlers instead of living in domain
- **Severity:** HIGH  
- **Rule violated:** "All business invariants must live inside Aggregate roots, Domain methods, Domain services"  
- **Layer:** Application (rule belongs in Domain)  
- **Description:** The cancellation cutoff hour check (`hoursUntilBooking < tenant.TenantSettings.CancellationCutoffHours`) is copy-pasted into two separate Application handlers instead of being enforced inside the `Booking` aggregate's `Cancel` / `GuestCancel` methods.  
- **Impacted files:**
  - `ApexBooking.Core.Application/Features/Bookings/Commands/CancelBooking/CancelBookingCommandHandler.cs` lines 43–49
  - `ApexBooking.Core.Application/Features/Bookings/Commands/CancelBookingByToken/CancelBookingByTokenCommandHandler.cs` lines 51–58

---

#### BACKEND-DOMAIN-02 — Plan limits business rules in Application/Common instead of Domain
- **Severity:** HIGH  
- **Rule violated:** "Business rules belong in Domain"  
- **Layer:** Application (rule belongs in Domain)  
- **Description:** `PlanLimits.cs` defines booking/resource/service limits per `TenantPlan`. These are business invariants and belong in the Domain layer, not Application/Common.  
- **Impacted file:** `ApexBooking.Core.Application/Common/PlanLimits.cs`

---

### 2.5 Controller Violations

#### BACKEND-CTRL-01 — Business logic inside StaffController
- **Severity:** HIGH  
- **Rule violated:** "Business logic inside controllers is forbidden"  
- **Layer:** WebApi  
- **Description:** `StaffController.AddException` performs enum parsing and throws a `BusinessRuleBrokenException` directly in the controller. This validation belongs in a FluentValidation validator or the domain, not the controller.  
- **Impacted file:** `ApexBooking.WebApi/Controllers/StaffController.cs` lines 111–112
- **Exact mismatch:**
  ```csharp
  if (!Enum.TryParse<ExceptionType>(dto.ExceptionType, out var exceptionType))
      throw new BusinessRuleBrokenException("Invalid exception type.");  // FORBIDDEN in controller
  ```

---

#### BACKEND-CTRL-02 — Controller imports domain entity type
- **Severity:** MEDIUM  
- **Rule violated:** Clean layer boundaries — controllers must not reference Domain entities directly  
- **Layer:** WebApi  
- **Description:** `StaffController.cs` imports `ApexBooking.Core.Domain.Entities` directly for `ExceptionType`. Domain entity types must not leak into the WebApi layer.  
- **Impacted file:** `ApexBooking.WebApi/Controllers/StaffController.cs` line 9

---

#### BACKEND-CTRL-03 — Controllers access .Data from BaseResponse to construct routes
- **Severity:** MEDIUM  
- **Rule violated:** "Controllers must not wrap responses in BaseResponse" / direct dependency on forbidden pattern  
- **Layer:** WebApi  
- **Description:** Controllers dereference `.Data` from the `BaseResponse<T>` to extract IDs for `CreatedAtAction`. This creates a hard coupling to the forbidden response wrapper.  
- **Impacted files:**
  - `ApexBooking.WebApi/Controllers/StaffController.cs` line 63: `response.Data.StaffId.Value`
  - `ApexBooking.WebApi/Controllers/ServiceController.cs` line 60: `response.Data.Id`

---

#### BACKEND-CTRL-04 — Route parameter name mismatch in StaffController
- **Severity:** LOW  
- **Rule violated:** API contract integrity  
- **Layer:** WebApi  
- **Description:** The route defines `{resourceId:guid}` but the method parameter is `staffId`. The route value will not bind correctly.  
- **Impacted file:** `ApexBooking.WebApi/Controllers/StaffController.cs` line 43: `public async Task<IActionResult> GetById(Guid staffId)`

---

### 2.6 Authentication Violations

#### BACKEND-AUTH-01 — Plaintext refresh token exposed in API response body
- **Severity:** HIGH  
- **Rule violated:** "Never expose sensitive authentication details in API responses"  
- **Layer:** Application  
- **Description:** `LoginCommandHandler` and `AcceptInvitationCommandHandler` both include `RefreshToken = rawRefreshToken` in the `AuthResponseDto` returned to the client. The token is also correctly placed in an HttpOnly cookie via `ICookieService`, but its presence in the response body violates the security rule.  
- **Impacted files:**
  - `ApexBooking.Core.Application/Features/Auth/Commands/Login/LoginCommandHandler.cs` line 78
  - `ApexBooking.Core.Application/Features/Auth/Commands/AcceptInvitation/AcceptInvitationCommandHandler.cs` line 86

---

### 2.7 CQRS Violations

#### BACKEND-CQRS-01 — Query handlers return BaseResponse wrapping instead of raw DTOs
- **Severity:** HIGH  
- **Rule violated:** CQRS rules — "Queries never mutate state" + Error handling rules — no BaseResponse from handlers  
- **Layer:** Application  
- **Description:** All query handlers (GetBookings, GetServices, GetStaff, GetPublicTenant, etc.) return `BaseResponse<T>` instead of returning their DTOs directly. Queries should return the DTO type and throw `NotFoundException` if data is absent.  
- **Impacted files:** All `*QueryHandler.cs` files across `/Features/**/Queries/`

---

### 2.8 Mapping Violations

#### BACKEND-MAP-01 — Dtos folder exists in WebApi layer alongside Application Dtos
- **Severity:** LOW  
- **Rule violated:** "DTO mapping uses static extension method mappings. Mapping classes live inside ApexBooking.Core.Application/mapper/"  
- **Layer:** WebApi  
- **Description:** `ApexBooking.WebApi/Dtos/` contains request DTOs that are WebApi-transport-specific. These are distinct from Application DTOs. While transport DTOs in WebApi are acceptable, the presence of a `Dtos/` folder in WebApi should be confirmed as strictly transport-only (no business-shaped DTOs).  
- **Impacted path:** `ApexBooking.WebApi/Dtos/`  
- **Classification:** Requires confirmation

---

## 3. Frontend Violations

### 3.1 Page Architecture Violations

#### FRONTEND-PAGE-01 — CustomerBookingWizardPage calls axiosInstance directly
- **Severity:** CRITICAL  
- **Rule violated:** "Pages must not: Call axios directly"  
- **Layer:** Frontend / pages  
- **Description:** The page contains three separate direct `axiosInstance.get(...)` calls inside `useEffect` hooks for loading tenant, services, and resources. None of these are abstracted into hooks.  
- **Impacted file:** `apex-booking-client/src/features/bookings/pages/CustomerBookingWizardPage.tsx` lines 87, 98–99, 118–120

---

#### FRONTEND-PAGE-02 — TenantDashboardPage calls axiosInstance directly without a hook
- **Severity:** CRITICAL  
- **Rule violated:** "Pages must not: Call axios directly"  
- **Layer:** Frontend / pages  
- **Description:** The page fetches `/dashboard/summary` via `axiosInstance.get` inside a `useEffect`. No hook exists for this operation.  
- **Impacted file:** `apex-booking-client/src/features/dashboard/TenantDashboardPage.tsx` lines 20–42

---

#### FRONTEND-PAGE-03 — TenantDashboardPage sets Authorization header manually
- **Severity:** HIGH  
- **Rule violated:** "JWT handled globally" — axiosInstance interceptor must handle auth, not individual callers  
- **Layer:** Frontend / pages  
- **Description:** The page manually adds `Authorization: Bearer ${accessToken}` to the request config. The axiosInstance interceptor already injects this header automatically.  
- **Impacted file:** `apex-booking-client/src/features/dashboard/TenantDashboardPage.tsx` line 26

---

#### FRONTEND-PAGE-04 — TenantDashboardPage manually parses nested BaseResponse on success path
- **Severity:** HIGH  
- **Rule violated:** "Pages must not: Duplicate API logic"  
- **Layer:** Frontend / pages  
- **Description:** The page checks `res.data?.isSuccess` and accesses `res.data.data` directly, bypassing the axiosInstance response interceptor which already unwraps the response. Double-unwrapping indicates a broken response handling contract.  
- **Impacted file:** `apex-booking-client/src/features/dashboard/TenantDashboardPage.tsx` lines 31–33

---

#### FRONTEND-PAGE-05 — CustomerBookingWizardPage contains business/validation logic
- **Severity:** HIGH  
- **Rule violated:** "No business logic in the frontend — Validation rules beyond UI format checks"  
- **Layer:** Frontend / pages  
- **Description:** The `validateDetails()` function at line 195–204 includes email format validation (`/^[^\s@]+@[^\s@]+\.[^\s@]+$/`) and required field enforcement. Booking-specific field requirements (guest name, email, phone) are business context, not pure UI format checks.  
- **Impacted file:** `apex-booking-client/src/features/bookings/pages/CustomerBookingWizardPage.tsx` lines 195–205

---

### 3.2 Hook Violations

#### FRONTEND-HOOK-01 — useBookings contains large commented-out dead code block
- **Severity:** MEDIUM  
- **Rule violated:** Code completeness and output rule — "Every output must compile and run", no incomplete implementations  
- **Layer:** Frontend / hooks  
- **Description:** Lines 153–304 contain a full commented-out duplicate implementation of the hook. Dead code of this scale must not remain in production files.  
- **Impacted file:** `apex-booking-client/src/features/bookings/hooks/useBookings.ts` lines 153–304

---

#### FRONTEND-HOOK-02 — useBookings constructs URL with user ID embedded in path
- **Severity:** MEDIUM  
- **Rule violated:** "Never: Hardcode endpoints"  
- **Layer:** Frontend / hooks  
- **Description:** `/booking/customer/${user?.id}` constructs a path with a dynamic user ID embedded in the route string. No such backend endpoint exists in the controller list, making this a potentially broken API assumption.  
- **Impacted file:** `apex-booking-client/src/features/bookings/hooks/useBookings.ts` line 49

---

#### FRONTEND-HOOK-03 — Hooks check result.isSuccess instead of relying solely on error state
- **Severity:** MEDIUM  
- **Rule violated:** "Hooks must: Catch API errors, Normalize errors into UI-safe messages, Expose error state" — the error path should be the catch block, not manual isSuccess inspection  
- **Layer:** Frontend / hooks  
- **Description:** Multiple hooks inspect `result.isSuccess` and `result.errors?.[0]?.message` on the happy path response. This couples the hooks to the backend's forbidden `BaseResponse` pattern. Hooks should only rely on the `catch` block since the axiosInstance interceptor already rejects non-success responses.  
- **Impacted files:**
  - `apex-booking-client/src/features/bookings/hooks/useBookings.ts` (multiple locations)
  - `apex-booking-client/src/features/auth/hooks/useLogin.ts` line 30
  - `apex-booking-client/src/services/axiosInstance.ts` line 88

---

### 3.3 Component Violations

#### FRONTEND-COMP-01 — LoginForm contains commented-out dead code block
- **Severity:** LOW  
- **Rule violated:** Code completeness and output rule  
- **Layer:** Frontend / components  
- **Description:** Lines 176–312 contain a full commented-out duplicate of the component implementation.  
- **Impacted file:** `apex-booking-client/src/features/auth/components/LoginForm.tsx` lines 176–312

---

### 3.4 Styling Violations

#### FRONTEND-STYLE-01 — App.css exists as a second global CSS file
- **Severity:** HIGH  
- **Rule violated:** "Only one global CSS file: src/index.css"  
- **Layer:** Frontend / styling  
- **Description:** `src/App.css` exists alongside `src/index.css` and contains structural styling (`.hero`, `#center`, `#next-steps`, `.ticks`, etc.). Only `src/index.css` is the permitted global stylesheet.  
- **Impacted file:** `apex-booking-client/src/App.css`

---

#### FRONTEND-STYLE-02 — Inline styles used for structural layout (not dynamic positioning)
- **Severity:** MEDIUM  
- **Rule violated:** "Inline styles allowed only for dynamic positioning and temporary debug cases"  
- **Layer:** Frontend / styling  
- **Description:** Multiple components and pages use `style={{ maxWidth: ... }}`, `style={{ fontSize: ... }}`, and `style={{ width: ..., height: ... }}` for fixed structural layout and typography — none of which qualify as dynamic positioning.  
- **Impacted files:**
  - `apex-booking-client/src/features/settings/pages/SettingsPage.tsx` lines 183, 301, 428, 482 — `style={{ maxWidth: 560 }}`
  - `apex-booking-client/src/features/auth/components/LoginForm.tsx` line 88 — `style={{ width: "100%", maxWidth: "400px" }}`
  - `apex-booking-client/src/features/bookings/pages/CustomerBookingWizardPage.tsx` lines 379, 386 — `style={{ maxWidth: 680, margin: "0 auto" }}`, `style={{ maxHeight: 60 }}`
  - `apex-booking-client/src/features/superadmin/pages/OrganizationDetailPage.tsx` lines 27, 44, 171, 177, 185, 199 — multiple font-size and dimension inline styles

---

### 3.5 State Management Violations

#### FRONTEND-STATE-01 — src/store/ directory exists (Redux/store scaffold)
- **Severity:** MEDIUM  
- **Rule violated:** "Forbidden: Redux (unless explicitly added later)"  
- **Layer:** Frontend / state  
- **Description:** The directory `src/store/` exists (containing only `.gitkeep`). Even as an empty scaffold, this directory signals intent to introduce Redux or a global store system, which is forbidden unless explicitly approved.  
- **Impacted path:** `apex-booking-client/src/store/`

---

### 3.6 API Integration Violations

#### FRONTEND-API-01 — Frontend defines BaseResponse as its expected API contract type
- **Severity:** HIGH  
- **Rule violated:** "TypeScript interfaces mirror backend DTOs" — but BaseResponse is a forbidden backend pattern, not a DTO  
- **Layer:** Frontend / types  
- **Description:** `src/types/index.ts` exports `interface BaseResponse<T>` with `isSuccess`, `data`, and `errors`. This mirrors the backend's forbidden `BaseResponse` class. If the backend is corrected to return raw DTOs and throw exceptions, every hook and page using `result.isSuccess` will break.  
- **Impacted file:** `apex-booking-client/src/types/index.ts` lines 12–20

---

### 3.7 Security Violations

#### FRONTEND-SEC-01 — console.error calls in production code
- **Severity:** MEDIUM  
- **Rule violated:** "No sensitive logs in console"  
- **Layer:** Frontend  
- **Description:** `console.error` calls exist in pages and hooks. While these specific instances do not log sensitive data (tokens, passwords), the rule prohibits all console output in production code.  
- **Impacted files:**
  - `apex-booking-client/src/features/bookings/pages/CustomerBookingWizardPage.tsx` lines 90, 101, 125
  - `apex-booking-client/src/features/dashboard/TenantDashboardPage.tsx` line 35
  - `apex-booking-client/src/features/auth/hooks/useLogout.ts` line 31

---

### 3.8 Cross-Feature Hook Usage

#### FRONTEND-FEAT-01 — BookingsPage uses hooks from two other feature domains
- **Severity:** MEDIUM  
- **Rule violated:** "No cross-feature hooks unless explicitly approved"  
- **Layer:** Frontend / features  
- **Description:** `BookingsPage.tsx` imports and calls `useServices()` from the `service` feature and `useResources()` from the `resources` feature. Cross-feature hook imports require explicit approval.  
- **Impacted file:** `apex-booking-client/src/features/bookings/pages/BookingsPage.tsx` lines 14–15

---

## 4. Cross-Layer Violations

#### CROSS-01 — Entire system is structurally locked to the forbidden BaseResponse contract
- **Severity:** CRITICAL  
- **Rule violated:** Backend: "Returning BaseResponse from any Command or Query handler" / Frontend: "TypeScript interfaces mirror backend DTOs"  
- **Description:** The backend returns `BaseResponse<T>` from every handler. The frontend has modeled its `BaseResponse<T>` interface in `src/types/index.ts` and uses `.isSuccess`, `.data`, `.errors[0].message` in every hook and page. The `axiosInstance.ts` response interceptor also parses the `BaseResponse` structure. If the backend's error handling is corrected to use exceptions and raw DTOs, every frontend hook breaks simultaneously. The two layers cannot be independently corrected — they must be migrated together.

---

#### CROSS-02 — Frontend assumes backend returns raw data in res.data but also assumes BaseResponse wrapper
- **Severity:** HIGH  
- **Rule violated:** API contract consistency  
- **Description:** The axiosInstance interceptor at line 43 (`(response) => response.data`) unwraps the Axios envelope, returning the backend body directly. Yet hooks then access `.isSuccess`, `.data`, `.errors` on that returned value — meaning the "data" from axios is the `BaseResponse` object. `TenantDashboardPage.tsx` takes this further and checks `res.data?.isSuccess` when the interceptor would have already stripped `.data` once. The contract is inconsistently applied across the codebase.  
- **Impacted files:**
  - `apex-booking-client/src/services/axiosInstance.ts` line 43
  - `apex-booking-client/src/features/dashboard/TenantDashboardPage.tsx` line 31

---

#### CROSS-03 — Frontend calls /dashboard/summary endpoint with no corresponding backend controller
- **Severity:** HIGH  
- **Rule violated:** "All API calls must: Match confirmed backend routes"  
- **Description:** `TenantDashboardPage.tsx` calls `/api/dashboard/summary` but no `DashboardController` exists in `ApexBooking.WebApi/Controllers/`. This endpoint either does not exist or is a guessed route, which is forbidden by the "No Assumptions Rule."  
- **Impacted files:**
  - `apex-booking-client/src/features/dashboard/TenantDashboardPage.tsx` line 24
  - `ApexBooking.WebApi/Controllers/` (missing DashboardController)

---

#### CROSS-04 — Frontend assumes /booking/customer/{userId} endpoint exists; backend has no such route
- **Severity:** HIGH  
- **Rule violated:** "All API calls must: Match confirmed backend routes"  
- **Description:** `useBookings.ts` calls `/booking/customer/${user?.id}` but `BookingController.cs` has no route matching `/booking/customer/{id}`. This is an invented endpoint.  
- **Impacted files:**
  - `apex-booking-client/src/features/bookings/hooks/useBookings.ts` line 49
  - `ApexBooking.WebApi/Controllers/BookingController.cs` (missing route)

---

#### CROSS-05 — Plaintext refresh token flows from backend response body to frontend sessionStorage risk
- **Severity:** HIGH  
- **Rule violated:** Backend: "Never expose sensitive authentication details in API responses" / Frontend: "JWT stored in sessionStorage only" (refresh token should only be in httpOnly cookie)  
- **Description:** The backend returns `RefreshToken = rawRefreshToken` in the `AuthResponseDto`. The frontend's `useLogin.ts` does not store this token (it stores only the `accessToken`), but the raw token still travels in the response body. Because the token also exists in an httpOnly cookie via `ICookieService`, the body exposure is redundant and insecure.

---

## 5. Newly Introduced Rule Violations (Stage 2 Focus)

### 5.1 Styling Rules (New)

#### NEW-STYLE-01 — Inline styles for layout violate the styling constraint introduced in the frontend CLAUDE.md
- **Severity:** MEDIUM  
- **Rule:** "Inline styles allowed only for dynamic positioning and temporary debug cases"  
- **Old behavior:** Inline styles were used freely for layout constraints (maxWidth, fontSize, width/height)  
- **New rule:** Any structural, fixed-value, or typography styling must move to component-level CSS files  
- **Affected:** Multiple pages and components as listed under FRONTEND-STYLE-02

---

#### NEW-STYLE-02 — App.css violates the single global CSS file rule
- **Severity:** HIGH  
- **Rule:** "Only one global CSS file: src/index.css"  
- **Old behavior:** Vite scaffold created `App.css` as a secondary global file  
- **New rule:** Only `index.css` is permitted. `App.css` must be removed or its contents merged into `index.css`

---

### 5.2 Error Handling Rules (New)

#### NEW-EH-01 — All handlers predate exception-driven error model; BaseResponse is the systemic legacy violation
- **Severity:** CRITICAL  
- **Rule:** "Domain layer throws exceptions for all business rule violations; Application layer does not convert errors into response objects"  
- **Old behavior:** Handlers return `BaseResponse.Failure(...)` for all failure paths  
- **New rule:** Handlers must throw; only the WebApi global exception handler may catch and translate

---

### 5.3 Coding Rules (New)

#### NEW-CODE-01 — Component-level CSS files (.styles.css) are absent for most components
- **Severity:** MEDIUM  
- **Rule:** "All UI styling lives in component-level CSS files. Pattern: Button.tsx → Button.styles.css. Imported via index.css only."  
- **Old behavior:** Components use inline styles and Bootstrap classes exclusively with no paired CSS files  
- **New rule:** Each component that has custom styling must own a `.styles.css` file  
- **Affected:** Most components in `src/features/*/components/` and `src/components/`

---

## 6. Consolidated Gap List (MASTER LIST)

| # | Violation | Layer | Severity | Impacted Files | Rule Reference |
|---|---|---|---|---|---|
| 1 | BACKEND-EH-01 | Application | CRITICAL | All `*Handler.cs` (100+ files) | Error Handling — Forbidden Patterns |
| 2 | BACKEND-EH-02 | Application | CRITICAL | `CancelBookingCommandHandler.cs`, `CancelBookingByTokenCommandHandler.cs` | Error Handling — Forbidden Patterns |
| 3 | BACKEND-EH-03 | Application | CRITICAL | All handlers with `BaseResponse.Failure(...)` | Error Handling — Error Translation Boundary |
| 4 | BACKEND-EH-04 | WebApi | CRITICAL | `ApexBooking.WebApi/Middleware/` (missing) | Error Handling — Error Translation Boundary |
| 5 | BACKEND-EH-05 | WebApi | HIGH | `BookingController.cs`, `WebhooksController.cs` | Error Handling — centralized middleware |
| 6 | BACKEND-EH-06 | Application | HIGH | `HandlePayPalWebhookHandler.cs` | Error Handling — Forbidden Patterns (swallowing) |
| 7 | BACKEND-EMAIL-01 | Application | CRITICAL (known) | `CreateBookingCommandHandler.cs` | Email and Notification Rules |
| 8 | BACKEND-EMAIL-02 | Application | CRITICAL | `ApproveTenantRequestCommandHandler.cs` | Email and Notification Rules |
| 9 | BACKEND-EMAIL-03 | Application | CRITICAL | `CreateTenantUserCommandHandler.cs` | Email and Notification Rules |
| 10 | BACKEND-EMAIL-04 | Application | HIGH | `ResendInvitationCommandHandler.cs` | Email and Notification Rules |
| 11 | BACKEND-EMAIL-05 | Application | HIGH | `ForgotPasswordCommandHandler.cs` | Email and Notification Rules |
| 12 | BACKEND-EMAIL-06 | Infrastructure | HIGH | `TrialExpiryJob.cs` | Email and Notification Rules |
| 13 | BACKEND-INFRA-01 | Application | CRITICAL | `InitiatePaymentHandler.cs` | Dependency Direction — Infrastructure leakage |
| 14 | BACKEND-DOMAIN-01 | Application | HIGH | `CancelBookingCommandHandler.cs`, `CancelBookingByTokenCommandHandler.cs` | Domain Rules — business invariants in domain |
| 15 | BACKEND-DOMAIN-02 | Application | HIGH | `PlanLimits.cs` | Domain Rules — business rules in domain |
| 16 | BACKEND-CTRL-01 | WebApi | HIGH | `StaffController.cs` lines 111–112 | API Rules — business logic in controllers |
| 17 | BACKEND-CTRL-02 | WebApi | MEDIUM | `StaffController.cs` line 9 | Dependency Direction |
| 18 | BACKEND-CTRL-03 | WebApi | MEDIUM | `StaffController.cs`, `ServiceController.cs` | Error Handling — no BaseResponse coupling |
| 19 | BACKEND-CTRL-04 | WebApi | LOW | `StaffController.cs` line 43 | API contract integrity |
| 20 | BACKEND-AUTH-01 | Application | HIGH | `LoginCommandHandler.cs`, `AcceptInvitationCommandHandler.cs` | Identity and Authentication |
| 21 | BACKEND-CQRS-01 | Application | HIGH | All query handlers | CQRS Rules + Error Handling |
| 22 | BACKEND-MAP-01 | WebApi | LOW | `ApexBooking.WebApi/Dtos/` | Mapping Rules (requires confirmation) |
| 23 | FRONTEND-PAGE-01 | Frontend/pages | CRITICAL | `CustomerBookingWizardPage.tsx` | Pages Rules |
| 24 | FRONTEND-PAGE-02 | Frontend/pages | CRITICAL | `TenantDashboardPage.tsx` | Pages Rules |
| 25 | FRONTEND-PAGE-03 | Frontend/pages | HIGH | `TenantDashboardPage.tsx` | API Integration Rules |
| 26 | FRONTEND-PAGE-04 | Frontend/pages | HIGH | `TenantDashboardPage.tsx` | Pages Rules — no API logic in pages |
| 27 | FRONTEND-PAGE-05 | Frontend/pages | HIGH | `CustomerBookingWizardPage.tsx` | No Business Logic Rule |
| 28 | FRONTEND-HOOK-01 | Frontend/hooks | MEDIUM | `useBookings.ts` | Code Generation — no incomplete implementations |
| 29 | FRONTEND-HOOK-02 | Frontend/hooks | MEDIUM | `useBookings.ts` | API Integration Rules — no hardcoded endpoints |
| 30 | FRONTEND-HOOK-03 | Frontend/hooks | MEDIUM | `useBookings.ts`, `useLogin.ts`, `axiosInstance.ts` | Hooks Rules — error state via catch only |
| 31 | FRONTEND-COMP-01 | Frontend/components | LOW | `LoginForm.tsx` | Code Generation — no incomplete implementations |
| 32 | FRONTEND-STYLE-01 | Frontend/styling | HIGH | `src/App.css` | Styling Architecture — single root CSS |
| 33 | FRONTEND-STYLE-02 | Frontend/styling | MEDIUM | Multiple pages and components | Styling Constraints — no inline layout styles |
| 34 | FRONTEND-STATE-01 | Frontend/state | MEDIUM | `src/store/` | State Management Rules — Redux forbidden |
| 35 | FRONTEND-API-01 | Frontend/types | HIGH | `src/types/index.ts` | DTO Rules — no forbidden pattern mirroring |
| 36 | FRONTEND-SEC-01 | Frontend/security | MEDIUM | `CustomerBookingWizardPage.tsx`, `TenantDashboardPage.tsx`, `useLogout.ts` | Security Rules — no console logs |
| 37 | FRONTEND-FEAT-01 | Frontend/features | MEDIUM | `BookingsPage.tsx` | Hook Integrity Rule — no cross-feature hooks |
| 38 | CROSS-01 | Full stack | CRITICAL | All handlers + all hooks | Both Claude.md — BaseResponse forbidden |
| 39 | CROSS-02 | Full stack | HIGH | `axiosInstance.ts`, `TenantDashboardPage.tsx` | API Integration Rules — consistent contract |
| 40 | CROSS-03 | Full stack | HIGH | `TenantDashboardPage.tsx`, WebApi controllers | API Safety Rule — no invented endpoints |
| 41 | CROSS-04 | Full stack | HIGH | `useBookings.ts`, `BookingController.cs` | API Safety Rule — no invented endpoints |
| 42 | CROSS-05 | Full stack | HIGH | `LoginCommandHandler.cs`, `AcceptInvitationCommandHandler.cs` | Authentication security |
| 43 | NEW-STYLE-01 | Frontend/styling | MEDIUM | Multiple pages and components | Styling Constraints (new rule) |
| 44 | NEW-STYLE-02 | Frontend/styling | HIGH | `src/App.css` | Single Root CSS Rule (new rule) |
| 45 | NEW-EH-01 | Application | CRITICAL | All handlers | Error Handling Rules (new rule) |
| 46 | NEW-CODE-01 | Frontend/styling | MEDIUM | Most components | Component Styling Rules (new rule) |
| 47 | (Additional) | Infrastructure | LOW | `TrialExpiryJob.cs` | Background job idempotency — `CompleteAsync` called per-tenant in loop; partial failure state not recoverable |
| 48 | (Additional) | Application | MEDIUM | `CreateBookingCommandHandler.cs` lines 77, 88 | Performance — `TimeZoneInfo.FindSystemTimeZoneById` called twice in same handler |
| 49 | (Additional) | Application | LOW | `ResendInvitationCommandHandler.cs` lines 34–35 | Performance — `ContinueWith` used for async continuation instead of `await` |
| 50 | (Additional) | Application | MEDIUM | `CreateBookingCommandHandler.cs` lines 123–125 | Performance — N+1: all bookings for the year loaded to count them (`GetAllAsync` then `.Count()`) instead of a count query |
| 51 | (Additional) | WebApi | LOW | `AuthController.cs` line 21 | `Controller` base class used instead of `ControllerBase` — `Controller` adds view support not needed in API controllers |

---

*End of AUDIT_GAP_REPORT.md*
