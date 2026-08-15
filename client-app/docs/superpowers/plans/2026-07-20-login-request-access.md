# ApexBooking Login & Request Access Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the Login page (`/login`) and Request Access page (`/request-access`) â€” client-side-only forms with validation, matching the Landing Page's visual language, sharing a dedicated auth layout. No auth logic, no API calls, no backend integration.

**Architecture:** Two new routes render `pages/LoginPage.tsx` and `pages/RequestAccessPage.tsx`, both wrapped in a shared `layouts/AuthLayout.tsx` (logo, responsive branding panel, centered card). Both pages use a shared `components/common/FormGroup.tsx` for label/control/error markup and pure functions from `utils/validators.ts` for required/email validation. Each page owns its own form state via `useState` â€” no generic form hook. The Landing Page's existing inert Login/Request Access buttons become `<Link>`s to the new routes.

**Tech Stack:** React 19 + TypeScript + Vite, React Router v7 (two new routes), Bootstrap 5 (existing CSS + JS bundle, no react-bootstrap). All already installed â€” no new dependencies.

## Global Constraints

- TypeScript only: `.ts`/`.tsx` files, never `.js`/`.jsx`.
- `verbatimModuleSyntax` is on (see `tsconfig.app.json`) â€” use `import type { X }` for type-only imports, or the build fails.
- `noUnusedLocals`/`noUnusedParameters` are on â€” no dead imports or variables, or `tsc -b` fails.
- Components: PascalCase filenames (`LoginPage.tsx`). Interfaces: `I`-prefixed (`ILoginFormValues`). Routes: kebab-case (`/request-access`).
- Light theme only. No dark mode, no glassmorphism/neumorphism/gradients.
- Bootstrap 5 utilities first; only reach for inline styles when no utility covers it.
- Mobile-first, fluid layouts only â€” no fixed-width containers.
- Never generate icon or image files. Reference them as plain string paths (`/assets/icons/...`, `/assets/images/...`), never as ES `import`s, so a missing file 404s in the browser instead of breaking the build.
- Never use `alert()` or `confirm()`.
- No test framework in this project â€” verify each task with `npx tsc -b` (typecheck) and `npm run lint` (oxlint); verify the final integration task by running the dev server and checking both pages in a browser at mobile/tablet/desktop widths.
- No git repository in this project â€” no commit steps in this plan.
- No Axios calls, no auth context/services/hooks, no token storage, no forgot-password page, no persisted "Remember Me" state, no business types beyond the 8 already in `config/industries.ts`, no fake API responses.
- Business Type dropdown reuses the existing `INDUSTRIES` array from `src/config/industries.ts` â€” do not create a duplicate config file.
- Submit buttons simulate a ~1s loading state via `setTimeout`, then return to idle. No success message, no navigation on submit.

---

## File Structure

```
src/
  interfaces/
    ILoginFormValues.ts       (new)
    IRequestAccessFormValues.ts (new)
  utils/
    validators.ts              (new)
  components/
    common/
      FormGroup.tsx            (new)
  layouts/
    AuthLayout.tsx              (new)
  pages/
    LoginPage.tsx                (new)
    RequestAccessPage.tsx        (new)
  App.tsx                        (modified â€” add 2 routes)
  components/landing/
    Header.tsx                   (modified â€” Login/Request Access buttons become Links)
    CallToActionSection.tsx      (modified â€” Login/Request Access buttons become Links)
PROJECT_TRACKER.md               (modified â€” add feature row)
```

---

### Task 1: Form value interfaces

**Files:**
- Create: `src/interfaces/ILoginFormValues.ts`
- Create: `src/interfaces/IRequestAccessFormValues.ts`

**Interfaces:**
- Consumes: nothing
- Produces: `ILoginFormValues`, `IRequestAccessFormValues` types â€” consumed by Task 5 (LoginPage) and Task 6 (RequestAccessPage)

- [ ] **Step 1: Create the login form values interface**

`src/interfaces/ILoginFormValues.ts`
```ts
export interface ILoginFormValues {
  email: string
  password: string
  rememberMe: boolean
}
```

- [ ] **Step 2: Create the request access form values interface**

`src/interfaces/IRequestAccessFormValues.ts`
```ts
export interface IRequestAccessFormValues {
  businessName: string
  description: string
  businessType: string
  email: string
  contactNumber: string
}
```

- [ ] **Step 3: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 2: Validators

**Files:**
- Create: `src/utils/validators.ts`

