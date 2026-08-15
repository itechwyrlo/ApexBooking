import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { FormSection } from '../common/FormSection'
import { Button } from '../common/Button'
import { useBranches } from '../../hooks/useBranches'
import { useToast } from '../../hooks/useToast'
import { addTeamMember } from '../../services/teamService'
import { isRequired, isValidEmail } from '../../utils/validators'
import { Role } from '../../types/Role'
import type { IAddTeamMemberValues } from '../../interfaces/IAddTeamMemberValues'

type TeamMemberField = keyof IAddTeamMemberValues

type IFormErrors = Partial<Record<TeamMemberField, string>>
type IFormTouched = Partial<Record<TeamMemberField, boolean>>

const INITIAL_VALUES: IAddTeamMemberValues = {
  branchId: '',
  firstName: '',
  lastName: '',
  email: '',
  contactNumber: '',
  role: '',
  customJobTitle: '',
}

const ALL_FIELDS_TOUCHED: IFormTouched = {
  branchId: true,
  firstName: true,
  lastName: true,
  email: true,
  role: true,
}

function validate(values: IAddTeamMemberValues): IFormErrors {
  const errors: IFormErrors = {}

  if (!isRequired(values.branchId)) {
    errors.branchId = 'Please select a branch.'
  }

  if (!isRequired(values.firstName)) {
    errors.firstName = 'First name is required.'
  }

  if (!isRequired(values.lastName)) {
    errors.lastName = 'Last name is required.'
  }

  if (!isRequired(values.email)) {
    errors.email = 'Email address is required.'
  } else if (!isValidEmail(values.email)) {
    errors.email = 'Enter a valid email address.'
  }

  if (!isRequired(values.role)) {
    errors.role = 'Please select a role.'
  }

  return errors
}

interface IAddTeamMemberModalProps {
  isOpen: boolean
  onClose: () => void
  onCreated: () => void
}

export function AddTeamMemberModal({ isOpen, onClose, onCreated }: IAddTeamMemberModalProps) {
  const { branches, isLoading: isLoadingBranches } = useBranches()
  const { showToast } = useToast()
  const [values, setValues] = useState<IAddTeamMemberValues>(INITIAL_VALUES)
  const [errors, setErrors] = useState<IFormErrors>({})
  const [touched, setTouched] = useState<IFormTouched>({})
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (isOpen) {
      setValues(INITIAL_VALUES)
      setErrors({})
      setTouched({})
    }
  }, [isOpen])

  const handleFieldChange = (field: TeamMemberField, value: string) => {
    const nextValues = { ...values, [field]: value }
    setValues(nextValues)
    setErrors(validate(nextValues))
  }

  const handleBlur = (field: TeamMemberField) => {
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
      await addTeamMember(values)
      showToast('success', `${values.firstName} ${values.lastName} was invited to your team.`)
      onCreated()
      onClose()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to add this team member. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal
      isOpen={isOpen}
      title="Add Team Member"
      description="Invite a new member to your team and assign their role."
      onClose={onClose}
    >
      <form noValidate onSubmit={handleSubmit}>
        <FormSection title="Personal Information">
          <div className="row">
            <div className="col-sm-6">
              <FormGroup
                label="First Name"
                htmlFor="firstName"
                required
                error={touched.firstName ? errors.firstName : undefined}
              >
                <input
                  type="text"
                  id="firstName"
                  name="firstName"
                  className={`form-control ${touched.firstName && errors.firstName ? 'is-invalid' : ''}`}
                  value={values.firstName}
                  onChange={(e) => handleFieldChange('firstName', e.target.value)}
                  onBlur={() => handleBlur('firstName')}
                  aria-invalid={touched.firstName && !!errors.firstName}
                />
              </FormGroup>
            </div>
            <div className="col-sm-6">
              <FormGroup label="Last Name" htmlFor="lastName" required error={touched.lastName ? errors.lastName : undefined}>
                <input
                  type="text"
                  id="lastName"
                  name="lastName"
                  className={`form-control ${touched.lastName && errors.lastName ? 'is-invalid' : ''}`}
                  value={values.lastName}
                  onChange={(e) => handleFieldChange('lastName', e.target.value)}
                  onBlur={() => handleBlur('lastName')}
                  aria-invalid={touched.lastName && !!errors.lastName}
                />
              </FormGroup>
            </div>
          </div>

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
            />
          </FormGroup>

          <FormGroup label="Contact Number" htmlFor="contactNumber">
            <input
              type="tel"
              id="contactNumber"
              name="contactNumber"
              className="form-control"
              value={values.contactNumber}
              onChange={(e) => handleFieldChange('contactNumber', e.target.value)}
            />
          </FormGroup>
        </FormSection>

        <hr className="my-4" />

        <FormSection title="Role & Assignment" description="Determines which branch this member works at and what they can access.">
          <FormGroup label="Branch" htmlFor="branchId" required error={touched.branchId ? errors.branchId : undefined}>
            <select
              id="branchId"
              name="branchId"
              className={`form-select ${touched.branchId && errors.branchId ? 'is-invalid' : ''}`}
              value={values.branchId}
              onChange={(e) => handleFieldChange('branchId', e.target.value)}
              onBlur={() => handleBlur('branchId')}
              disabled={isLoadingBranches}
              aria-invalid={touched.branchId && !!errors.branchId}
            >
              <option value="">{isLoadingBranches ? 'Loading branches...' : 'Select a branch'}</option>
              {branches.map((branch) => (
                <option key={branch.id} value={branch.id}>
                  {branch.name}
                </option>
              ))}
            </select>
          </FormGroup>

          <FormGroup label="Role" htmlFor="role" required error={touched.role ? errors.role : undefined}>
            <select
              id="role"
              name="role"
              className={`form-select ${touched.role && errors.role ? 'is-invalid' : ''}`}
              value={values.role}
              onChange={(e) => handleFieldChange('role', e.target.value)}
              onBlur={() => handleBlur('role')}
              aria-invalid={touched.role && !!errors.role}
            >
              <option value="">Select a role</option>
              <option value={Role.Admin}>Admin</option>
              <option value={Role.Staff}>Staff</option>
            </select>
          </FormGroup>

          <FormGroup label="Job Title" htmlFor="customJobTitle">
            <input
              type="text"
              id="customJobTitle"
              name="customJobTitle"
              className="form-control"
              placeholder="e.g. Senior Stylist"
              value={values.customJobTitle}
              onChange={(e) => handleFieldChange('customJobTitle', e.target.value)}
            />
          </FormGroup>
        </FormSection>

        <div className="modal-form-actions">
          <Button type="button" variant="outline-secondary" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" isLoading={isSubmitting}>
            {isSubmitting ? 'Adding...' : 'Add Team Member'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
