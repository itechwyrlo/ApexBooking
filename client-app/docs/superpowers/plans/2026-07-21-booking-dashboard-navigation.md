# Booking Dashboard Navigation & Auth Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a config-driven dashboard shell (sidebar, topbar, mobile off-canvas nav) with role-based menu visibility for the Booking module, backed by real integration against the existing `AuthController` (login, request-access, refresh, logout), with the route guard temporarily disabled so the dashboard can be viewed without a working login.

**Architecture:** A single `Sidebar` renders whichever module's nav config is active (today: Booking only) and filters items via `usePermissions().hasAccess()`, driven by the decoded JWT's role claim. `AuthContext` wraps real Axios calls (`authService`) against `http://localhost:5104/api/Auth/*`; the access token lives in `sessionStorage`, the refresh token is an httpOnly cookie the frontend never touches. `ProtectedRoute` contains the real auth check, commented out behind a `SAFEGUARD` marker. `DashboardLayout` composes `Sidebar` + `Topbar`, swapping the sidebar into a Bootstrap-styled off-canvas panel on mobile via React state (not Bootstrap's JS API, which has no shipped TypeScript types in this project).

**Tech Stack:** React 19 + TypeScript + Vite, React Router v7, Axios (existing dependency), Bootstrap 5 CSS classes driven by React state (no `bootstrap` JS class imports â€” see Global Constraints). All within already-installed dependencies.

## Global Constraints

- TypeScript only: `.ts`/`.tsx` files, never `.js`/`.jsx`.
- `verbatimModuleSyntax` is on â€” use `import type { X }` for anything used only as a type; use a plain `import { X }` for enums/functions/components referenced as values (e.g. `Role.Tenant`).
- `noUnusedLocals`/`noUnusedParameters` are on â€” no dead imports or variables, or `tsc -b` fails. This matters specifically in `ProtectedRoute.tsx`: `isAuthenticated` is destructured but the check that uses it is commented out (see below), so it is explicitly voided (`void isAuthenticated`) to keep the file compiling with the guard off.
- Components: PascalCase filenames. Interfaces: `I`-prefixed. Enums: PascalCase (`Role`), members PascalCase (`Tenant`, `Staff`). Routes: kebab-case.
- Light theme only. Bootstrap 5 utilities/components first.
- **No `bootstrap` JS class imports** (`import { Modal } from 'bootstrap'`, etc.): the installed `bootstrap` package ships no TypeScript types and `@types/bootstrap` is not installed. Modal/off-canvas/toast UI in this plan is built as plain React components that toggle Bootstrap's CSS classes (`show`, `d-block`, etc.) via component state â€” no new dependency, no untyped imports.
- Never generate icon/image files. Reference them as plain `src` string paths (`/assets/icons/appointments.svg`, etc.), never as ES `import`s, so a missing file 404s in the browser instead of breaking the build. This plan references icon paths that do not exist yet â€” add them to `PROJECT_TRACKER.md`'s existing "Known Follow-Ups" list (Task 20).
- Never use `alert()`/`confirm()`. User feedback goes through the `useToast()` hook built in Task 9.
- No test framework in this project â€” verify each task with `npx tsc -b` (typecheck) and `npm run lint` (oxlint). Task 20 does a full manual browser verification pass (this is the substitute for automated integration tests here).
- No git repository in this project â€” no commit steps in this plan.
- **Auth contract** â€” `AuthController` at `http://localhost:5104`, base path `api/Auth`:
  - `POST /api/Auth/login` â†’ `{ email, password }` â†’ `200 { accessToken }` (no refresh token in body) or `401`.
  - `POST /api/Auth/refresh` â†’ no body, relies on the httpOnly cookie (`withCredentials: true`) â†’ `200 { accessToken }` or `401`.
  - `POST /api/Auth/logout` â†’ no body â†’ `204`.
  - `POST /api/Auth/access-request` â†’ `RequestAccessCommand` shape (`businessName, description, businessType, email, contactNumber`) â†’ `202 { tenantId, status }` or `400`.
  - The refresh token is **never** read, stored, or referenced by any frontend code â€” it is an httpOnly cookie set/rotated entirely server-side.
  - The access token is stored in `sessionStorage` under key `apexbooking.accessToken`.
  - The decoded access token's role claim is under the literal key `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role` (the .NET `ClaimTypes.Role` URI) â€” do not shorten this to `"role"`.
- **SAFEGUARD marker** â€” the exact text `// SAFEGUARD: AUTHENTICATION â€” uncomment when backend integration begins` must be preserved verbatim in `ProtectedRoute.tsx` so it stays a single, greppable toggle.
- Full spec: `docs/superpowers/specs/2026-07-21-booking-dashboard-navigation-design.md`. One deliberate deviation from its folder listing: a standalone `config/permissions.config.ts` is **not** created â€” the spec's own examples only ever describe role arrays directly on nav items (e.g. "Clients â† Tenant only"), never a granular permission-string layer, so `Role[]` on `ISidebarNavItem` (Task 1) is the entire mechanism. Adding an indirection file with nothing distinct to hold would be an unused abstraction.

---

## File Structure

```
src/
  types/
    Role.ts                                    (new)
  interfaces/
    ISidebarNavItem.ts                         (new)
    IModule.ts                                 (new)
    IUser.ts                                   (new)
  utils/
    jwt.ts                                     (new)
  api/
    clients/
      authClient.ts                            (new)
    interceptors/
      authRefreshInterceptor.ts                (new)
  services/
    authService.ts                             (new)
  contexts/
    AuthContext.tsx                            (new)
    ToastContext.tsx                           (new)
  hooks/
    useAuth.ts                                 (new)
    usePermissions.ts                          (new)
    useToast.ts                                (new)
  routes/
    ProtectedRoute.tsx                         (new)
    AppRoutes.tsx                              (new)
  config/
    modules.config.ts                          (new)
    navigation/
      booking.nav.config.ts                    (new)
      settings.nav.config.ts                   (new)
  components/
    common/
      EmptyState.tsx                           (new)
      Modal.tsx                                (new)
      ModulePlaceholderPage.tsx                (new)
    layout/
      Sidebar.tsx                              (new)
      SidebarNavItem.tsx                       (new)
      ModuleSwitcher.tsx                       (new)
      Topbar.tsx                               (new)
      MobileNav.tsx                            (new)
  layouts/
    DashboardLayout.tsx                        (new)
    SettingsLayout.tsx                         (new)
  pages/
    booking/
      BookingOverviewPage.tsx                  (new)
      TimeOffsPage.tsx                         (new)
    LoginPage.tsx                              (modified)
    RequestAccessPage.tsx                      (modified)
  App.tsx                                      (modified â€” full replace)
.env.local                                     (new â€” real local dev value)
.env.example                                   (new â€” documents the key)
PROJECT_TRACKER.md                             (modified)
```

---

### Task 1: Core types and interfaces

**Files:**
- Create: `src/types/Role.ts`
- Create: `src/interfaces/ISidebarNavItem.ts`
- Create: `src/interfaces/IModule.ts`
- Create: `src/interfaces/IUser.ts`

**Interfaces:**
- Consumes: nothing
- Produces: `Role` enum (`Tenant`, `Staff`), `ISidebarNavItem` (`{ label, href, icon, roles, children? }`), `IModule` (`{ id, label, basePath }`), `IUser` (`{ id, email, tenantId, roles }`) â€” consumed by nearly every later task

- [ ] **Step 1: Create the Role type**

`src/types/Role.ts`
```ts
export const Role = {
  Tenant: 'Tenant',
  Staff: 'Staff',
} as const

export type Role = (typeof Role)[keyof typeof Role]
```

Note: `tsconfig.app.json` has `erasableSyntaxOnly: true`, which forbids real TS `enum` declarations (they compile to runtime code). This const-object + `typeof` pattern gives identical `Role.Tenant` / `Role[]` usage everywhere else in this plan â€” no other file changes.

- [ ] **Step 2: Create the sidebar nav item interface**

`src/interfaces/ISidebarNavItem.ts`
```ts
import type { Role } from '../types/Role'

export interface ISidebarNavItem {
  label: string
  href: string
  icon: string
  roles: Role[]
  children?: ISidebarNavItem[]
}
```

- [ ] **Step 3: Create the module interface**

`src/interfaces/IModule.ts`
```ts
export interface IModule {
  id: string
  label: string
  basePath: string
}
```

- [ ] **Step 4: Create the user interface**

`src/interfaces/IUser.ts`
```ts
import type { Role } from '../types/Role'

export interface IUser {
  id: string
  email: string
  tenantId: string
  roles: Role[]
}
```

- [ ] **Step 5: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 2: JWT decode utility

**Files:**
- Create: `src/utils/jwt.ts`

**Interfaces:**
- Consumes: `Role` (Task 1)
- Produces: `decodeJwt(token: string): IDecodedAccessToken`, `IDecodedAccessToken` (`{ sub, email, tenantId, roles }`) â€” consumed by Task 6 (AuthContext)

- [ ] **Step 1: Create the decode utility**

`src/utils/jwt.ts`
```ts
import type { Role } from '../types/Role'

export interface IDecodedAccessToken {
  sub: string
  email: string
  tenantId: string
  roles: Role[]
}

const ROLE_CLAIM_KEY = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'

export function decodeJwt(token: string): IDecodedAccessToken {
  const payloadSegment = token.split('.')[1]
  const base64 = payloadSegment.replace(/-/g, '+').replace(/_/g, '/')
  const padding = (4 - (base64.length % 4)) % 4
  const padded = base64 + '='.repeat(padding)
  const json = decodeURIComponent(
    atob(padded)
      .split('')
      .map((char) => '%' + char.charCodeAt(0).toString(16).padStart(2, '0'))
      .join(''),
  )
  const payload = JSON.parse(json) as Record<string, unknown>
  const rawRoles = payload[ROLE_CLAIM_KEY]
  const roles = Array.isArray(rawRoles) ? (rawRoles as Role[]) : rawRoles ? [rawRoles as Role] : []

  return {
    sub: String(payload.sub ?? ''),
    email: String(payload.email ?? ''),
    tenantId: String(payload.TenantId ?? ''),
    roles,
  }
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 3: Env config and Axios auth client

**Files:**
- Create: `.env.local`
- Create: `.env.example`
- Create: `src/api/clients/authClient.ts`

**Interfaces:**
- Consumes: nothing
- Produces: `authClient` (Axios instance), `getAccessToken(): string | null`, `setAccessToken(token: string): void`, `clearAccessToken(): void` â€” consumed by Tasks 4, 5, 6

- [ ] **Step 1: Create the local env file**

`.env.local`
```
VITE_API_BASE_URL=http://localhost:5104
```

- [ ] **Step 2: Create the example env file**

`.env.example`
```
VITE_API_BASE_URL=
```

- [ ] **Step 3: Create the Axios client with token storage**

`src/api/clients/authClient.ts`
```ts
import axios from 'axios'

const ACCESS_TOKEN_STORAGE_KEY = 'apexbooking.accessToken'

export const authClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  withCredentials: true,
})