**Interfaces:**
- Consumes: nothing
- Produces: `isRequired(value: string): boolean`, `isValidEmail(value: string): boolean` â€” consumed by Task 5 (LoginPage) and Task 6 (RequestAccessPage)

- [ ] **Step 1: Create the pure validator functions**

`src/utils/validators.ts`
```ts
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function isRequired(value: string): boolean {
  return value.trim().length > 0
}

export function isValidEmail(value: string): boolean {
  return EMAIL_PATTERN.test(value)
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 3: FormGroup component

**Files:**
- Create: `src/components/common/FormGroup.tsx`

**Interfaces:**
- Consumes: nothing from earlier tasks
- Produces: `<FormGroup label required htmlFor error>{children}</FormGroup>` â€” renders a Bootstrap-styled label, the passed-in control, and an inline error (`role="alert"`, `id="{htmlFor}-error"`) when `error` is truthy. Consumed by Task 5 (LoginPage) and Task 6 (RequestAccessPage), which are responsible for setting `id`, `aria-invalid`, and `aria-describedby="{htmlFor}-error"` on the control they pass as `children`.

- [ ] **Step 1: Create FormGroup**

`src/components/common/FormGroup.tsx`
```tsx
import type { ReactNode } from 'react'

interface IFormGroupProps {
  label: string
  htmlFor: string
  error?: string
  required?: boolean
  children: ReactNode
}

