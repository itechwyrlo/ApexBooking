import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'
import { PageHeader } from '../../components/common/PageHeader'
import { Card } from '../../components/common/Card'
import { Button } from '../../components/common/Button'
import { FormGroup } from '../../components/common/FormGroup'
import { Skeleton } from '../../components/common/Skeleton'
import { useBusinessProfile } from '../../hooks/useBusinessProfile'
import { useBranches } from '../../hooks/useBranches'
import { useToast } from '../../hooks/useToast'
import { useAuth } from '../../hooks/useAuth'
import { updateBusinessProfile } from '../../services/businessProfileService'
import { buildDashboardPath } from '../../config/dashboardRoutes'
import { isRequired } from '../../utils/validators'
import type { IBusinessProfileValues } from '../../interfaces/IBusinessProfile'

const BUSINESS_NAME_MAX_LENGTH = 200
const DESCRIPTION_MAX_LENGTH = 1000

type ProfileField = keyof IBusinessProfileValues
type IFormErrors = Partial<Record<ProfileField, string>>
type IFormTouched = Partial<Record<ProfileField, boolean>>

const ALL_FIELDS_TOUCHED: IFormTouched = { businessName: true, description: true, contactPhoneNumber: true }

function validate(values: IBusinessProfileValues): IFormErrors {
  const errors: IFormErrors = {}

  if (!isRequired(values.businessName)) {
    errors.businessName = 'Business name is required.'
  } else if (values.businessName.length > BUSINESS_NAME_MAX_LENGTH) {
    errors.businessName = `Business name cannot exceed ${BUSINESS_NAME_MAX_LENGTH} characters.`
  }

  if (values.description.length > DESCRIPTION_MAX_LENGTH) {
    errors.description = `Description cannot exceed ${DESCRIPTION_MAX_LENGTH} characters.`
  }

  return errors
}

export function BusinessProfilePage() {
  const { profile, isLoading, refetch } = useBusinessProfile()
  const { branches, isLoading: isLoadingBranches } = useBranches()
  const { showToast } = useToast()
  const { user } = useAuth()
  const slug = user?.slug ?? ''
  const [values, setValues] = useState<IBusinessProfileValues>({ businessName: '', description: '', contactPhoneNumber: '' })
  const [errors, setErrors] = useState<IFormErrors>({})
  const [touched, setTouched] = useState<IFormTouched>({})
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (profile) {
      setValues({
        businessName: profile.businessName,
        description: profile.description ?? '',
        contactPhoneNumber: profile.contactPhoneNumber ?? '',
      })
    }
  }, [profile])

  const handleFieldChange = (field: ProfileField, value: string) => {
    const nextValues = { ...values, [field]: value }
    setValues(nextValues)
    setErrors(validate(nextValues))
  }

  const handleBlur = (field: ProfileField) => {
    setTouched((prev) => ({ ...prev, [field]: true }))
  }

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
      await updateBusinessProfile(values)
      showToast('success', 'Business profile updated.')
      refetch()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to update business profile. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div>
      <PageHeader
        title="Business Profile"
        description="Add your business details so customers know who they're booking with."
      />
      <Card>
        {isLoading ? (
          <div>
            <Skeleton height="2.4rem" className="mb-3" />
            <Skeleton height="6rem" />
          </div>
        ) : (
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
              />
            </FormGroup>

            <FormGroup label="Description" htmlFor="description" error={touched.description ? errors.description : undefined}>
              <textarea
                id="description"
                name="description"
                rows={4}
                className={`form-control ${touched.description && errors.description ? 'is-invalid' : ''}`}
                value={values.description}
                onChange={(e) => handleFieldChange('description', e.target.value)}
                onBlur={() => handleBlur('description')}
                aria-invalid={touched.description && !!errors.description}
              />
            </FormGroup>

            <FormGroup label="Contact Phone Number" htmlFor="contactPhoneNumber">
              <input
                type="tel"
                id="contactPhoneNumber"
                name="contactPhoneNumber"
                className="form-control"
                value={values.contactPhoneNumber}
                onChange={(e) => handleFieldChange('contactPhoneNumber', e.target.value)}
              />
              <div className="form-text">
                Shown to customers on the refund-status page — a business number, not a personal one.
              </div>
            </FormGroup>

            {profile && (
              <FormGroup label="Business Type" htmlFor="businessType">
                <input type="text" id="businessType" className="form-control" value={profile.businessType} disabled />
                <div className="form-text">Business type is set when your workspace is created and can't be changed here.</div>
              </FormGroup>
            )}

            <div className="d-flex justify-content-end mt-4">
              <Button type="submit" isLoading={isSubmitting}>
                {isSubmitting ? 'Saving...' : 'Save Changes'}
              </Button>
            </div>
          </form>
        )}
      </Card>

      <Card className="mt-3">
        <div className="d-flex align-items-center justify-content-between gap-3">
          <div>
            <h2 className="fs-6 fw-semibold mb-1">Branches</h2>
            <p className="text-muted small mb-0">
              {isLoadingBranches
                ? 'Loading your branches...'
                : `You have ${branches.length} ${branches.length === 1 ? 'branch' : 'branches'}.`}
            </p>
          </div>
          <Button to={buildDashboardPath(slug, 'branches')} variant="outline-secondary" size="sm">
            Manage Branches
          </Button>
        </div>
      </Card>
    </div>
  )
}