let accessToken: string | null = sessionStorage.getItem(ACCESS_TOKEN_STORAGE_KEY)

export function getAccessToken(): string | null {
  return accessToken
}

export function setAccessToken(token: string): void {
  accessToken = token
  sessionStorage.setItem(ACCESS_TOKEN_STORAGE_KEY, token)
}

export function clearAccessToken(): void {
  accessToken = null
  sessionStorage.removeItem(ACCESS_TOKEN_STORAGE_KEY)
}

authClient.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.set('Authorization', `Bearer ${accessToken}`)
  }
  return config
})
```

- [ ] **Step 4: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 4: Auth refresh interceptor

**Files:**
- Create: `src/api/interceptors/authRefreshInterceptor.ts`

**Interfaces:**
- Consumes: `authClient`, `setAccessToken`, `clearAccessToken` (Task 3)
- Produces: `registerAuthRefreshInterceptor(): void` â€” consumed by Task 6 (AuthContext)

- [ ] **Step 1: Create the response interceptor**

`src/api/interceptors/authRefreshInterceptor.ts`
```ts
import type { AxiosError, InternalAxiosRequestConfig } from 'axios'
import { authClient, setAccessToken, clearAccessToken } from '../clients/authClient'

interface IRetryableRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean
}

let refreshPromise: Promise<string> | null = null

function refreshAccessToken(): Promise<string> {
  if (!refreshPromise) {
    refreshPromise = authClient
      .post<{ accessToken: string }>('/api/Auth/refresh')
      .then((response) => response.data.accessToken)
      .finally(() => {
        refreshPromise = null
      })
  }
  return refreshPromise
}