export function FormGroup({ label, htmlFor, error, required, children }: IFormGroupProps) {
  return (
    <div className="mb-3">
      <label htmlFor={htmlFor} className="form-label">
        {label}
        {required && (
          <span className="text-danger ms-1" aria-hidden="true">
            *
          </span>
        )}
      </label>
      {children}
      {error && (
        <div id={`${htmlFor}-error`} className="invalid-feedback d-block" role="alert">
          {error}
        </div>
      )}
    </div>
  )
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 4: AuthLayout

**Files:**
- Create: `src/layouts/AuthLayout.tsx`

**Interfaces:**
- Consumes: nothing from earlier tasks
- Produces: `<AuthLayout>{children}</AuthLayout>` â€” full-height centered layout with logo, responsive branding panel, and a card wrapping `children`. Consumed by Task 5 (LoginPage) and Task 6 (RequestAccessPage).

- [ ] **Step 1: Create AuthLayout**

`src/layouts/AuthLayout.tsx`
```tsx
import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

interface IAuthLayoutProps {
  children: ReactNode
}

export function AuthLayout({ children }: IAuthLayoutProps) {
  return (
    <div className="min-vh-100 d-flex align-items-center bg-light py-5">
      <div className="container">
        <div className="row justify-content-center align-items-center g-5">
          <div className="col-lg-6 d-none d-lg-block">
            <Link to="/" className="d-inline-flex align-items-center gap-2 mb-4 text-decoration-none">
              <img src="/assets/icons/logo.svg" alt="" width={32} height={32} />
              <span className="fw-semibold fs-5 text-dark">ApexBooking</span>
            </Link>
            <img
              src="/assets/images/auth-illustration.png"
              alt=""
              className="img-fluid rounded-3"
            />
          </div>
          <div className="col-12 col-md-8 col-lg-5">
            <div className="d-flex d-lg-none justify-content-center mb-4">
              <Link to="/" className="d-inline-flex align-items-center gap-2 text-decoration-none">
                <img src="/assets/icons/logo.svg" alt="" width={32} height={32} />
                <span className="fw-semibold fs-5 text-dark">ApexBooking</span>
              </Link>
            </div>
            <div className="card border-0 shadow-sm">
              <div className="card-body p-4 p-md-5">{children}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 5: Login page

**Files:**
- Create: `src/pages/LoginPage.tsx`

**Interfaces:**
- Consumes: `AuthLayout` (Task 4), `FormGroup` (Task 3), `isRequired`/`isValidEmail` (Task 2), `ILoginFormValues` (Task 1)
- Produces: `<LoginPage />` â€” consumed by Task 7 (routing)

- [ ] **Step 1: Create LoginPage**

`src/pages/LoginPage.tsx`
```tsx
import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { AuthLayout } from '../layouts/AuthLayout'
import { FormGroup } from '../components/common/FormGroup'
import { isRequired, isValidEmail } from '../utils/validators'
import type { ILoginFormValues } from '../interfaces/ILoginFormValues'

interface ILoginFormErrors {
  email?: string
  password?: string
}

interface ILoginFormTouched {
  email?: boolean
  password?: boolean
}

const INITIAL_VALUES: ILoginFormValues = {
  email: '',
  password: '',
  rememberMe: false,
}

function validate(values: ILoginFormValues): ILoginFormErrors {
  const errors: ILoginFormErrors = {}

  if (!isRequired(values.email)) {
    errors.email = 'Email address is required.'
  } else if (!isValidEmail(values.email)) {
    errors.email = 'Enter a valid email address.'
  }

  if (!isRequired(values.password)) {
    errors.password = 'Password is required.'
  }

  return errors
}

export function LoginPage() {
  const [values, setValues] = useState<ILoginFormValues>(INITIAL_VALUES)
  const [errors, setErrors] = useState<ILoginFormErrors>({})
  const [touched, setTouched] = useState<ILoginFormTouched>({})
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleFieldChange = (field: 'email' | 'password', value: string) => {
    const nextValues = { ...values, [field]: value }
    setValues(nextValues)
    setErrors(validate(nextValues))
  }

  const handleBlur = (field: 'email' | 'password') => {
    setTouched((prev) => ({ ...prev, [field]: true }))
  }

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validate(values)
    setErrors(validationErrors)
    setTouched({ email: true, password: true })

    if (Object.keys(validationErrors).length > 0) {
      return
    }

    setIsSubmitting(true)
    setTimeout(() => {
      setIsSubmitting(false)
    }, 1000)
  }

  return (
    <AuthLayout>
      <h1 className="h3 fw-bold mb-1">Welcome back</h1>
      <p className="text-secondary mb-4">Log in to manage your business on ApexBooking.</p>
      <form noValidate onSubmit={handleSubmit}>
        <FormGroup label="Email Address" htmlFor="email" required error={touched.email ? errors.email : undefined}>
          <input
            type="email"
            id="email"
            name="email"
            className={`form-control ${touched.email && errors.email ? 'is-invalid' : ''}`}
            value={values.email}
            onChange={(e) => handleFieldChange('email', e.target.value)}
            onBlur={() => handleBlur('email')}
            aria-invalid={touched.email && !!errors.email}
            aria-describedby={touched.email && errors.email ? 'email-error' : undefined}
          />
        </FormGroup>

        <FormGroup
          label="Password"
          htmlFor="password"
          required
          error={touched.password ? errors.password : undefined}
        >
          <input
            type="password"
            id="password"
            name="password"
            className={`form-control ${touched.password && errors.password ? 'is-invalid' : ''}`}
            value={values.password}
            onChange={(e) => handleFieldChange('password', e.target.value)}
            onBlur={() => handleBlur('password')}
            aria-invalid={touched.password && !!errors.password}
            aria-describedby={touched.password && errors.password ? 'password-error' : undefined}
          />
        </FormGroup>

        <div className="d-flex align-items-center justify-content-between mb-4">
          <div className="form-check">
            <input
              type="checkbox"
              id="rememberMe"
              className="form-check-input"
              checked={values.rememberMe}
              onChange={(e) => setValues((prev) => ({ ...prev, rememberMe: e.target.checked }))}
            />
            <label htmlFor="rememberMe" className="form-check-label">
              Remember Me
            </label>
          </div>
          <button type="button" className="btn btn-link p-0 text-decoration-none">
            Forgot Password?
          </button>
        </div>

        <button type="submit" className="btn btn-primary w-100 mb-3" disabled={isSubmitting}>
          {isSubmitting ? (
            <>
              <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />
              Logging in...
            </>
          ) : (
            'Login'
          )}
        </button>

        <p className="text-center text-secondary mb-0">
          Don&apos;t have access? <Link to="/request-access">Request Access</Link>
        </p>
      </form>
    </AuthLayout>
  )
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 6: Request Access page

**Files:**
- Create: `src/pages/RequestAccessPage.tsx`

**Interfaces:**
- Consumes: `AuthLayout` (Task 4), `FormGroup` (Task 3), `isRequired`/`isValidEmail` (Task 2), `IRequestAccessFormValues` (Task 1), `INDUSTRIES` from `src/config/industries.ts` (existing)
- Produces: `<RequestAccessPage />` â€” consumed by Task 7 (routing)

- [ ] **Step 1: Create RequestAccessPage**

`src/pages/RequestAccessPage.tsx`
```tsx
import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { AuthLayout } from '../layouts/AuthLayout'
import { FormGroup } from '../components/common/FormGroup'
import { INDUSTRIES } from '../config/industries'
import { isRequired, isValidEmail } from '../utils/validators'
import type { IRequestAccessFormValues } from '../interfaces/IRequestAccessFormValues'

type RequestAccessField = keyof IRequestAccessFormValues

type IRequestAccessFormErrors = Partial<Record<RequestAccessField, string>>
type IRequestAccessFormTouched = Partial<Record<RequestAccessField, boolean>>

const INITIAL_VALUES: IRequestAccessFormValues = {
  businessName: '',
  description: '',
  businessType: '',
  email: '',
  contactNumber: '',
}

const ALL_FIELDS_TOUCHED: IRequestAccessFormTouched = {
  businessName: true,
  description: true,
  businessType: true,
  email: true,
  contactNumber: true,
}

function validate(values: IRequestAccessFormValues): IRequestAccessFormErrors {
  const errors: IRequestAccessFormErrors = {}

  if (!isRequired(values.businessName)) {
    errors.businessName = 'Business name is required.'
  }

  if (!isRequired(values.description)) {
    errors.description = 'Description is required.'
  }

  if (!isRequired(values.businessType)) {
    errors.businessType = 'Business type is required.'
  }

  if (!isRequired(values.email)) {
    errors.email = 'Email address is required.'
  } else if (!isValidEmail(values.email)) {
    errors.email = 'Enter a valid email address.'
  }

  if (!isRequired(values.contactNumber)) {
    errors.contactNumber = 'Contact number is required.'
  }

  return errors
}

export function RequestAccessPage() {
  const [values, setValues] = useState<IRequestAccessFormValues>(INITIAL_VALUES)
  const [errors, setErrors] = useState<IRequestAccessFormErrors>({})
  const [touched, setTouched] = useState<IRequestAccessFormTouched>({})
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleFieldChange = (field: RequestAccessField, value: string) => {
    const nextValues = { ...values, [field]: value }
    setValues(nextValues)
    setErrors(validate(nextValues))
  }

  const handleBlur = (field: RequestAccessField) => {
    setTouched((prev) => ({ ...prev, [field]: true }))
  }

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validate(values)
    setErrors(validationErrors)
    setTouched(ALL_FIELDS_TOUCHED)

    if (Object.keys(validationErrors).length > 0) {
      return
    }

    setIsSubmitting(true)
    setTimeout(() => {
      setIsSubmitting(false)
    }, 1000)
  }

  return (
    <AuthLayout>
      <h1 className="h3 fw-bold mb-1">Request Access</h1>
      <p className="text-secondary mb-4">Tell us about your business to get started with ApexBooking.</p>
      <form noValidate onSubmit={handleSubmit}>
        <FormGroup
          label="Business Name"
          htmlFor="businessName"
          required
          error={touched.businessName ? errors.businessName : undefined}
        >
          <input
            type="text"
            id="businessName"
            name="businessName"
            className={`form-control ${touched.businessName && errors.businessName ? 'is-invalid' : ''}`}
            value={values.businessName}
            onChange={(e) => handleFieldChange('businessName', e.target.value)}
            onBlur={() => handleBlur('businessName')}
            aria-invalid={touched.businessName && !!errors.businessName}
            aria-describedby={touched.businessName && errors.businessName ? 'businessName-error' : undefined}
          />
        </FormGroup>

        <FormGroup
          label="Description"
          htmlFor="description"
          required
          error={touched.description ? errors.description : undefined}
        >
          <textarea
            id="description"
            name="description"
            rows={4}
            className={`form-control ${touched.description && errors.description ? 'is-invalid' : ''}`}
            value={values.description}
            onChange={(e) => handleFieldChange('description', e.target.value)}
            onBlur={() => handleBlur('description')}
            aria-invalid={touched.description && !!errors.description}
            aria-describedby={touched.description && errors.description ? 'description-error' : undefined}
          />
        </FormGroup>

        <FormGroup
          label="Business Type"
          htmlFor="businessType"
          required
          error={touched.businessType ? errors.businessType : undefined}
        >
          <select
            id="businessType"
            name="businessType"
            className={`form-select ${touched.businessType && errors.businessType ? 'is-invalid' : ''}`}
            value={values.businessType}
            onChange={(e) => handleFieldChange('businessType', e.target.value)}
            onBlur={() => handleBlur('businessType')}
            aria-invalid={touched.businessType && !!errors.businessType}
            aria-describedby={touched.businessType && errors.businessType ? 'businessType-error' : undefined}
          >
            <option value="">Select a business type</option>
            {INDUSTRIES.map((industry) => (
              <option key={industry.id} value={industry.name}>
                {industry.name}
              </option>
            ))}
          </select>
        </FormGroup>

        <FormGroup label="Email Address" htmlFor="email" required error={touched.email ? errors.email : undefined}>
          <input
            type="email"
            id="email"
            name="email"
            className={`form-control ${touched.email && errors.email ? 'is-invalid' : ''}`}
            value={values.email}
            onChange={(e) => handleFieldChange('email', e.target.value)}
            onBlur={() => handleBlur('email')}
            aria-invalid={touched.email && !!errors.email}
            aria-describedby={touched.email && errors.email ? 'email-error' : undefined}
          />
        </FormGroup>

        <FormGroup
          label="Contact Number"
          htmlFor="contactNumber"
          required
          error={touched.contactNumber ? errors.contactNumber : undefined}
        >
          <input
            type="text"
            id="contactNumber"
            name="contactNumber"
            className={`form-control ${touched.contactNumber && errors.contactNumber ? 'is-invalid' : ''}`}
            value={values.contactNumber}
            onChange={(e) => handleFieldChange('contactNumber', e.target.value)}
            onBlur={() => handleBlur('contactNumber')}
            aria-invalid={touched.contactNumber && !!errors.contactNumber}
            aria-describedby={touched.contactNumber && errors.contactNumber ? 'contactNumber-error' : undefined}
          />
        </FormGroup>

        <div className="d-flex flex-column flex-sm-row gap-2 mt-4">
          <button type="submit" className="btn btn-primary flex-fill" disabled={isSubmitting}>
            {isSubmitting ? (
              <>
                <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />
                Submitting...
              </>
            ) : (
              'Request Access'
            )}
          </button>
          <Link to="/login" className="btn btn-outline-secondary flex-fill">
            Back to Login
          </Link>
        </div>
      </form>
    </AuthLayout>
  )
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 7: Wire routes and connect Landing Page navigation

**Files:**
- Modify: `src/App.tsx` (full rewrite)
- Modify: `src/components/landing/Header.tsx`
- Modify: `src/components/landing/CallToActionSection.tsx`

**Interfaces:**
- Consumes: `LoginPage` (Task 5), `RequestAccessPage` (Task 6)
- Produces: the complete `/login` and `/request-access` routes, reachable from the Landing Page header and call-to-action section

- [ ] **Step 1: Add the two routes to App.tsx**

`src/App.tsx`
```tsx
import { Routes, Route } from 'react-router-dom'
import { LandingPage } from './pages/LandingPage'
import { LoginPage } from './pages/LoginPage'
import { RequestAccessPage } from './pages/RequestAccessPage'

function App() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/request-access" element={<RequestAccessPage />} />
    </Routes>
  )
}

