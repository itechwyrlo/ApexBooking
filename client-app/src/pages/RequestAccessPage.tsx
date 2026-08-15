import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { FormGroup } from '../components/common/FormGroup'
import { Button } from '../components/common/Button'
import { Reveal } from '../components/common/Reveal'
import { PlanSummaryCard } from '../components/requestAccess/PlanSummaryCard'
import { ProgressTracker } from '../components/requestAccess/ProgressTracker'
import { FormSectionHeader } from '../components/requestAccess/FormSectionHeader'
import { BusinessTypeSelector } from '../components/requestAccess/BusinessTypeSelector'
import { SlugValidationHint } from '../components/requestAccess/SlugValidationHint'
import { PRICING_PLANS } from '../config/pricing'
import { isRequired, isValidEmail } from '../utils/validators'
import { requestAccess } from '../services/authService'
import { useToast } from '../hooks/useToast'
import type { IRequestAccessFormValues } from '../interfaces/IRequestAccessFormValues'

type RequestAccessField = keyof IRequestAccessFormValues

type IRequestAccessFormErrors = Partial<Record<RequestAccessField, string>>
type IRequestAccessFormTouched = Partial<Record<RequestAccessField, boolean>>

const SLUG_PATTERN = /^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$/

const INITIAL_VALUES: IRequestAccessFormValues = {
  businessName: '',
  businessType: '',
  slug: '',
  ownerFirstName: '',
  ownerLastName: '',
  ownerEmail: '',
  requestedPlan: '',
}

const ALL_FIELDS_TOUCHED: IRequestAccessFormTouched = {
  businessName: true,
  businessType: true,
  slug: true,
  ownerFirstName: true,
  ownerLastName: true,
  ownerEmail: true,
  requestedPlan: true,
}

function validate(values: IRequestAccessFormValues): IRequestAccessFormErrors {
  const errors: IRequestAccessFormErrors = {}

  if (!isRequired(values.requestedPlan)) {
    errors.requestedPlan = 'Please select a plan before continuing.'
  }

  if (!isRequired(values.businessName)) {
    errors.businessName = 'Business name is required.'
  }

  if (!isRequired(values.businessType)) {
    errors.businessType = 'Business type is required.'
  }

  if (!isRequired(values.slug)) {
    errors.slug = 'Slug is required.'
  } else if (!SLUG_PATTERN.test(values.slug)) {
    errors.slug = 'Use lowercase letters, numbers, and hyphens only.'
  }

  if (!isRequired(values.ownerFirstName)) {
    errors.ownerFirstName = 'First name is required.'
  }

  if (!isRequired(values.ownerLastName)) {
    errors.ownerLastName = 'Last name is required.'
  }

  if (!isRequired(values.ownerEmail)) {
    errors.ownerEmail = 'Email address is required.'
  } else if (!isValidEmail(values.ownerEmail)) {
    errors.ownerEmail = 'Enter a valid email address.'
  }

  return errors
}

