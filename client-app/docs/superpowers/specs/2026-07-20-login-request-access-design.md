# ApexBooking Login & Request Access â€” Design Spec

Status: Approved (source: user-authored feature prompt, treated as final spec,
refined through clarifying questions below)
Scope: Login page and Request Access page only. No auth, no APIs, no
dashboard, no forgot-password page, no other future pages.

## Source of Truth

Content, fields, and behavior are defined by the feature prompt supplied by
the user (Login form fields, Request Access form fields mapped to the
`RequestAccessCommand` record, shared auth layout, navigation flow). This
document records the technical decisions needed to turn that content spec
into code, and resolves what the prompt left implicit.

Governing standards: `Claude/AI_ROLE_&_Core_Principles.md`,
`Claude/Technology_Stack.md`, `Claude/Progressive_Web_App_Standards.md`
(these three files together serve as `BASE_PROMPT.md`, which does not exist
as a standalone file in this project).

## Current State

Landing Page is complete (`src/pages/LandingPage.tsx` and
`src/components/landing/*`). `Header.tsx` and `CallToActionSection.tsx`
currently render plain, inert `<button>` elements labeled "Login" and
"Request Access" with no navigation wired up. `App.tsx` only defines the
`/` route. `react-router-dom` is installed and already in use for that
route. No auth, form, or validation code exists anywhere in the project yet.

## Resolved Ambiguities (via clarifying questions)

1. **Landing page nav buttons** â€” now that `/login` and `/request-access`
   will exist, the existing inert buttons in `Header.tsx` and
   `CallToActionSection.tsx` are updated to `<Link>` elements pointing at
   the new routes, completing the Landing â†’ Login â†’ Request Access flow
   described in the prompt's Navigation section.
2. **Page chrome** â€” Login and Request Access use a dedicated, minimal
   `AuthLayout` (logo, optional branding panel, card). They do **not**
   reuse the Landing Page's `Header`/`Footer`; this matches the prompt's
   "Shared Authentication Layout" section (branding, logo, card â€” no
   mention of site nav/footer) and standard enterprise auth-page
   conventions.
3. **Submit behavior with no backend** â€” on successful client-side
   validation, the submit button enters a loading/disabled state for a
   short simulated delay (`setTimeout`, ~1s), then returns to idle. No
   success message, no navigation, no fake API/service call â€” this exists
   only to satisfy the prompt's explicit "show loading state on submit"
   requirement without inventing backend behavior.
4. **Forgot Password** â€” no forgot-password page is in scope, so this
   renders as an inert link (no `href`/no-op), the same treatment the
   Landing Page spec gave to Login/Request Access before their target
   pages existed.
5. **Business Type options** â€” the required 8 options (Salon, Barbershop,
   Clinic, Hardware Store, Construction Supplier, Retail Store, Auto Repair
   Shop, Small Wholesaler) are byte-for-byte identical to the existing
   `config/industries.ts` used by the Landing Page's "Businesses We
   Support" section. The Request Access dropdown reuses that config
   directly instead of duplicating it, per the Configuration Driven
   Development standard.
6. **Contact Number format** â€” plain text input, no mask/pattern
   validation, per the prompt's explicit "do not assume a country-specific
   format."
7. **Validation approach** â€” no validation library is installed
   (`react-hook-form`, `yup`, `zod`, etc. are all absent from
   `package.json`). Per the Technology Stack standard ("never introduce
   additional libraries unless explicitly requested"), validation is
   implemented with plain pure functions in `utils/validators.ts`
   (`isRequired`, `isValidEmail`) and local component state â€” no new
   package needed given the small rule set (required + email format only).

## Component Architecture

Chosen approach: composition-based shared primitives, local form state per
page (no generic form-handling hook â€” only two forms exist, and a shared
hook would be premature abstraction for that count).

- `src/layouts/AuthLayout.tsx` â€” centered card layout. Desktop (`lg`+)
  renders a second column with the branding illustration
  (`/assets/images/auth-illustration.png`, referenced as a plain `src`
  string, not imported â€” consistent with the Landing Page's asset-reference
  approach, so a missing file 404s in the browser instead of failing the
  build). Mobile/tablet render the card only. Logo
  (`/assets/icons/logo.svg`) above the card links to `/`.
- `src/components/common/FormGroup.tsx` â€” reusable label + control + inline
  error wrapper. Takes `label`, `htmlFor`, `error`, `required`, and
  `children` (the actual `<input>`/`<textarea>`/`<select>`). Wires
  `aria-invalid` and `aria-describedby` to an error element with
  `role="alert"` when `error` is present. Used by every field in both
  forms (7 fields total), satisfying the "if UI repeats 2+ times, make it
  reusable" standard.
- `src/utils/validators.ts` â€” pure functions, no React dependency:
  `isRequired(value: string): boolean`, `isValidEmail(value: string):
  boolean`.
- `src/interfaces/ILoginFormValues.ts` â€” `{ email: string; password:
  string; rememberMe: boolean }`.
- `src/interfaces/IRequestAccessFormValues.ts` â€” mirrors
  `RequestAccessCommand` exactly: `{ businessName: string; description:
  string; businessType: string; email: string; contactNumber: string }`.

## Pages

### `src/pages/LoginPage.tsx`

Fields: Email Address, Password, Remember Me (checkbox), Forgot Password
(inert link), Login button, "Request Access" link to `/request-access`.

Validation: Email required + valid format; Password required. Errors shown
per-field on blur (touched) and on submit attempt. Login button disabled
and shows a spinner while `isSubmitting` is true.

### `src/pages/RequestAccessPage.tsx`

Fields exactly matching `RequestAccessCommand`, no additional properties:
Business Name (text, required), Description (textarea, required), Business
Type (`<select>` sourced from `config/industries.ts`, required), Email
(text, required + valid format), Contact Number (text, required, no format
assumption).

Buttons: primary "Request Access" (disabled + spinner while submitting),
secondary "Back to Login" (`<Link>` to `/login`).

## Routing Changes

`src/App.tsx` adds two routes:

```tsx
<Route path="/login" element={<LoginPage />} />
<Route path="/request-access" element={<RequestAccessPage />} />
```

`src/components/landing/Header.tsx` and
`src/components/landing/CallToActionSection.tsx`: the existing "Login" and
"Request Access" `<button>` elements become `<Link>`s (styled with the same
existing Bootstrap classes) to `/login` and `/request-access` respectively.

## Non-Goals

No Axios calls, no auth context/services/hooks, no token storage, no
forgot-password page, no persisted "Remember Me" state, no business types
beyond the 8 listed, no dark mode, no backend integration of any kind.

## Deliverable Tracking

`PROJECT_TRACKER.md` is updated to add a row for this feature under the
Booking Module table, following the existing format, once implementation is
complete.