export default App
```

- [ ] **Step 2: Wire the Header's Login/Request Access buttons to the new routes**

In `src/components/landing/Header.tsx`, add the import and replace the two `<button>` elements (currently lines 35-40) with `<Link>`s:

```tsx
import { Link } from 'react-router-dom'
import { NAV_ITEMS } from '../../config/navigation'
import { InstallAppButton } from '../pwa/InstallAppButton'

export function Header() {
  return (
    <header className="sticky-top bg-white border-bottom">
      <nav className="navbar navbar-expand-lg container py-3">
        <a className="navbar-brand d-flex align-items-center gap-2" href="#home">
          <img src="/assets/icons/logo.svg" alt="" width={32} height={32} />
          <span className="fw-semibold fs-5">ApexBooking</span>
        </a>
        <button
          className="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#mainNav"
          aria-controls="mainNav"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          <span className="navbar-toggler-icon"></span>
        </button>
        <div className="collapse navbar-collapse" id="mainNav">
          <ul className="navbar-nav mx-auto my-3 my-lg-0 gap-lg-4">
            {NAV_ITEMS.map((item) => (
              <li className="nav-item" key={item.href}>
                <a className="nav-link" href={item.href}>
                  {item.label}
                </a>
              </li>
            ))}
          </ul>
          <div className="d-flex align-items-center gap-2">
            <InstallAppButton />
            <Link to="/login" className="btn btn-outline-secondary">
              Login
            </Link>
            <Link to="/request-access" className="btn btn-primary">
              Request Access
            </Link>
          </div>
        </div>
      </nav>
    </header>
  )
}
```

- [ ] **Step 3: Wire the Call To Action section's buttons to the new routes**

`src/components/landing/CallToActionSection.tsx` (full rewrite)
```tsx
import { Link } from 'react-router-dom'