export function RequestAccessPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const { showToast } = useToast()
  const selectedPlanId = searchParams.get('plan')
  const selectedPlan = PRICING_PLANS.find((plan) => plan.id === selectedPlanId)
  const [values, setValues] = useState<IRequestAccessFormValues>(() => ({
    ...INITIAL_VALUES,
    requestedPlan: selectedPlan?.name ?? '',
  }))
  const [errors, setErrors] = useState<IRequestAccessFormErrors>({})
  const [touched, setTouched] = useState<IRequestAccessFormTouched>({})
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    const nextPlan = PRICING_PLANS.find((plan) => plan.id === selectedPlanId)?.name ?? ''

    setValues((currentValues) => ({
      ...currentValues,
      requestedPlan: nextPlan,
    }))

    if (!nextPlan) {
      navigate('/#pricing', { replace: true })
    }
  }, [navigate, selectedPlanId])

  const handleFieldChange = (field: RequestAccessField, value: string) => {
    const nextValues = { ...values, [field]: value }
    setValues(nextValues)
    setErrors(validate(nextValues))
  }

  const handleBlur = (field: RequestAccessField) => {
    setTouched((prev) => ({ ...prev, [field]: true }))
  }

  const isEmptyRequired = (field: RequestAccessField) => Boolean(touched[field]) && !isRequired(values[field] as string)

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
      navigate('/request-access/pending', {
        state: { businessName: values.businessName, ownerEmail: values.ownerEmail },
      })
    } catch {
      showToast('error', 'We could not submit your request. Please check your details and try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const isSlugValid = values.slug.length > 0 && !errors.slug

  return (
    <div className="request-access-shell">
      <header className="request-access-topbar">
        <div className="container">
          <Link to="/" className="d-inline-flex align-items-center gap-2 text-decoration-none">
            <img src="/favicon.svg" alt="ApexBooking logo" width={28} height={28} />
            <span className="fw-bold fs-5 font-display text-dark">ApexBooking</span>
          </Link>
        </div>
      </header>

      <div className="container py-4 py-md-5">
        <ProgressTracker currentStep="Details" />

        <h1 className="h3 fw-bold mb-1">Request Access</h1>
        <p className="text-secondary mb-4">Tell us about your business to get your booking page set up.</p>

        <div className="row gy-4">
          <div className="col-md-4">
            <Reveal>{selectedPlan && <PlanSummaryCard plan={selectedPlan} />}</Reveal>
          </div>

          <div className="col-md-8">
            <form noValidate onSubmit={handleSubmit}>
              <Reveal delayStep={1}>
                <FormSectionHeader icon="business-profile" label="Business details" />

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
                    className={`form-control ${touched.businessName && errors.businessName ? 'is-invalid' : ''} ${isEmptyRequired('businessName') ? 'border-start border-danger border-3' : ''}`}
                    value={values.businessName}
                    onChange={(e) => handleFieldChange('businessName', e.target.value)}
                    onBlur={() => handleBlur('businessName')}
                    aria-invalid={touched.businessName && !!errors.businessName}
                    aria-describedby={touched.businessName && errors.businessName ? 'businessName-error' : undefined}
                  />
                </FormGroup>

                <FormGroup
                  label="Business Type"
                  htmlFor="businessType"
                  required
                  error={touched.businessType ? errors.businessType : undefined}
                >
                  <BusinessTypeSelector
                    id="businessType"
                    value={values.businessType}
                    onChange={(value) => handleFieldChange('businessType', value)}
                    onSelect={() => handleBlur('businessType')}
                    isInvalid={!!touched.businessType && !!errors.businessType}
                  />
                </FormGroup>

                <FormGroup label="Slug" htmlFor="slug" required error={touched.slug ? errors.slug : undefined}>
                  <input
                    type="text"
                    id="slug"
                    name="slug"
                    className={`form-control ${touched.slug && errors.slug ? 'is-invalid' : ''} ${isEmptyRequired('slug') ? 'border-start border-danger border-3' : ''}`}
                    value={values.slug}
                    onChange={(e) => handleFieldChange('slug', e.target.value.toLowerCase())}
                    onBlur={() => handleBlur('slug')}
                    aria-invalid={touched.slug && !!errors.slug}
                    aria-describedby={touched.slug && errors.slug ? 'slug-error' : 'slug-help'}
                  />
                  {!(touched.slug && errors.slug) && <SlugValidationHint slug={values.slug} isValid={isSlugValid} />}
                </FormGroup>
              </Reveal>

              <Reveal delayStep={2}>
                <FormSectionHeader icon="user-check" label="Owner details" />

                <div className="row">
                  <div className="col-sm-6">
                    <FormGroup
                      label="First Name"
                      htmlFor="ownerFirstName"
                      required
                      error={touched.ownerFirstName ? errors.ownerFirstName : undefined}
                    >
                      <input
                        type="text"
                        id="ownerFirstName"
                        name="ownerFirstName"
                        className={`form-control ${touched.ownerFirstName && errors.ownerFirstName ? 'is-invalid' : ''} ${isEmptyRequired('ownerFirstName') ? 'border-start border-danger border-3' : ''}`}
                        value={values.ownerFirstName}
                        onChange={(e) => handleFieldChange('ownerFirstName', e.target.value)}
                        onBlur={() => handleBlur('ownerFirstName')}
                        aria-invalid={touched.ownerFirstName && !!errors.ownerFirstName}
                        aria-describedby={touched.ownerFirstName && errors.ownerFirstName ? 'ownerFirstName-error' : undefined}
                      />
                    </FormGroup>
                  </div>
                  <div className="col-sm-6">
                    <FormGroup
                      label="Last Name"
                      htmlFor="ownerLastName"
                      required
                      error={touched.ownerLastName ? errors.ownerLastName : undefined}
                    >
                      <input
                        type="text"
                        id="ownerLastName"
                        name="ownerLastName"
                        className={`form-control ${touched.ownerLastName && errors.ownerLastName ? 'is-invalid' : ''} ${isEmptyRequired('ownerLastName') ? 'border-start border-danger border-3' : ''}`}
                        value={values.ownerLastName}
                        onChange={(e) => handleFieldChange('ownerLastName', e.target.value)}
                        onBlur={() => handleBlur('ownerLastName')}
                        aria-invalid={touched.ownerLastName && !!errors.ownerLastName}
                        aria-describedby={touched.ownerLastName && errors.ownerLastName ? 'ownerLastName-error' : undefined}
                      />
                    </FormGroup>
                  </div>
                </div>

                <FormGroup
                  label="Email Address"
                  htmlFor="ownerEmail"
                  required
                  error={touched.ownerEmail ? errors.ownerEmail : undefined}
                >
                  <input
                    type="email"
                    id="ownerEmail"
                    name="ownerEmail"
                    className={`form-control ${touched.ownerEmail && errors.ownerEmail ? 'is-invalid' : ''} ${isEmptyRequired('ownerEmail') ? 'border-start border-danger border-3' : ''}`}
                    value={values.ownerEmail}
                    onChange={(e) => handleFieldChange('ownerEmail', e.target.value)}
                    onBlur={() => handleBlur('ownerEmail')}
                    aria-invalid={touched.ownerEmail && !!errors.ownerEmail}
                    aria-describedby={touched.ownerEmail && errors.ownerEmail ? 'ownerEmail-error' : undefined}
                  />
                </FormGroup>
              </Reveal>

              <Reveal delayStep={3} className="d-flex flex-column gap-3 mt-4">
                <Button type="submit" isLoading={isSubmitting} fullWidth>
                  {isSubmitting ? 'Submitting...' : 'Request Access'}
                </Button>
                <Button to="/find-workspace" variant="link" className="text-center">
                  Back to Login
                </Button>
              </Reveal>
            </form>
          </div>
        </div>
      </div>
    </div>
  )
}
