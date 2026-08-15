import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import axios from 'axios'
import { AuthLayout } from '../layouts/AuthLayout'
import { FormGroup } from '../components/common/FormGroup'
import { Button } from '../components/common/Button'
import { Icon } from '../components/common/Icon'
import { isRequired, isValidEmail } from '../utils/validators'
import { useAuth } from '../hooks/useAuth'
import { findWorkspace, getWorkspaceSummary, type IWorkspaceSummary } from '../services/authService'
import { getDashboardPath } from '../config/dashboardRoutes'
import type { ILoginFormValues } from '../interfaces/ILoginFormValues'

const NOT_FOUND_MESSAGE = "We couldn't find a workspace for this email."
const GENERIC_LOOKUP_ERROR = 'Something went wrong. Please try again.'
const NETWORK_ERROR_MESSAGE = 'Could not connect. Check your connection and try again.'
const INCORRECT_PASSWORD_MESSAGE = 'Incorrect password'

type Stage = 'email' | 'password'
type WorkspaceStatus = 'checking' | 'ready' | 'not-found'

interface ILoginFormErrors {
  email?: string
  password?: string
}

interface ILoginFormTouched {
  email?: boolean
  password?: boolean
}

const INITIAL_LOGIN_VALUES: ILoginFormValues = {
  email: '',
  password: '',
  rememberMe: false,
}

// Fallback only — used if a real business name somehow isn't available (workspace-summary call
// failed after find-workspace already confirmed the tenant is real). The normal path always has
// the real BusinessProfile.BusinessName from the backend.
function formatSlugAsName(slug: string): string {
  return slug
    .split('-')
    .filter(Boolean)
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ')
}

