import { useState, type FormEvent } from 'react'
import { isRequired, isValidEmail } from '../../utils/validators'
import type { IBookingContactValues } from '../../interfaces/publicBooking/IInitiateBookingValues'
import type { WizardDirection } from '../../hooks/usePublicBookingWizard'

interface ICustomerInfoStepProps {
  initialValues: IBookingContactValues | null
  direction: WizardDirection
  onContinue: (values: IBookingContactValues) => void
}

const EMPTY_VALUES: IBookingContactValues = {
  customerFirstName: '',
  customerLastName: '',
  customerEmail: '',
  customerPhone: '',
  customerNotes: '',
}

type ContactField = keyof IBookingContactValues
type IFormErrors = Partial<Record<ContactField, string>>

function validate(values: IBookingContactValues): IFormErrors {
  const errors: IFormErrors = {}

  if (!isRequired(values.customerFirstName)) errors.customerFirstName = 'First name is required.'
  if (!isRequired(values.customerLastName)) errors.customerLastName = 'Last name is required.'

  if (!isRequired(values.customerEmail)) {
    errors.customerEmail = 'Email address is required.'
  } else if (!isValidEmail(values.customerEmail)) {
    errors.customerEmail = 'Enter a valid email address.'
  }

  if (!isRequired(values.customerPhone)) errors.customerPhone = 'Phone number is required.'

  return errors
}

export function CustomerInfoStep({ initialValues, direction, onContinue }: ICustomerInfoStepProps) {
  const [values, setValues] = useState<IBookingContactValues>(initialValues ?? EMPTY_VALUES)
  const [errors, setErrors] = useState<IFormErrors>({})
  const [touched, setTouched] = useState<Partial<Record<ContactField, boolean>>>({})

  const handleChange = (field: ContactField, value: string) => {
    const next = { ...values, [field]: value }
    setValues(next)
    setErrors(validate(next))
  }

  const handleBlur = (field: ContactField) => {
    setTouched((prev) => ({ ...prev, [field]: true }))
  }

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validate(values)
    setErrors(validationErrors)
    setTouched({ customerFirstName: true, customerLastName: true, customerEmail: true, customerPhone: true })

    if (Object.keys(validationErrors).length > 0) return

    onContinue(values)
  }

  return (
    <div className={`pb-step-enter-${direction}`}>
      <h1 className="pb-display fs-3 mb-1">Your information</h1>
      <p className="pb-muted mb-4">We'll use these details to confirm your appointment.</p>

      <form noValidate onSubmit={handleSubmit}>
        <div className="row g-3 mb-3">
          <div className="col-sm-6">
            <label htmlFor="pb-first-name" className="form-label small fw-semibold pb-muted">
              First Name
            </label>
            <input
              type="text"
              id="pb-first-name"
              className={`form-control ${touched.customerFirstName && errors.customerFirstName ? 'is-invalid' : ''}`}
              value={values.customerFirstName}
              onChange={(e) => handleChange('customerFirstName', e.target.value)}
              onBlur={() => handleBlur('customerFirstName')}
            />
            {touched.customerFirstName && errors.customerFirstName && (
              <div className="invalid-feedback d-block">{errors.customerFirstName}</div>
            )}
          </div>
          <div className="col-sm-6">
            <label htmlFor="pb-last-name" className="form-label small fw-semibold pb-muted">
              Last Name
            </label>
            <input
              type="text"
              id="pb-last-name"
              className={`form-control ${touched.customerLastName && errors.customerLastName ? 'is-invalid' : ''}`}
              value={values.customerLastName}
              onChange={(e) => handleChange('customerLastName', e.target.value)}
              onBlur={() => handleBlur('customerLastName')}
            />
            {touched.customerLastName && errors.customerLastName && (
              <div className="invalid-feedback d-block">{errors.customerLastName}</div>
            )}
          </div>
        </div>

        <div className="mb-3">
          <label htmlFor="pb-email" className="form-label small fw-semibold pb-muted">
            Email Address
          </label>
          <input
            type="email"
            id="pb-email"
            className={`form-control ${touched.customerEmail && errors.customerEmail ? 'is-invalid' : ''}`}
            value={values.customerEmail}
            onChange={(e) => handleChange('customerEmail', e.target.value)}
            onBlur={() => handleBlur('customerEmail')}
          />
          {touched.customerEmail && errors.customerEmail && <div className="invalid-feedback d-block">{errors.customerEmail}</div>}
        </div>

        <div className="mb-3">
          <label htmlFor="pb-phone" className="form-label small fw-semibold pb-muted">
            Phone Number
          </label>
          <input
            type="tel"
            id="pb-phone"
            className={`form-control ${touched.customerPhone && errors.customerPhone ? 'is-invalid' : ''}`}
            value={values.customerPhone}
            onChange={(e) => handleChange('customerPhone', e.target.value)}
            onBlur={() => handleBlur('customerPhone')}
          />
          {touched.customerPhone && errors.customerPhone && <div className="invalid-feedback d-block">{errors.customerPhone}</div>}
        </div>

        <div className="mb-4">
          <label htmlFor="pb-notes" className="form-label small fw-semibold pb-muted">
            Notes (optional)
          </label>
          <textarea
            id="pb-notes"
            className="form-control"
            rows={2}
            value={values.customerNotes}
            onChange={(e) => handleChange('customerNotes', e.target.value)}
          />
        </div>

        <button type="submit" className="btn pb-btn-primary w-100">
          Continue
        </button>
      </form>
    </div>
  )
}
