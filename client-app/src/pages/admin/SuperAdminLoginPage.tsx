import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import axios from 'axios'
import { SuperAdminAuthLayout } from '../../layouts/SuperAdminAuthLayout'
import { FormGroup } from '../../components/common/FormGroup'
import { Button } from '../../components/common/Button'
import { Icon } from '../../components/common/Icon'
import { isRequired, isValidEmail } from '../../utils/validators'
import { useAuth } from '../../hooks/useAuth'
import type { ISuperAdminLoginFormValues } from '../../interfaces/ISuperAdminLoginFormValues'

const NETWORK_ERROR_MESSAGE = 'Could not connect. Check your connection and try again.'
const INVALID_CREDENTIALS_MESSAGE = 'Invalid email or password.'

interface ISuperAdminLoginFormErrors {
  email?: string
  password?: string
}

interface ISuperAdminLoginFormTouched {
  email?: boolean
  password?: boolean
}

const INITIAL_VALUES: ISuperAdminLoginFormValues = {
  email: '',
  password: '',
}

function validate(values: ISuperAdminLoginFormValues): ISuperAdminLoginFormErrors {
  const errors: ISuperAdminLoginFormErrors = {}

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

export function SuperAdminLoginPage() {
  const [values, setValues] = useState<ISuperAdminLoginFormValues>(INITIAL_VALUES)
  const [errors, setErrors] = useState<ISuperAdminLoginFormErrors>({})
  const [touched, setTouched] = useState<ISuperAdminLoginFormTouched>({})
  const [isPasswordVisible, setIsPasswordVisible] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [networkError, setNetworkError] = useState<string | null>(null)
  const navigate = useNavigate()
  const { loginAsSuperAdmin } = useAuth()

  const handleFieldChange = (field: 'email' | 'password', value: string) => {
    const nextValues = { ...values, [field]: value }
    setValues(nextValues)
    setErrors(validate(nextValues))
    setNetworkError(null)
  }

  const handleBlur = (field: 'email' | 'password') => {
    setTouched((prev) => ({ ...prev, [field]: true }))
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validate(values)
    setErrors(validationErrors)
    setTouched({ email: true, password: true })
    setNetworkError(null)

    if (Object.keys(validationErrors).length > 0) {
      return
    }

    setIsSubmitting(true)
    try {
      // The backend rejects non-platform-admin accounts on this endpoint outright (401) — no
      // client-side re-check needed here, the same way the tenant login page doesn't re-verify
      // tenant membership after a successful call.
      await loginAsSuperAdmin(values.email, values.password)
      navigate('/admin')
    } catch (error) {
      if (axios.isAxiosError(error) && !error.response) {
        setNetworkError(NETWORK_ERROR_MESSAGE)
      } else {
        setErrors((prev) => ({ ...prev, password: INVALID_CREDENTIALS_MESSAGE }))
        setTouched((prev) => ({ ...prev, password: true }))
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <SuperAdminAuthLayout>
      <div className="auth-form-fade">
        <h1 className="h5 fw-bold mb-1">Super Admin Login</h1>
        <p className="text-secondary small mb-4">Authorized personnel only. Enter your credentials to continue.</p>

        {networkError && (
          <div className="alert alert-danger auth-error-banner" role="alert">
            {networkError}
          </div>
        )}

        <form noValidate onSubmit={handleSubmit}>
          <FormGroup label="Email Address" htmlFor="email" required error={touched.email ? errors.email : undefined}>
            <input
              type="email"
              id="email"
              name="email"
              inputMode="email"
              autoComplete="email"
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
            <div className="password-input-wrap">
              <input
                type={isPasswordVisible ? 'text' : 'password'}
                id="password"
                name="password"
                autoComplete="current-password"
                className={`form-control ${touched.password && errors.password ? 'is-invalid' : ''}`}
                value={values.password}
                onChange={(e) => handleFieldChange('password', e.target.value)}
                onBlur={() => handleBlur('password')}
                aria-invalid={touched.password && !!errors.password}
                aria-describedby={touched.password && errors.password ? 'password-error' : undefined}
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

          <Button type="submit" fullWidth isLoading={isSubmitting} className="mt-2">
            {isSubmitting ? 'Logging in...' : 'Login'}
          </Button>
        </form>
      </div>
    </SuperAdminAuthLayout>
  )
}