function validateLogin(values: ILoginFormValues): ILoginFormErrors {
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

export function FindWorkspacePage() {
  const navigate = useNavigate()
  const { slug: urlSlug } = useParams<{ slug: string }>()
  const { login } = useAuth()
  const passwordInputRef = useRef<HTMLInputElement>(null)

  const [stage, setStage] = useState<Stage>(urlSlug ? 'password' : 'email')
  const [resolvedSlug, setResolvedSlug] = useState<string | null>(urlSlug ?? null)
  // Direct-link arrivals (/:slug/login) haven't been verified by anything yet, so they start in
  // "checking" and get confirmed (or rejected) against the real backend before the password form
  // ever renders. Arrivals via the email lookup below are already verified by find-workspace itself.
  const [workspaceStatus, setWorkspaceStatus] = useState<WorkspaceStatus>(urlSlug ? 'checking' : 'ready')
  const [workspaceSummary, setWorkspaceSummary] = useState<IWorkspaceSummary | null>(null)
  const workspaceName = workspaceSummary?.businessName ?? (resolvedSlug ? formatSlugAsName(resolvedSlug) : '')

  // Stage 1: email lookup.
  const [lookupEmail, setLookupEmail] = useState('')
  const [lookupTouched, setLookupTouched] = useState(false)
  const [isLookingUp, setIsLookingUp] = useState(false)
  const [notFound, setNotFound] = useState(false)
  const [lookupError, setLookupError] = useState<string | null>(null)

  // Stage 2: password entry for the resolved workspace.
  const [loginValues, setLoginValues] = useState<ILoginFormValues>(INITIAL_LOGIN_VALUES)
  const [loginErrors, setLoginErrors] = useState<ILoginFormErrors>({})
  const [loginTouched, setLoginTouched] = useState<ILoginFormTouched>({})
  const [isPasswordVisible, setIsPasswordVisible] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [networkError, setNetworkError] = useState<string | null>(null)

  useEffect(() => {
    if (stage === 'password' && workspaceStatus === 'ready') {
      passwordInputRef.current?.focus()
    }
  }, [stage, workspaceStatus])

  // Reacts to the URL's slug actually changing, not just to first mount — /:slug/login and
  // /find-workspace render the same component at the same position in the route tree, so React
  // Router reuses one instance across navigation between them instead of remounting it. Without
  // this, state from a previous slug (e.g. "not-found") would keep showing after navigating to
  // /find-workspace via the "Find Your Workspace" button, even though the URL changed correctly.
  useEffect(() => {
    if (!urlSlug) {
      setStage('email')
      setResolvedSlug(null)
      setWorkspaceSummary(null)
      setWorkspaceStatus('ready')
      return
    }

    setStage('password')
    setResolvedSlug(urlSlug)
    setWorkspaceSummary(null)
    setWorkspaceStatus('checking')

    let isMounted = true
    getWorkspaceSummary(urlSlug)
      .then((summary) => {
        if (!isMounted) return
        if (summary) {
          setWorkspaceSummary(summary)
          setWorkspaceStatus('ready')
        } else {
          setWorkspaceStatus('not-found')
        }
      })
      .catch(() => {
        if (isMounted) setWorkspaceStatus('not-found')
      })

    return () => {
      isMounted = false
    }
  }, [urlSlug])

  const lookupFieldError = lookupTouched && !isValidEmail(lookupEmail) ? 'Enter a valid email address.' : undefined

  const handleLookupSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setLookupTouched(true)
    setNotFound(false)
    setLookupError(null)

    if (!isRequired(lookupEmail) || !isValidEmail(lookupEmail)) {
      return
    }

    setIsLookingUp(true)
    try {
      const slug = await findWorkspace(lookupEmail)
      if (slug) {
        setLoginValues((prev) => ({ ...prev, email: lookupEmail }))
        setResolvedSlug(slug)
        // Best-effort — find-workspace already confirmed this tenant is real and active, so a
        // failure here just falls back to the slug-derived label rather than blocking a flow the
        // user already successfully completed.
        try {
          const summary = await getWorkspaceSummary(slug)
          if (summary) {
            setWorkspaceSummary(summary)
          }
        } catch {
          // fallback label covers it
        }
        setStage('password')
        return
      }
      setNotFound(true)
    } catch {
      setLookupError(GENERIC_LOOKUP_ERROR)
    } finally {
      setIsLookingUp(false)
    }
  }

  const handleUseDifferentWorkspace = () => {
    setStage('email')
    setResolvedSlug(null)
    setWorkspaceSummary(null)
    setLoginValues({ ...INITIAL_LOGIN_VALUES, email: '' })
    setLoginErrors({})
    setLoginTouched({})
    setNetworkError(null)
    setIsPasswordVisible(false)
  }

  const handleLoginFieldChange = (field: 'email' | 'password', value: string) => {
    const nextValues = { ...loginValues, [field]: value }
    setLoginValues(nextValues)
    setLoginErrors(validateLogin(nextValues))
    setNetworkError(null)
  }

  const handleLoginBlur = (field: 'email' | 'password') => {
    setLoginTouched((prev) => ({ ...prev, [field]: true }))
  }

  const handleLoginSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validateLogin(loginValues)
    setLoginErrors(validationErrors)
    setLoginTouched({ email: true, password: true })
    setNetworkError(null)

    if (Object.keys(validationErrors).length > 0 || !resolvedSlug) {
      return
    }

    setIsSubmitting(true)
    try {
      const user = await login(resolvedSlug, loginValues.email, loginValues.password)
      navigate(getDashboardPath(user))
    } catch (error) {
      if (axios.isAxiosError(error) && !error.response) {
        setNetworkError(NETWORK_ERROR_MESSAGE)
      } else {
        setLoginErrors((prev) => ({ ...prev, password: INCORRECT_PASSWORD_MESSAGE }))
        setLoginTouched((prev) => ({ ...prev, password: true }))
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  if (workspaceStatus === 'checking') {
    return (
      <AuthLayout>
        <div className="auth-form-fade d-flex flex-column align-items-center py-4">
          <span className="spinner-border text-primary" role="status" aria-hidden="true" />
          <p className="text-secondary small mt-3 mb-0">Checking workspace...</p>
        </div>
      </AuthLayout>
    )
  }

  if (workspaceStatus === 'not-found') {
    return (
      <AuthLayout>
        <div className="auth-form-fade text-center">
          <h1 className="h4 fw-bold mb-2">Workspace Not Found</h1>
          <p className="text-secondary mb-4">
            We couldn&apos;t find a business at this address. Double-check the link, or find your workspace by
            email instead.
          </p>
          <Button to="/find-workspace" fullWidth>
            Find Your Workspace
          </Button>
        </div>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout>
      <div key={stage} className="auth-form-fade">
        <h1 className="h3 fw-bold mb-1">{stage === 'password' ? `Welcome back to ${workspaceName}` : 'Find Your Workspace'}</h1>
        <p className="text-secondary mb-4">
          {stage === 'password' ? 'Enter your password to continue.' : 'Enter your work email to sign in.'}
        </p>
      </div>

      <div
        className={`stage-panel ${stage === 'email' ? 'stage-panel--visible' : 'stage-panel--hidden'}`}
        inert={stage !== 'email'}
      >
        {lookupError && (
          <div className="alert alert-danger auth-error-banner" role="alert">
            {lookupError}
          </div>
        )}

        <form noValidate onSubmit={handleLookupSubmit}>
          <FormGroup
            label="Email Address"
            htmlFor="email"
            required
            error={lookupTouched && lookupFieldError ? lookupFieldError : notFound ? NOT_FOUND_MESSAGE : undefined}
          >
            <input
              type="email"
              id="email"
              name="email"
              inputMode="email"
              autoComplete="email"
              className={`form-control ${lookupTouched && (lookupFieldError || notFound) ? 'is-invalid' : ''}`}
              value={lookupEmail}
              onChange={(e) => {
                setLookupEmail(e.target.value)
                setNotFound(false)
              }}
              onBlur={() => setLookupTouched(true)}
              aria-invalid={lookupTouched && (!!lookupFieldError || notFound)}
              aria-describedby={lookupTouched && (lookupFieldError || notFound) ? 'email-error' : undefined}
            />
          </FormGroup>

          <Button type="submit" fullWidth isLoading={isLookingUp} className="mt-2">
            {isLookingUp ? 'Searching...' : 'Continue'}
          </Button>
        </form>
      </div>

      <div
        className={`stage-panel ${stage === 'password' ? 'stage-panel--visible' : 'stage-panel--hidden'}`}
        inert={stage !== 'password'}
      >
        <div className="workspace-strip">
          {workspaceSummary?.logo ? (
            <img src={workspaceSummary.logo} alt="" width={40} height={40} className="workspace-strip__logo" />
          ) : (
            <div className="workspace-strip__avatar bg-gradient-brand" aria-hidden="true">
              {workspaceName.charAt(0)}
            </div>
          )}
          <div>
            <p className="workspace-strip__label mb-0">Signing in to</p>
            <p className="workspace-strip__name mb-0">{workspaceName}</p>
          </div>
        </div>

        {networkError && (
          <div className="alert alert-danger auth-error-banner" role="alert">
            {networkError}
          </div>
        )}

        <form noValidate onSubmit={handleLoginSubmit}>
          <FormGroup
            label="Email Address"
            htmlFor="loginEmail"
            required
            error={loginTouched.email ? loginErrors.email : undefined}
          >
            <input
              type="email"
              id="loginEmail"
              name="email"
              inputMode="email"
              autoComplete="email"
              className={`form-control ${loginTouched.email && loginErrors.email ? 'is-invalid' : ''}`}
              value={loginValues.email}
              onChange={(e) => handleLoginFieldChange('email', e.target.value)}
              onBlur={() => handleLoginBlur('email')}
              aria-invalid={loginTouched.email && !!loginErrors.email}
              aria-describedby={loginTouched.email && loginErrors.email ? 'loginEmail-error' : undefined}
            />
          </FormGroup>

          <FormGroup
            label="Password"
            htmlFor="loginPassword"
            required
            error={loginTouched.password ? loginErrors.password : undefined}
          >
            <div className="password-input-wrap">
              <input
                ref={passwordInputRef}
                type={isPasswordVisible ? 'text' : 'password'}
                id="loginPassword"
                name="password"
                autoComplete="current-password"
                className={`form-control ${loginTouched.password && loginErrors.password ? 'is-invalid' : ''}`}
                value={loginValues.password}
                onChange={(e) => handleLoginFieldChange('password', e.target.value)}
                onBlur={() => handleLoginBlur('password')}
                aria-invalid={loginTouched.password && !!loginErrors.password}
                aria-describedby={loginTouched.password && loginErrors.password ? 'loginPassword-error' : undefined}
              />
              <button
                type="button"
                className="password-toggle"
                onClick={() => setIsPasswordVisible((visible) => !visible)}
                aria-label={isPasswordVisible ? 'Hide password' : 'Show password'}
              >
                <Icon name={isPasswordVisible ? 'eye-slash' : 'eye'} size={18} />
              </button>
            </div>
          </FormGroup>

          <p className="mb-3">
            <Button variant="link" className="p-0 text-decoration-none small" onClick={handleUseDifferentWorkspace}>
              Not you? Change email
            </Button>
          </p>

          <div className="d-flex align-items-center justify-content-between mb-4">
            <div className="form-check">
              <input
                type="checkbox"
                id="rememberMe"
                className="form-check-input"
                checked={loginValues.rememberMe}
                onChange={(e) => setLoginValues((prev) => ({ ...prev, rememberMe: e.target.checked }))}
              />
              <label htmlFor="rememberMe" className="form-check-label">
                Remember Me
              </label>
            </div>
            <Button variant="link" className="p-0 text-decoration-none">
              Forgot Password?
            </Button>
          </div>

          <Button type="submit" fullWidth isLoading={isSubmitting} className="mb-3">
            {isSubmitting ? 'Logging in...' : 'Login'}
          </Button>

          <p className="text-center text-secondary mb-0">
            Don&apos;t have access?{' '}
            <Link
              to="/#pricing"
              onClick={(event) => {
                event.preventDefault()
                window.location.assign('/#pricing')
              }}
            >
              Request Access
            </Link>
          </p>
        </form>
      </div>
    </AuthLayout>
  )
}
