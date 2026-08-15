import { useState, type FormEvent } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import axios from 'axios'
import { AuthLayout } from '../layouts/AuthLayout'
import { FormGroup } from '../components/common/FormGroup'
import { Button } from '../components/common/Button'
import { Icon } from '../components/common/Icon'
import { getDashboardPath } from '../config/dashboardRoutes'
import { isRequired, isStrongPassword } from '../utils/validators'
import { useAuth } from '../hooks/useAuth'
import type { IResetPasswordFormValues } from '../interfaces/IResetPasswordFormValues'

interface IResetPasswordFormErrors {
  newPassword?: string
  confirmPassword?: string
}

interface IResetPasswordFormTouched {
  newPassword?: boolean
  confirmPassword?: boolean
}

const INITIAL_VALUES: IResetPasswordFormValues = {
  newPassword: '',
  confirmPassword: '',
}

const PASSWORD_REQUIREMENTS_HINT =
  'At least 8 characters, with an uppercase letter, a lowercase letter, a number, and a symbol.'

const GENERIC_ERROR_MESSAGE = 'We could not set your password. Please try again.'

function validate(values: IResetPasswordFormValues): IResetPasswordFormErrors {
  const errors: IResetPasswordFormErrors = {}

  if (!isRequired(values.newPassword)) {
    errors.newPassword = 'Password is required.'
  } else if (!isStrongPassword(values.newPassword)) {
    errors.newPassword = PASSWORD_REQUIREMENTS_HINT
  }

  if (!isRequired(values.confirmPassword)) {
    errors.confirmPassword = 'Please confirm your password.'
  } else if (values.confirmPassword !== values.newPassword) {
    errors.confirmPassword = 'Passwords do not match.'
  }

  return errors
}

function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const detail = (error.response?.data as { detail?: string } | undefined)?.detail
    if (detail) return detail
  }
  return GENERIC_ERROR_MESSAGE
}

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const { resetPassword } = useAuth()
  const userId = searchParams.get('userId')
  const token = searchParams.get('token')

  const [values, setValues] = useState<IResetPasswordFormValues>(INITIAL_VALUES)
  const [errors, setErrors] = useState<IResetPasswordFormErrors>({})
  const [touched, setTouched] = useState<IResetPasswordFormTouched>({})
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)

  if (!userId || !token) {
    return (
      <AuthLayout>
        <div className="text-center">
          <div
            className="d-inline-flex align-items-center justify-content-center rounded-circle mb-4"
            style={{ width: 56, height: 56, backgroundColor: 'var(--color-canvas)' }}
          >
            <Icon name="x-circle" size={24} />
          </div>
          <h1 className="h3 fw-bold mb-3">Invalid Link</h1>
          <p className="text-secondary mb-4">
            This link is missing information it needs to work. Please use the link from your email, or ask
            for a new one.
          </p>
          <Button to="/find-workspace" className="w-100">
            Find Your Workspace
          </Button>
        </div>
      </AuthLayout>
    )
  }

  const handleFieldChange = (field: keyof IResetPasswordFormValues, value: string) => {
    const nextValues = { ...values, [field]: value }
    setValues(nextValues)
    setErrors(validate(nextValues))
  }

  const handleBlur = (field: keyof IResetPasswordFormValues) => {
    setTouched((prev) => ({ ...prev, [field]: true }))
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validate(values)
    setErrors(validationErrors)
    setTouched({ newPassword: true, confirmPassword: true })
    setSubmitError(null)

    if (Object.keys(validationErrors).length > 0) {
      return
    }

    setIsSubmitting(true)
    try {
      const user = await resetPassword(userId, token, values.newPassword, values.confirmPassword)
      navigate(getDashboardPath(user), { replace: true })
    } catch (error) {
      setSubmitError(extractErrorMessage(error))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthLayout>
      <h1 className="h3 fw-bold mb-1">Set Your Password</h1>
      <p className="text-secondary mb-4">Choose a password to secure your account.</p>

      {submitError && (
        <div className="alert alert-danger auth-error-banner" role="alert">
          {submitError}
        </div>
      )}

      <form noValidate onSubmit={handleSubmit}>
        <FormGroup
          label="New Password"
          htmlFor="newPassword"
          required
          error={touched.newPassword ? errors.newPassword : undefined}
        >
          <input
            type="password"
            id="newPassword"
            name="newPassword"
            className={`form-control ${touched.newPassword && errors.newPassword ? 'is-invalid' : ''}`}
            value={values.newPassword}
            onChange={(e) => handleFieldChange('newPassword', e.target.value)}
            onBlur={() => handleBlur('newPassword')}
            aria-invalid={touched.newPassword && !!errors.newPassword}
            aria-describedby={touched.newPassword && errors.newPassword ? 'newPassword-error' : 'newPassword-help'}
          />
          {!(touched.newPassword && errors.newPassword) && (
            <div id="newPassword-help" className="form-text">
              {PASSWORD_REQUIREMENTS_HINT}
            </div>
          )}
        </FormGroup>

        <FormGroup
          label="Confirm Password"
          htmlFor="confirmPassword"
          required
          error={touched.confirmPassword ? errors.confirmPassword : undefined}
        >
          <input
            type="password"
            id="confirmPassword"
            name="confirmPassword"
            className={`form-control ${touched.confirmPassword && errors.confirmPassword ? 'is-invalid' : ''}`}
            value={values.confirmPassword}
            onChange={(e) => handleFieldChange('confirmPassword', e.target.value)}
            onBlur={() => handleBlur('confirmPassword')}
            aria-invalid={touched.confirmPassword && !!errors.confirmPassword}
            aria-describedby={touched.confirmPassword && errors.confirmPassword ? 'confirmPassword-error' : undefined}
          />
        </FormGroup>

        <Button type="submit" fullWidth isLoading={isSubmitting} className="mt-3">
          {isSubmitting ? 'Setting Password...' : 'Set Password'}
        </Button>
      </form>
    </AuthLayout>
  )
}