export function registerAuthRefreshInterceptor(): void {
  authClient.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
      const config = error.config as IRetryableRequestConfig | undefined
      const isRefreshCall = config?.url?.includes('/refresh')

      if (error.response?.status !== 401 || !config || config._retry || isRefreshCall) {
        return Promise.reject(error)
      }

      config._retry = true

      try {
        const newAccessToken = await refreshAccessToken()
        setAccessToken(newAccessToken)
        return authClient(config)
      } catch (refreshError) {
        clearAccessToken()
        return Promise.reject(refreshError)
      }
    },
  )
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 5: Auth service

**Files:**
- Create: `src/services/authService.ts`

**Interfaces:**
- Consumes: `authClient`, `setAccessToken`, `clearAccessToken` (Task 3), `IRequestAccessFormValues` (existing, `src/interfaces/IRequestAccessFormValues.ts`)
- Produces: `login(email: string, password: string): Promise<void>` and `logout(): Promise<void>` â€” consumed by Task 6 (AuthContext). `requestAccess(values: IRequestAccessFormValues): Promise<{ tenantId: string; status: string }>` â€” consumed by Task 19 (RequestAccessPage). `refreshToken(): Promise<void>` is exported for symmetry/future explicit use but is not called by any task in this plan â€” the 401 interceptor (Task 4) hits `/api/Auth/refresh` directly rather than through this wrapper, to keep `api/` free of a dependency on `services/`.

- [ ] **Step 1: Create the auth service**

`src/services/authService.ts`
```ts
import { authClient, setAccessToken, clearAccessToken } from '../api/clients/authClient'
import type { IRequestAccessFormValues } from '../interfaces/IRequestAccessFormValues'

interface ILoginResponse {
  accessToken: string
}

interface IRequestAccessResponse {
  tenantId: string
  status: string
}

export async function login(email: string, password: string): Promise<void> {
  const response = await authClient.post<ILoginResponse>('/api/Auth/login', { email, password })
  setAccessToken(response.data.accessToken)
}

export async function refreshToken(): Promise<void> {
  const response = await authClient.post<ILoginResponse>('/api/Auth/refresh')
  setAccessToken(response.data.accessToken)
}

export async function logout(): Promise<void> {
  await authClient.post('/api/Auth/logout')
  clearAccessToken()
}

export async function requestAccess(values: IRequestAccessFormValues): Promise<IRequestAccessResponse> {
  const response = await authClient.post<IRequestAccessResponse>('/api/Auth/access-request', values)
  return response.data
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 6: Auth context and useAuth hook

**Files:**
- Create: `src/contexts/AuthContext.tsx`
- Create: `src/hooks/useAuth.ts`

**Interfaces:**
- Consumes: `getAccessToken`, `clearAccessToken` (Task 3), `registerAuthRefreshInterceptor` (Task 4), `authService.login`/`authService.logout` (Task 5), `decodeJwt` (Task 2), `IUser` (Task 1)
- Produces: `AuthProvider` component, `useAuth(): { user: IUser | null; isAuthenticated: boolean; isLoading: boolean; login(email, password): Promise<void>; logout(): Promise<void> }` â€” consumed by Task 7, Task 8, Task 13, Task 18, App.tsx (Task 17)

- [ ] **Step 1: Create the auth context**

`src/contexts/AuthContext.tsx`
```tsx
import { createContext, useContext, useState, type ReactNode } from 'react'
import { getAccessToken, clearAccessToken } from '../api/clients/authClient'
import { registerAuthRefreshInterceptor } from '../api/interceptors/authRefreshInterceptor'
import * as authService from '../services/authService'
import { decodeJwt } from '../utils/jwt'
import type { IUser } from '../interfaces/IUser'

registerAuthRefreshInterceptor()