export function CallToActionSection() {
  return (
    <section className="py-5 bg-primary text-white text-center">
      <div className="container">
        <h2 className="fw-bold mb-3">Ready to bring your business online?</h2>
        <p className="mb-4 opacity-75">
          Join local businesses already using ApexBooking to manage bookings.
        </p>
        <div className="d-flex flex-wrap justify-content-center gap-3">
          <Link to="/request-access" className="btn btn-light btn-lg">
            Request Access
          </Link>
          <Link to="/login" className="btn btn-outline-light btn-lg">
            Login
          </Link>
        </div>
      </div>
    </section>
  )
}
```

- [ ] **Step 4: Typecheck, lint, and build**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

Run: `npm run build`
Expected: build succeeds (missing `/assets/icons/logo.svg` and `/assets/images/auth-illustration.png` are fine â€” plain `src` string paths, not imports).

- [ ] **Step 5: Manual browser verification**

Run: `npm run dev`, open the printed local URL.

Check:
- From `/`, the Header's and Call-To-Action section's "Login" and "Request Access" buttons navigate to `/login` and `/request-access`.
- `/login`: submitting with empty fields shows both required errors; an invalid email shows the email-format error; filling both fields and submitting shows a spinner on the Login button for ~1s, then returns to idle with the form still filled in. "Request Access" link at the bottom navigates to `/request-access`. Logo at the top navigates back to `/`.
- `/request-access`: submitting with empty fields shows all five required errors; an invalid email shows the email-format error; the Business Type dropdown lists exactly the 8 options (Salon, Barbershop, Clinic, Hardware Store, Construction Supplier, Retail Store, Auto Repair Shop, Small Wholesaler); filling all fields and submitting shows a spinner on the Request Access button for ~1s, then returns to idle. "Back to Login" navigates to `/login`.
- Resize the viewport through mobile (~375px), tablet (~768px), laptop (~1280px), and wide (~1920px) on both pages: at `lg` and above, the branding panel appears beside the card; below `lg`, only the card (with logo above it) is shown. No horizontal scrollbar at any width.
- Tab through both forms with the keyboard only: focus order is logical, every input has a visible focus indicator, and the Forgot Password / Back to Login / Login / Request Access links and buttons are all reachable.
- No console errors (missing icon/image 404s for `logo.svg` and `auth-illustration.png` are expected and fine at this stage).

---

### Task 8: Update PROJECT_TRACKER.md

**Files:**
- Modify: `PROJECT_TRACKER.md`

**Interfaces:**
- Consumes: nothing
- Produces: updated status tracker reflecting this feature's completion

- [ ] **Step 1: Add a row for this feature and remove the now-resolved follow-up note**

Replace the `Booking Module` table and the `/login` / `/request-access` bullet in `Known Follow-Ups` in `PROJECT_TRACKER.md` with:

```markdown
## Booking Module