interface IAuthContextValue {
  user: IUser | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<IAuthContextValue | undefined>(undefined)

function buildUserFromToken(token: string): IUser {
  const decoded = decodeJwt(token)
  return {
    id: decoded.sub,
    email: decoded.email,
    tenantId: decoded.tenantId,
    roles: decoded.roles,
  }
}

interface IAuthProviderProps {
  children: ReactNode
}

export function AuthProvider({ children }: IAuthProviderProps) {
  const [user, setUser] = useState<IUser | null>(() => {
    const token = getAccessToken()
    return token ? buildUserFromToken(token) : null
  })
  const [isLoading, setIsLoading] = useState(false)

  async function login(email: string, password: string): Promise<void> {
    setIsLoading(true)
    try {
      await authService.login(email, password)
      const token = getAccessToken()
      setUser(token ? buildUserFromToken(token) : null)
    } finally {
      setIsLoading(false)
    }
  }

  async function logout(): Promise<void> {
    await authService.logout()
    clearAccessToken()
    setUser(null)
  }

  const value: IAuthContextValue = {
    user,
    isAuthenticated: user !== null,
    isLoading,
    login,
    logout,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): IAuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
```

- [ ] **Step 2: Create the hook re-export**

`src/hooks/useAuth.ts`
```ts
export { useAuth } from '../contexts/AuthContext'
```

- [ ] **Step 3: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 7: usePermissions hook

**Files:**
- Create: `src/hooks/usePermissions.ts`

**Interfaces:**
- Consumes: `useAuth` (Task 6), `ISidebarNavItem` (Task 1)
- Produces: `usePermissions(): { hasAccess(item: ISidebarNavItem): boolean }` â€” consumed by Task 12 (Sidebar)

- [ ] **Step 1: Create the permissions hook**

`src/hooks/usePermissions.ts`
```ts
import { useAuth } from './useAuth'
import type { ISidebarNavItem } from '../interfaces/ISidebarNavItem'

interface IUsePermissionsResult {
  hasAccess: (item: ISidebarNavItem) => boolean
}

export function usePermissions(): IUsePermissionsResult {
  const { user } = useAuth()

  function hasAccess(item: ISidebarNavItem): boolean {
    if (!user) return false
    return item.roles.some((role) => user.roles.includes(role))
  }

  return { hasAccess }
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 8: ProtectedRoute with the safeguard

**Files:**
- Create: `src/routes/ProtectedRoute.tsx`

**Interfaces:**
- Consumes: `useAuth` (Task 6), `Role` (Task 1)
- Produces: `ProtectedRoute` component (`{ children: ReactNode; allowedRoles?: Role[] }`) â€” consumed by Task 17 (AppRoutes)

- [ ] **Step 1: Create the guard component**

`src/routes/ProtectedRoute.tsx`
```tsx
import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import type { Role } from '../types/Role'

interface IProtectedRouteProps {
  children: ReactNode
  allowedRoles?: Role[]
}

export function ProtectedRoute({ children, allowedRoles }: IProtectedRouteProps) {
  const { user, isAuthenticated } = useAuth()
  void isAuthenticated // referenced so this compiles with the SAFEGUARD check below commented out

  // SAFEGUARD: AUTHENTICATION â€” uncomment when backend integration begins
  // if (!isAuthenticated) return <Navigate to="/login" replace />

  if (allowedRoles && user && !allowedRoles.some((role) => user.roles.includes(role))) {
    return <Navigate to="/app/booking" replace />
  }

  return <>{children}</>
}
```

- [ ] **Step 2: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 9: Toast notification primitive

**Files:**
- Create: `src/contexts/ToastContext.tsx`
- Create: `src/hooks/useToast.ts`

**Interfaces:**
- Consumes: nothing
- Produces: `ToastProvider` component, `useToast(): { showToast(variant: 'success' | 'error' | 'warning' | 'info', message: string): void }` â€” consumed by Task 17 (App.tsx), Task 18 (LoginPage), Task 19 (RequestAccessPage)

- [ ] **Step 1: Create the toast context**

`src/contexts/ToastContext.tsx`
```tsx
import { createContext, useCallback, useContext, useState, type ReactNode } from 'react'

export type ToastVariant = 'success' | 'error' | 'warning' | 'info'

interface IToastState {
  id: number
  variant: ToastVariant
  message: string
}

interface IToastContextValue {
  showToast: (variant: ToastVariant, message: string) => void
}

const ToastContext = createContext<IToastContextValue | undefined>(undefined)

const AUTO_DISMISS_MS = 5000

const VARIANT_CLASSES: Record<ToastVariant, string> = {
  success: 'text-bg-success',
  error: 'text-bg-danger',
  warning: 'text-bg-warning',
  info: 'text-bg-info',
}

interface IToastProviderProps {
  children: ReactNode
}

export function ToastProvider({ children }: IToastProviderProps) {
  const [toast, setToast] = useState<IToastState | null>(null)

  const showToast = useCallback((variant: ToastVariant, message: string) => {
    const id = Date.now()
    setToast({ id, variant, message })
    window.setTimeout(() => {
      setToast((current) => (current?.id === id ? null : current))
    }, AUTO_DISMISS_MS)
  }, [])

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <div className="toast-container position-fixed top-0 end-0 p-3" style={{ zIndex: 1080 }}>
        {toast && (
          <div className={`toast show ${VARIANT_CLASSES[toast.variant]}`} role="alert" aria-live="assertive" aria-atomic="true">
            <div className="d-flex">
              <div className="toast-body">{toast.message}</div>
              <button
                type="button"
                className="btn-close btn-close-white me-2 m-auto"
                aria-label="Close"
                onClick={() => setToast(null)}
              />
            </div>
          </div>
        )}
      </div>
    </ToastContext.Provider>
  )
}

export function useToast(): IToastContextValue {
  const context = useContext(ToastContext)
  if (!context) {
    throw new Error('useToast must be used within a ToastProvider')
  }
  return context
}
```

- [ ] **Step 2: Create the hook re-export**

`src/hooks/useToast.ts`
```ts
export { useToast } from '../contexts/ToastContext'
```

- [ ] **Step 3: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 10: Navigation configuration

**Files:**
- Create: `src/config/modules.config.ts`
- Create: `src/config/navigation/booking.nav.config.ts`
- Create: `src/config/navigation/settings.nav.config.ts`

**Interfaces:**
- Consumes: `IModule`, `ISidebarNavItem`, `Role` (Task 1)
- Produces: `MODULES: IModule[]`, `BOOKING_NAV_ITEMS: ISidebarNavItem[]`, `ISettingsNavItem` (`{ label, href }`), `SETTINGS_NAV_ITEMS: ISettingsNavItem[]` â€” consumed by Task 12 (Sidebar/ModuleSwitcher), Task 14 (SettingsLayout)

- [ ] **Step 1: Create the module registry**

`src/config/modules.config.ts`
```ts
import type { IModule } from '../interfaces/IModule'

export const MODULES: IModule[] = [{ id: 'booking', label: 'Booking', basePath: '/app/booking' }]
```

- [ ] **Step 2: Create the Booking nav config**

`src/config/navigation/booking.nav.config.ts`
```ts
import type { ISidebarNavItem } from '../../interfaces/ISidebarNavItem'
import { Role } from '../../types/Role'

export const BOOKING_NAV_ITEMS: ISidebarNavItem[] = [
  {
    label: 'Appointments',
    href: '/app/booking/appointments',
    icon: '/assets/icons/appointments.svg',
    roles: [Role.Tenant, Role.Staff],
  },
  {
    label: 'Calendar',
    href: '/app/booking/calendar',
    icon: '/assets/icons/calendar.svg',
    roles: [Role.Tenant, Role.Staff],
  },
  { label: 'Clients', href: '/app/booking/clients', icon: '/assets/icons/clients.svg', roles: [Role.Tenant] },
  { label: 'Staff', href: '/app/booking/staff', icon: '/assets/icons/staff.svg', roles: [Role.Tenant] },
  { label: 'Services', href: '/app/booking/services', icon: '/assets/icons/services.svg', roles: [Role.Tenant] },
  {
    label: 'Business Profile',
    href: '/app/booking/business-profile',
    icon: '/assets/icons/business-profile.svg',
    roles: [Role.Tenant],
  },
  {
    label: 'Time Offs',
    href: '/app/booking/time-offs',
    icon: '/assets/icons/time-offs.svg',
    roles: [Role.Tenant, Role.Staff],
  },
  { label: 'Settings', href: '/app/booking/settings', icon: '/assets/icons/settings.svg', roles: [Role.Tenant] },
]
```

- [ ] **Step 3: Create the Settings sub-nav config**

`src/config/navigation/settings.nav.config.ts`
```ts
export interface ISettingsNavItem {
  label: string
  href: string
}

export const SETTINGS_NAV_ITEMS: ISettingsNavItem[] = [
  { label: 'Booking Settings', href: '/app/booking/settings/booking' },
  { label: 'Payment Settings', href: '/app/booking/settings/payment' },
]
```

- [ ] **Step 4: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 11: Common reusable components

**Files:**
- Create: `src/components/common/EmptyState.tsx`
- Create: `src/components/common/Modal.tsx`
- Create: `src/components/common/ModulePlaceholderPage.tsx`

**Interfaces:**
- Consumes: nothing
- Produces: `EmptyState` (`{ title, description, actionLabel?, onAction? }`), `Modal` (`{ isOpen, title, onClose, children }`), `ModulePlaceholderPage` (`{ title, description }`) â€” consumed by Tasks 15, 16, 17

- [ ] **Step 1: Create the empty-state component**

`src/components/common/EmptyState.tsx`
```tsx
interface IEmptyStateProps {
  title: string
  description: string
  actionLabel?: string
  onAction?: () => void
}

export function EmptyState({ title, description, actionLabel, onAction }: IEmptyStateProps) {
  return (
    <div className="text-center py-5">
      <h3 className="fs-6 fw-semibold mb-2">{title}</h3>
      <p className="text-muted mb-3">{description}</p>
      {actionLabel && onAction && (
        <button type="button" className="btn btn-primary btn-sm" onClick={onAction}>
          {actionLabel}
        </button>
      )}
    </div>
  )
}
```

- [ ] **Step 2: Create the modal component**

`src/components/common/Modal.tsx`
```tsx
import type { ReactNode } from 'react'

interface IModalProps {
  isOpen: boolean
  title: string
  onClose: () => void
  children: ReactNode
}

export function Modal({ isOpen, title, onClose, children }: IModalProps) {
  if (!isOpen) return null

  return (
    <>
      <div className="modal d-block" tabIndex={-1} role="dialog" aria-modal="true" aria-label={title}>
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <div className="modal-header">
              <h2 className="modal-title fs-5">{title}</h2>
              <button type="button" className="btn-close" aria-label="Close" onClick={onClose} />
            </div>
            <div className="modal-body">{children}</div>
          </div>
        </div>
      </div>
      <div className="modal-backdrop show" />
    </>
  )
}
```

- [ ] **Step 3: Create the placeholder page component**

`src/components/common/ModulePlaceholderPage.tsx`
```tsx
import { EmptyState } from './EmptyState'

interface IModulePlaceholderPageProps {
  title: string
  description: string
}

export function ModulePlaceholderPage({ title, description }: IModulePlaceholderPageProps) {
  return (
    <div>
      <h1 className="fs-3 fw-semibold mb-4">{title}</h1>
      <EmptyState title="Nothing here yet" description={description} />
    </div>
  )
}
```

- [ ] **Step 4: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 12: Sidebar components

**Files:**
- Create: `src/components/layout/SidebarNavItem.tsx`
- Create: `src/components/layout/ModuleSwitcher.tsx`
- Create: `src/components/layout/Sidebar.tsx`

**Interfaces:**
- Consumes: `ISidebarNavItem` (Task 1), `usePermissions` (Task 7), `BOOKING_NAV_ITEMS` (Task 10), `MODULES` (Task 10)
- Produces: `Sidebar` component (no props) â€” consumed by Task 14 (DashboardLayout)

- [ ] **Step 1: Create the recursive nav item**

`src/components/layout/SidebarNavItem.tsx`
```tsx
import { NavLink } from 'react-router-dom'
import type { ISidebarNavItem } from '../../interfaces/ISidebarNavItem'

interface ISidebarNavItemProps {
  item: ISidebarNavItem
}

export function SidebarNavItem({ item }: ISidebarNavItemProps) {
  return (
    <li className="nav-item">
      <NavLink
        to={item.href}
        className={({ isActive }) =>
          `nav-link d-flex align-items-center gap-2 px-3 py-2 rounded${isActive ? ' active bg-primary text-white' : ' text-dark'}`
        }
      >
        <img src={item.icon} alt="" width={18} height={18} aria-hidden="true" />
        <span>{item.label}</span>
      </NavLink>
      {item.children && item.children.length > 0 && (
        <ul className="nav flex-column ms-3">
          {item.children.map((child) => (
            <SidebarNavItem key={child.href} item={child} />
          ))}
        </ul>
      )}
    </li>
  )
}
```

- [ ] **Step 2: Create the module switcher**

`src/components/layout/ModuleSwitcher.tsx`
```tsx
import { MODULES } from '../../config/modules.config'

export function ModuleSwitcher() {
  return (
    <select className="form-select" defaultValue={MODULES[0]?.id} aria-label="Switch module" disabled={MODULES.length <= 1}>
      {MODULES.map((module) => (
        <option key={module.id} value={module.id}>
          {module.label}
        </option>
      ))}
    </select>
  )
}
```

- [ ] **Step 3: Create the sidebar**

`src/components/layout/Sidebar.tsx`
```tsx
import { BOOKING_NAV_ITEMS } from '../../config/navigation/booking.nav.config'
import { usePermissions } from '../../hooks/usePermissions'
import { ModuleSwitcher } from './ModuleSwitcher'
import { SidebarNavItem } from './SidebarNavItem'

export function Sidebar() {
  const { hasAccess } = usePermissions()
  const visibleItems = BOOKING_NAV_ITEMS.filter(hasAccess)

  return (
    <nav className="d-flex flex-column h-100 p-3" aria-label="Primary">
      <ModuleSwitcher />
      <ul className="nav nav-pills flex-column gap-1 mt-3">
        {visibleItems.map((item) => (
          <SidebarNavItem key={item.href} item={item} />
        ))}
      </ul>
    </nav>
  )
}
```

- [ ] **Step 4: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 13: Topbar and mobile nav

**Files:**
- Create: `src/components/layout/Topbar.tsx`
- Create: `src/components/layout/MobileNav.tsx`

**Interfaces:**
- Consumes: `useAuth` (Task 6)
- Produces: `Topbar` (`{ onMenuClick: () => void }`), `MobileNav` (`{ isOpen, onClose, children }`) â€” consumed by Task 14 (DashboardLayout)

- [ ] **Step 1: Create the topbar**

`src/components/layout/Topbar.tsx`
```tsx
import { useAuth } from '../../hooks/useAuth'

interface ITopbarProps {
  onMenuClick: () => void
}

export function Topbar({ onMenuClick }: ITopbarProps) {
  const { user, logout } = useAuth()

  return (
    <header className="d-flex align-items-center border-bottom bg-white px-3 py-2">
      <button type="button" className="btn btn-outline-secondary d-lg-none" aria-label="Open menu" onClick={onMenuClick}>
        <img src="/assets/icons/menu.svg" alt="" width={20} height={20} />
      </button>
      <div className="ms-auto d-flex align-items-center gap-3">
        <span className="text-muted small">{user?.email}</span>
        <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => void logout()}>
          Log out
        </button>
      </div>
    </header>
  )
}
```

- [ ] **Step 2: Create the mobile off-canvas nav**

`src/components/layout/MobileNav.tsx`
```tsx
import type { ReactNode } from 'react'

interface IMobileNavProps {
  isOpen: boolean
  onClose: () => void
  children: ReactNode
}

export function MobileNav({ isOpen, onClose, children }: IMobileNavProps) {
  return (
    <div className="d-lg-none">
      <div
        className={`offcanvas offcanvas-start${isOpen ? ' show' : ''}`}
        style={{ visibility: isOpen ? 'visible' : 'hidden' }}
        tabIndex={-1}
        aria-hidden={!isOpen}
      >
        <div className="offcanvas-header">
          <h2 className="offcanvas-title fs-5">Menu</h2>
          <button type="button" className="btn-close" aria-label="Close" onClick={onClose} />
        </div>
        <div className="offcanvas-body p-0">{children}</div>
      </div>
      {isOpen && <div className="offcanvas-backdrop show" onClick={onClose} />}
    </div>
  )
}
```

- [ ] **Step 3: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 14: DashboardLayout and SettingsLayout

**Files:**
- Create: `src/layouts/DashboardLayout.tsx`
- Create: `src/layouts/SettingsLayout.tsx`

**Interfaces:**
- Consumes: `Sidebar` (Task 12), `Topbar`, `MobileNav` (Task 13), `SETTINGS_NAV_ITEMS` (Task 10)
- Produces: `DashboardLayout`, `SettingsLayout` components (both render `<Outlet />`) â€” consumed by Task 17 (AppRoutes)

- [ ] **Step 1: Create the dashboard shell**

`src/layouts/DashboardLayout.tsx`
```tsx
import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import { Sidebar } from '../components/layout/Sidebar'
import { Topbar } from '../components/layout/Topbar'
import { MobileNav } from '../components/layout/MobileNav'

export function DashboardLayout() {
  const [isMobileNavOpen, setIsMobileNavOpen] = useState(false)

  return (
    <div className="d-flex min-vh-100">
      <aside className="d-none d-lg-block border-end bg-white" style={{ width: '260px' }}>
        <Sidebar />
      </aside>
      <MobileNav isOpen={isMobileNavOpen} onClose={() => setIsMobileNavOpen(false)}>
        <Sidebar />
      </MobileNav>
      <div className="d-flex flex-column flex-grow-1">
        <Topbar onMenuClick={() => setIsMobileNavOpen(true)} />
        <main className="flex-grow-1 p-3 p-md-4">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Create the settings sub-shell**

`src/layouts/SettingsLayout.tsx`
```tsx
import { NavLink, Outlet } from 'react-router-dom'
import { SETTINGS_NAV_ITEMS } from '../config/navigation/settings.nav.config'

export function SettingsLayout() {
  return (
    <div>
      <h1 className="fs-3 fw-semibold mb-4">Settings</h1>
      <div className="row g-4">
        <div className="col-12 col-md-3">
          <ul className="nav nav-pills flex-column gap-1">
            {SETTINGS_NAV_ITEMS.map((item) => (
              <li className="nav-item" key={item.href}>
                <NavLink to={item.href} className={({ isActive }) => `nav-link${isActive ? ' active' : ' text-dark'}`}>
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </div>
        <div className="col-12 col-md-9">
          <Outlet />
        </div>
      </div>
    </div>
  )
}
```

- [ ] **Step 3: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 15: Booking Overview page

**Files:**
- Create: `src/pages/booking/BookingOverviewPage.tsx`

**Interfaces:**
- Consumes: `EmptyState` (Task 11)
- Produces: `BookingOverviewPage` component â€” consumed by Task 17 (AppRoutes)

- [ ] **Step 1: Create the overview page**

`src/pages/booking/BookingOverviewPage.tsx`
```tsx
import { Link } from 'react-router-dom'
import { EmptyState } from '../../components/common/EmptyState'

export function BookingOverviewPage() {
  return (
    <div>
      <h1 className="fs-3 fw-semibold mb-4">Overview</h1>
      <div className="row g-3 mb-4">
        <div className="col-6 col-md-3">
          <div className="card border-0 shadow-sm">
            <div className="card-body">
              <p className="text-muted small mb-1">Today&apos;s Appointments</p>
              <p className="fs-4 fw-semibold mb-0">0</p>
            </div>
          </div>
        </div>
        <div className="col-6 col-md-3">
          <div className="card border-0 shadow-sm">
            <div className="card-body">
              <p className="text-muted small mb-1">Upcoming This Week</p>
              <p className="fs-4 fw-semibold mb-0">0</p>
            </div>
          </div>
        </div>
      </div>
      <div className="row g-3">
        <div className="col-12 col-md-6">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-body">
              <h2 className="fs-6 fw-semibold mb-3">Quick Actions</h2>
              <div className="d-flex flex-wrap gap-2">
                <Link to="/app/booking/appointments" className="btn btn-primary btn-sm">
                  New Appointment
                </Link>
                <Link to="/app/booking/clients" className="btn btn-outline-secondary btn-sm">
                  Add Client
                </Link>
              </div>
            </div>
          </div>
        </div>
        <div className="col-12 col-md-6">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-body">
              <h2 className="fs-6 fw-semibold mb-3">Today&apos;s Calendar</h2>
              <EmptyState title="No appointments today" description="Your schedule for today will appear here." />
            </div>
          </div>
        </div>
        <div className="col-12">
          <div className="card border-0 shadow-sm">
            <div className="card-body">
              <h2 className="fs-6 fw-semibold mb-3">Recent Activity</h2>
              <EmptyState title="No recent activity" description="Booking activity will show up here as it happens." />
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

### Task 16: Time Offs page

**Files:**
- Create: `src/pages/booking/TimeOffsPage.tsx`

**Interfaces:**
- Consumes: `Modal`, `EmptyState` (Task 11)
- Produces: `TimeOffsPage` component â€” consumed by Task 17 (AppRoutes)

- [ ] **Step 1: Create the Time Offs page**

`src/pages/booking/TimeOffsPage.tsx`
```tsx
import { useState } from 'react'
import { Modal } from '../../components/common/Modal'
import { EmptyState } from '../../components/common/EmptyState'

export function TimeOffsPage() {
  const [isAddModalOpen, setIsAddModalOpen] = useState(false)

  return (
    <div>
      <div className="d-flex align-items-center justify-content-between mb-4">
        <h1 className="fs-3 fw-semibold mb-0">Time Offs</h1>
        <button type="button" className="btn btn-primary" onClick={() => setIsAddModalOpen(true)}>
          Add Time Off
        </button>
      </div>
      <EmptyState title="No time off recorded" description="Time off requests will appear here once added." />
      <Modal isOpen={isAddModalOpen} title="Add Time Off" onClose={() => setIsAddModalOpen(false)}>
        <p className="text-muted mb-0">Time off creation form is not yet available.</p>
      </Modal>
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

### Task 17: App routing

**Files:**
- Create: `src/routes/AppRoutes.tsx`
- Modify: `src/App.tsx` (full replace)

**Interfaces:**
- Consumes: `ProtectedRoute` (Task 8), `DashboardLayout`/`SettingsLayout` (Task 14), `BookingOverviewPage` (Task 15), `TimeOffsPage` (Task 16), `ModulePlaceholderPage` (Task 11), `Role` (Task 1), `AuthProvider` (Task 6), `ToastProvider` (Task 9), existing `LandingPage`/`LoginPage`/`RequestAccessPage`
- Produces: full route tree mounted at `/`, `/login`, `/request-access`, `/app` (redirect), `/app/booking/*`

- [ ] **Step 1: Create the route tree**

`src/routes/AppRoutes.tsx`
```tsx
import { Navigate, Route, Routes } from 'react-router-dom'
import { LandingPage } from '../pages/LandingPage'
import { LoginPage } from '../pages/LoginPage'
import { RequestAccessPage } from '../pages/RequestAccessPage'
import { DashboardLayout } from '../layouts/DashboardLayout'
import { SettingsLayout } from '../layouts/SettingsLayout'
import { BookingOverviewPage } from '../pages/booking/BookingOverviewPage'
import { TimeOffsPage } from '../pages/booking/TimeOffsPage'
import { ModulePlaceholderPage } from '../components/common/ModulePlaceholderPage'
import { ProtectedRoute } from './ProtectedRoute'
import { Role } from '../types/Role'

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/request-access" element={<RequestAccessPage />} />

      <Route path="/app" element={<Navigate to="/app/booking" replace />} />

      <Route
        path="/app/booking"
        element={
          <ProtectedRoute>
            <DashboardLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<BookingOverviewPage />} />
        <Route
          path="appointments"
          element={<ModulePlaceholderPage title="Appointments" description="Appointment scheduling is coming soon." />}
        />
        <Route
          path="calendar"
          element={<ModulePlaceholderPage title="Calendar" description="Calendar view is coming soon." />}
        />
        <Route
          path="clients"
          element={
            <ProtectedRoute allowedRoles={[Role.Tenant]}>
              <ModulePlaceholderPage title="Clients" description="Client management is coming soon." />
            </ProtectedRoute>
          }
        />
        <Route
          path="staff"
          element={
            <ProtectedRoute allowedRoles={[Role.Tenant]}>
              <ModulePlaceholderPage title="Staff" description="Staff management is coming soon." />
            </ProtectedRoute>
          }
        />
        <Route
          path="services"
          element={
            <ProtectedRoute allowedRoles={[Role.Tenant]}>
              <ModulePlaceholderPage title="Services" description="Service management is coming soon." />
            </ProtectedRoute>
          }
        />
        <Route
          path="business-profile"
          element={
            <ProtectedRoute allowedRoles={[Role.Tenant]}>
              <ModulePlaceholderPage title="Business Profile" description="Business profile settings are coming soon." />
            </ProtectedRoute>
          }
        />
        <Route path="time-offs" element={<TimeOffsPage />} />
        <Route
          path="settings"
          element={
            <ProtectedRoute allowedRoles={[Role.Tenant]}>
              <SettingsLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<Navigate to="booking" replace />} />
          <Route
            path="booking"
            element={<ModulePlaceholderPage title="Booking Settings" description="Booking settings are coming soon." />}
          />
          <Route
            path="payment"
            element={<ModulePlaceholderPage title="Payment Settings" description="Payment settings are coming soon." />}
          />
        </Route>
      </Route>
    </Routes>
  )
}
```

- [ ] **Step 2: Replace App.tsx to mount the providers and routes**

`src/App.tsx` â€” replace the entire file:
```tsx
import { AuthProvider } from './contexts/AuthContext'
import { ToastProvider } from './contexts/ToastContext'
import { AppRoutes } from './routes/AppRoutes'

function App() {
  return (
    <AuthProvider>
      <ToastProvider>
        <AppRoutes />
      </ToastProvider>
    </AuthProvider>
  )
}

export default App
```

- [ ] **Step 3: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 18: Wire LoginPage to real authentication

**Files:**
- Modify: `src/pages/LoginPage.tsx`

**Interfaces:**
- Consumes: `useAuth` (Task 6), `useToast` (Task 9)

- [ ] **Step 1: Update the imports**

In `src/pages/LoginPage.tsx`, replace:
```tsx
import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { AuthLayout } from '../layouts/AuthLayout'
import { FormGroup } from '../components/common/FormGroup'
import { isRequired, isValidEmail } from '../utils/validators'
import type { ILoginFormValues } from '../interfaces/ILoginFormValues'
```
with:
```tsx
import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { AuthLayout } from '../layouts/AuthLayout'
import { FormGroup } from '../components/common/FormGroup'
import { isRequired, isValidEmail } from '../utils/validators'
import { useAuth } from '../hooks/useAuth'
import { useToast } from '../hooks/useToast'
import type { ILoginFormValues } from '../interfaces/ILoginFormValues'
```

- [ ] **Step 2: Add the new hooks inside the component**

Replace:
```tsx
export function LoginPage() {
  const [values, setValues] = useState<ILoginFormValues>(INITIAL_VALUES)
  const [errors, setErrors] = useState<ILoginFormErrors>({})
  const [touched, setTouched] = useState<ILoginFormTouched>({})
  const [isSubmitting, setIsSubmitting] = useState(false)
```
with:
```tsx
export function LoginPage() {
  const [values, setValues] = useState<ILoginFormValues>(INITIAL_VALUES)
  const [errors, setErrors] = useState<ILoginFormErrors>({})
  const [touched, setTouched] = useState<ILoginFormTouched>({})
  const [isSubmitting, setIsSubmitting] = useState(false)
  const navigate = useNavigate()
  const { login } = useAuth()
  const { showToast } = useToast()
```

- [ ] **Step 3: Replace the simulated submit with a real login call**

Replace:
```tsx
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
```
with:
```tsx
  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validate(values)
    setErrors(validationErrors)
    setTouched({ email: true, password: true })

    if (Object.keys(validationErrors).length > 0) {
      return
    }

    setIsSubmitting(true)
    try {
      await login(values.email, values.password)
      navigate('/app/booking')
    } catch {
      showToast('error', 'Invalid email or password.')
    } finally {
      setIsSubmitting(false)
    }
  }
```

- [ ] **Step 4: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 19: Wire RequestAccessPage to real submission

**Files:**
- Modify: `src/pages/RequestAccessPage.tsx`

**Interfaces:**
- Consumes: `authService.requestAccess` (Task 5), `useToast` (Task 9)

- [ ] **Step 1: Update the imports**

In `src/pages/RequestAccessPage.tsx`, replace:
```tsx
import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { AuthLayout } from '../layouts/AuthLayout'
import { FormGroup } from '../components/common/FormGroup'
import { INDUSTRIES } from '../config/industries'
import { isRequired, isValidEmail } from '../utils/validators'
import type { IRequestAccessFormValues } from '../interfaces/IRequestAccessFormValues'
```
with:
```tsx
import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { AuthLayout } from '../layouts/AuthLayout'
import { FormGroup } from '../components/common/FormGroup'
import { INDUSTRIES } from '../config/industries'
import { isRequired, isValidEmail } from '../utils/validators'
import { requestAccess } from '../services/authService'
import { useToast } from '../hooks/useToast'
import type { IRequestAccessFormValues } from '../interfaces/IRequestAccessFormValues'
```

- [ ] **Step 2: Add the new hooks inside the component**

Replace:
```tsx
export function RequestAccessPage() {
  const [values, setValues] = useState<IRequestAccessFormValues>(INITIAL_VALUES)
  const [errors, setErrors] = useState<IRequestAccessFormErrors>({})
  const [touched, setTouched] = useState<IRequestAccessFormTouched>({})
  const [isSubmitting, setIsSubmitting] = useState(false)
```
with:
```tsx
export function RequestAccessPage() {
  const [values, setValues] = useState<IRequestAccessFormValues>(INITIAL_VALUES)
  const [errors, setErrors] = useState<IRequestAccessFormErrors>({})
  const [touched, setTouched] = useState<IRequestAccessFormTouched>({})
  const [isSubmitting, setIsSubmitting] = useState(false)
  const navigate = useNavigate()
  const { showToast } = useToast()
```

- [ ] **Step 3: Replace the simulated submit with a real API call**

Replace:
```tsx
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
```
with:
```tsx
  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validate(values)
    setErrors(validationErrors)
    setTouched(ALL_FIELDS_TOUCHED)

    if (Object.keys(validationErrors).length > 0) {
      return
    }

    setIsSubmitting(true)
    try {
      await requestAccess(values)
      showToast('success', 'Request received â€” we will review it shortly.')
      navigate('/login')
    } catch {
      showToast('error', 'We could not submit your request. Please check your details and try again.')
    } finally {
      setIsSubmitting(false)
    }
  }
```

- [ ] **Step 4: Typecheck and lint**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.

---

### Task 20: Project tracker update and full manual verification

**Files:**
- Modify: `PROJECT_TRACKER.md`

- [ ] **Step 1: Add the feature row to the Booking Module table**

In `PROJECT_TRACKER.md`, add a new row to the table under `## Booking Module`:
```
| Dashboard Navigation & Auth Integration | Complete | Config-driven sidebar/topbar/mobile off-canvas nav shell at `/app/booking/*`, role-based visibility (Tenant/Staff) via `usePermissions`. Real `AuthController` integration (login, access-request, refresh, logout) â€” access token in `sessionStorage`, refresh token is httpOnly-cookie-only. `ProtectedRoute`'s auth check is commented out behind a `SAFEGUARD` marker for dev preview; role checks (`allowedRoles`) are active. Appointments, Calendar, Clients, Staff, Services, Business Profile, and both Settings pages are placeholder pages pending future tasks. Time Offs has a working "Add Time Off" modal shell with no backend yet. |
```

- [ ] **Step 2: Add new follow-ups**

In `PROJECT_TRACKER.md`, under `## Known Follow-Ups`, add:
```
- New icon assets referenced by the dashboard nav do not exist yet:
  `/assets/icons/appointments.svg`, `calendar.svg`, `clients.svg`, `staff.svg`,
  `services.svg`, `business-profile.svg`, `time-offs.svg`, `settings.svg`,
  `menu.svg`. The app builds and runs without them; the browser shows broken
  images until they're supplied.
- `ProtectedRoute.tsx`'s `SAFEGUARD: AUTHENTICATION` block must be uncommented
  once real login is expected to gate the dashboard â€” currently `/app/booking`
  is reachable without logging in.
- The backend's CORS policy must allow credentials from the frontend's exact
  origin (not `*`) for the httpOnly refresh cookie to round-trip correctly â€”
  this is backend configuration, not addressed by this frontend change.
- No reset-password page exists yet even though `AuthController` exposes
  `POST /api/Auth/reset-password` â€” out of scope until requested.
```

- [ ] **Step 3: Start the dev server**

Run: `npm run dev`
Expected: Vite starts without errors, prints a local URL (e.g. `http://localhost:5173`).

- [ ] **Step 4: Manually verify the dashboard shell**

In a browser, at the dev server URL:
1. Navigate directly to `/app/booking` â€” it should render `DashboardLayout` (the `SAFEGUARD` check is commented out, so no login is required).
2. Confirm the sidebar shows no items yet (no user means `usePermissions().hasAccess` returns `false` for every item) â€” this is expected until a real login populates `user`.
3. Navigate to `/login`, submit valid-looking credentials. If the backend at `http://localhost:5104` is running and returns a `200`, confirm: redirect to `/app/booking`, sidebar now shows items filtered by the logged-in user's role, the topbar shows the user's email.
4. If the backend returns `401`, confirm a red error toast appears in the top-right corner (not a browser `alert()`).
5. Resize the browser below the `lg` breakpoint (or use device toolbar). Confirm the sidebar is hidden and a hamburger button appears in the topbar; clicking it opens the off-canvas menu with the same nav items; clicking the backdrop or close button closes it.
6. Click "Time Offs" in the sidebar, then "Add Time Off" â€” confirm a modal opens with the close button working.
7. If logged in as a Tenant-role user, click "Settings" â€” confirm it lands on `/app/booking/settings/booking` (redirected from the settings index) with a two-item sub-nav (Booking Settings, Payment Settings).
8. Navigate to `/request-access`, submit the form. If the backend returns `202`, confirm a green success toast appears and the page redirects to `/login`. If it returns `400`, confirm a red error toast appears.

- [ ] **Step 5: Final typecheck and lint across the whole project**

Run: `npx tsc -b`
Expected: no errors.

Run: `npm run lint`
Expected: no errors.