| Feature | Status | Notes |
|---|---|---|
| Landing Page | Complete | Static marketing page at `/`. Sections: Header, Hero, Businesses We Support, Booking Features, Dashboard Preview, Pricing, How It Works, Call To Action, Footer. PWA install button wired via `useInstallPrompt`. No auth, no API integration. |
| Login Page | Complete | UI-only form at `/login` (email, password, remember me, forgot password placeholder). Client-side validation only, no auth logic, no API calls. Shares `AuthLayout` and `FormGroup` with Request Access. |
| Request Access Page | Complete | UI-only form at `/request-access` matching the `RequestAccessCommand` shape (business name, description, business type, email, contact number). Client-side validation only, no API calls, no submission logic. |

## Known Follow-Ups (not started, not in current scope)

- Icon/image assets referenced by the Landing Page and auth pages
  (`/assets/icons/**`, `/assets/images/**`, `pwa-192x192.png`,
  `pwa-512x512.png`) do not exist yet and need to be added by hand â€” the
  app builds and runs without them, the browser just shows a broken image
  / default icon until they're supplied.
- Login and Request Access are UI only: no auth context, no session
  handling, no backend submission, no forgot-password page. These will be
  wired up in a future backend-integration task.
```

- [ ] **Step 2: Verify**

Run: `npm run build`
Expected: still succeeds (this task only edits a markdown file, no code changes).

Confirm `PROJECT_TRACKER.md` renders correctly (open it in the editor).
