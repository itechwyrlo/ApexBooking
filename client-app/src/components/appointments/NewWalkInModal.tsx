import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { FormSection } from '../common/FormSection'
import { Button } from '../common/Button'
import { WalkInStaffCard } from './WalkInStaffCard'
import { useToast } from '../../hooks/useToast'
import { useBranches } from '../../hooks/useBranches'
import { useServicesByBranch } from '../../hooks/useServicesByBranch'
import { useWalkInAvailableStaff } from '../../hooks/useWalkInAvailableStaff'
import { useCustomerSearch } from '../../hooks/useCustomerSearch'
import { scheduleBooking } from '../../services/bookingService'
import { isRequired, isValidEmail } from '../../utils/validators'
import { getBranchLocalNow } from '../../utils/branchTime'
import type { IScheduleBookingValues } from '../../interfaces/ITenantBooking'
import type { ICustomer } from '../../interfaces/ICustomer'

const EMPTY_VALUES: IScheduleBookingValues = {
  branchId: '',
  staffId: '',
  serviceId: '',
  customerFirstName: '',
  customerLastName: '',
  customerEmail: '',
  customerPhone: '',
  scheduledDate: '',
  scheduledStartTime: '',
  customerNotes: '',
  admitImmediately: false,
}

type WalkInField = keyof IScheduleBookingValues
type IFormErrors = Partial<Record<WalkInField, string>>

function validate(values: IScheduleBookingValues, selectedTime: string | null): IFormErrors {
  const errors: IFormErrors = {}

  if (!isRequired(values.branchId)) errors.branchId = 'Select a branch.'
  if (!isRequired(values.serviceId)) errors.serviceId = 'Select a service.'
  if (!isRequired(values.staffId) || !selectedTime) errors.staffId = 'Select an available team member and time.'
  if (!isRequired(values.customerFirstName)) errors.customerFirstName = 'First name is required.'
  if (!isRequired(values.customerLastName)) errors.customerLastName = 'Last name is required.'
  if (!isRequired(values.customerEmail) && !isRequired(values.customerPhone)) {
    errors.customerEmail = 'Provide an email or phone number.'
  }
  if (isRequired(values.customerEmail) && !isValidEmail(values.customerEmail)) {
    errors.customerEmail = 'Enter a valid email address.'
  }

  return errors
}

function splitName(fullName: string): { firstName: string; lastName: string } {
  const trimmed = fullName.trim()
  const spaceIndex = trimmed.indexOf(' ')
  if (spaceIndex === -1) return { firstName: trimmed, lastName: '' }
  return { firstName: trimmed.slice(0, spaceIndex), lastName: trimmed.slice(spaceIndex + 1) }
}

interface INewWalkInModalProps {
  isOpen: boolean
  onClose: () => void
  onScheduled: () => void
}

export function NewWalkInModal({ isOpen, onClose, onScheduled }: INewWalkInModalProps) {
  const { showToast } = useToast()
  const { branches } = useBranches()
  const [values, setValues] = useState<IScheduleBookingValues>(EMPTY_VALUES)
  const [errors, setErrors] = useState<IFormErrors>({})
  const [touched, setTouched] = useState<Partial<Record<WalkInField, boolean>>>({})
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [selectedTime, setSelectedTime] = useState<string | null>(null)

  const [selectedCustomer, setSelectedCustomer] = useState<ICustomer | null>(null)
  const [customerSearchTerm, setCustomerSearchTerm] = useState('')
  const { results: customerResults } = useCustomerSearch(customerSearchTerm)

  const { services: activeServices } = useServicesByBranch(values.branchId || null)
  const { staff: availableStaff, isLoading: isLoadingStaff, refetch: refetchStaff } = useWalkInAvailableStaff(
    values.branchId || null,
    values.serviceId || null,
  )
  const recommendedIndex = availableStaff.findIndex((s) => s.recommendedTimeRaw !== null)

  useEffect(() => {
    if (isOpen) {
      setValues(EMPTY_VALUES)
      setErrors({})
      setTouched({})
      setSelectedTime(null)
      setSelectedCustomer(null)
      setCustomerSearchTerm('')
    }
  }, [isOpen])

  // Single-branch tenants never need to see a selector — preselect it, same convenience the
  // public wizard already applies when there's only one branch to choose from.
  useEffect(() => {
    if (isOpen && branches.length === 1 && !values.branchId) {
      setValues((prev) => ({ ...prev, branchId: branches[0].id }))
    }
  }, [isOpen, branches, values.branchId])

  const handleFieldChange = <K extends WalkInField>(field: K, value: IScheduleBookingValues[K]) => {
    const nextValues = { ...values, [field]: value }
    if (field === 'branchId') {
      nextValues.serviceId = ''
      nextValues.staffId = ''
      setSelectedTime(null)
    } else if (field === 'serviceId') {
      nextValues.staffId = ''
      setSelectedTime(null)
    }
    setValues(nextValues)
    setErrors(validate(nextValues, field === 'branchId' || field === 'serviceId' ? null : selectedTime))
  }

  const handleBlur = (field: WalkInField) => {
    setTouched((prev) => ({ ...prev, [field]: true }))
  }

  const handleSelectStaff = (staffId: string, recommendedRawTime: string | null) => {
    setValues((prev) => ({ ...prev, staffId }))
    setSelectedTime(recommendedRawTime)
    setErrors(validate({ ...values, staffId }, recommendedRawTime))
  }

  const handleSelectTime = (rawTime: string) => {
    setSelectedTime(rawTime)
    setErrors(validate(values, rawTime))
  }

  const handleSelectCustomer = (customer: ICustomer) => {
    const { firstName, lastName } = splitName(customer.name)
    setSelectedCustomer(customer)
    setCustomerSearchTerm('')
    setValues((prev) => ({
      ...prev,
      customerFirstName: firstName,
      customerLastName: lastName,
      customerEmail: customer.email ?? '',
      customerPhone: customer.phoneNumber ?? '',
    }))
  }

  const handleClearCustomer = () => {
    setSelectedCustomer(null)
    setValues((prev) => ({ ...prev, customerFirstName: '', customerLastName: '', customerEmail: '', customerPhone: '' }))
  }

  const selectedBranch = branches.find((branch) => branch.id === values.branchId)
  const selectedService = activeServices.find((service) => service.id === values.serviceId)
  const selectedStaff = availableStaff.find((member) => member.tenantMemberId === values.staffId)
  const selectedTimeDisplay =
    selectedStaff && selectedTime
      ? (selectedTime === selectedStaff.recommendedTimeRaw
          ? selectedStaff.recommendedTimeDisplay
          : selectedStaff.alternateTimes.find((t) => t.raw === selectedTime)?.display) ?? null
      : null

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validate(values, selectedTime)
    setErrors(validationErrors)
    setTouched({
      branchId: true,
      serviceId: true,
      staffId: true,
      customerFirstName: true,
      customerLastName: true,
      customerEmail: true,
    })

    if (Object.keys(validationErrors).length > 0 || !selectedTime) {
      return
    }

    setIsSubmitting(true)
    try {
      const { date } = getBranchLocalNow(selectedBranch?.timeZoneId ?? 'Asia/Manila')
      // Only the immediate "available now" slot auto-admits — a later-today slot on an
      // otherwise-busy staff member stays a normal Scheduled booking, admitted when they arrive.
      const admitImmediately = selectedStaff?.isAvailableNow === true && selectedTime === selectedStaff.recommendedTimeRaw
      await scheduleBooking({ ...values, admitImmediately, scheduledDate: date, scheduledStartTime: selectedTime })
      showToast('success', `Walk-in appointment created for ${values.customerFirstName} ${values.customerLastName}.`)
      onScheduled()
      onClose()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to create this walk-in appointment. Please try again.')
      // Availability may have changed between selection and submission (e.g. someone else just
      // booked this slot) — refresh so the staff/time list reflects reality instead of staying stale.
      refetchStaff()
      setSelectedTime(null)
      setValues((prev) => ({ ...prev, staffId: '' }))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal
      isOpen={isOpen}
      title="New Walk-in Appointment"
      description="Schedule an appointment for a customer who walked in without a booking."
      onClose={onClose}
    >
      <form noValidate onSubmit={handleSubmit}>
        <FormSection title="Branch & Service">
          <div className="row">
            <div className="col-sm-6">
              <FormGroup label="Branch" htmlFor="walkInBranch" required error={touched.branchId ? errors.branchId : undefined}>
                <select
                  id="walkInBranch"
                  className={`form-select ${touched.branchId && errors.branchId ? 'is-invalid' : ''}`}
                  value={values.branchId}
                  disabled={branches.length === 1}
                  onChange={(e) => handleFieldChange('branchId', e.target.value)}
                  onBlur={() => handleBlur('branchId')}
                >
                  <option value="">Select branch</option>
                  {branches.map((branch) => (
                    <option key={branch.id} value={branch.id}>
                      {branch.name}
                    </option>
                  ))}
                </select>
              </FormGroup>
            </div>
            <div className="col-sm-6">
              <FormGroup label="Service" htmlFor="walkInService" required error={touched.serviceId ? errors.serviceId : undefined}>
                <select
                  id="walkInService"
                  className={`form-select ${touched.serviceId && errors.serviceId ? 'is-invalid' : ''}`}
                  value={values.serviceId}
                  onChange={(e) => handleFieldChange('serviceId', e.target.value)}
                  onBlur={() => handleBlur('serviceId')}
                >
                  <option value="">Select service</option>
                  {activeServices.map((service) => (
                    <option key={service.id} value={service.id}>
                      {service.name} ({service.durationMinutes} min)
                    </option>
                  ))}
                </select>
              </FormGroup>
            </div>
          </div>
        </FormSection>

        <hr className="my-4" />

        <FormSection title="Customer" description="Search for a returning customer, or fill in the details below for a new one.">
          <div className="position-relative mb-3">
            <FormGroup label="Search Existing Customer" htmlFor="walkInCustomerSearch">
              <input
                type="text"
                id="walkInCustomerSearch"
                className="form-control"
                placeholder="Search by name, email, or phone"
                value={customerSearchTerm}
                onChange={(e) => setCustomerSearchTerm(e.target.value)}
                autoComplete="off"
              />
            </FormGroup>
            {customerSearchTerm.trim().length >= 2 && customerResults.length > 0 && (
              <ul className="list-group position-absolute w-100 shadow-sm" style={{ zIndex: 5 }}>
                {customerResults.map((customer) => (
                  <li key={customer.id} className="list-group-item list-group-item-action" role="button" onClick={() => handleSelectCustomer(customer)}>
                    <div className="fw-semibold">{customer.name}</div>
                    <div className="text-muted small">{customer.email ?? customer.phoneNumber}</div>
                  </li>
                ))}
              </ul>
            )}
            {selectedCustomer && (
              <div className="small text-muted mt-1">
                Using existing client: <strong>{selectedCustomer.name}</strong>{' '}
                <button type="button" className="btn btn-link btn-sm p-0 align-baseline" onClick={handleClearCustomer}>
                  Change
                </button>
              </div>
            )}
          </div>

          <div className="row">
            <div className="col-sm-6">
              <FormGroup
                label="First Name"
                htmlFor="walkInFirstName"
                required
                error={touched.customerFirstName ? errors.customerFirstName : undefined}
              >
                <input
                  type="text"
                  id="walkInFirstName"
                  className={`form-control ${touched.customerFirstName && errors.customerFirstName ? 'is-invalid' : ''}`}
                  value={values.customerFirstName}
                  readOnly={!!selectedCustomer}
                  onChange={(e) => handleFieldChange('customerFirstName', e.target.value)}
                  onBlur={() => handleBlur('customerFirstName')}
                />
              </FormGroup>
            </div>
            <div className="col-sm-6">
              <FormGroup
                label="Last Name"
                htmlFor="walkInLastName"
                required
                error={touched.customerLastName ? errors.customerLastName : undefined}
              >
                <input
                  type="text"
                  id="walkInLastName"
                  className={`form-control ${touched.customerLastName && errors.customerLastName ? 'is-invalid' : ''}`}
                  value={values.customerLastName}
                  readOnly={!!selectedCustomer}
                  onChange={(e) => handleFieldChange('customerLastName', e.target.value)}
                  onBlur={() => handleBlur('customerLastName')}
                />
              </FormGroup>
            </div>
          </div>

          <div className="row">
            <div className="col-sm-6">
              <FormGroup label="Email" htmlFor="walkInEmail" error={touched.customerEmail ? errors.customerEmail : undefined}>
                <input
                  type="email"
                  id="walkInEmail"
                  className={`form-control ${touched.customerEmail && errors.customerEmail ? 'is-invalid' : ''}`}
                  value={values.customerEmail}
                  readOnly={!!selectedCustomer}
                  onChange={(e) => handleFieldChange('customerEmail', e.target.value)}
                  onBlur={() => handleBlur('customerEmail')}
                />
              </FormGroup>
            </div>
            <div className="col-sm-6">
              <FormGroup label="Phone" htmlFor="walkInPhone">
                <input
                  type="tel"
                  id="walkInPhone"
                  className="form-control"
                  value={values.customerPhone}
                  readOnly={!!selectedCustomer}
                  onChange={(e) => handleFieldChange('customerPhone', e.target.value)}
                />
              </FormGroup>
            </div>
          </div>

          <FormGroup label="Notes" htmlFor="walkInNotes">
            <textarea
              id="walkInNotes"
              rows={2}
              className="form-control"
              value={values.customerNotes}
              onChange={(e) => handleFieldChange('customerNotes', e.target.value)}
            />
          </FormGroup>
        </FormSection>

        <hr className="my-4" />

        <FormSection title="Staff & Time" description="Team members who can perform this service, ranked by soonest availability.">
          {!values.branchId || !values.serviceId ? (
            <p className="text-muted small">Select a branch and service first.</p>
          ) : isLoadingStaff ? (
            <p className="text-muted small">Checking availability...</p>
          ) : availableStaff.length === 0 ? (
            <p className="text-muted small">No team members offer this service at the selected branch.</p>
          ) : (
            <>
              {availableStaff.every((s) => s.recommendedTimeRaw === null) && (
                <p className="text-muted small">No team members are available for this service today.</p>
              )}
              {availableStaff.map((staffMember, index) => (
                <WalkInStaffCard
                  key={staffMember.tenantMemberId}
                  staff={staffMember}
                  isRecommended={index === recommendedIndex}
                  isSelected={values.staffId === staffMember.tenantMemberId}
                  selectedTime={values.staffId === staffMember.tenantMemberId ? selectedTime : null}
                  onSelectStaff={() => handleSelectStaff(staffMember.tenantMemberId, staffMember.recommendedTimeRaw)}
                  onSelectTime={handleSelectTime}
                />
              ))}
            </>
          )}
          {touched.staffId && errors.staffId && <div className="text-danger small mt-1">{errors.staffId}</div>}
        </FormSection>

        {values.staffId && selectedTime && selectedService && selectedBranch && (
          <>
            <hr className="my-4" />
            <FormSection title="Appointment Summary">
              <dl className="row small mb-0">
                <dt className="col-sm-4 text-muted fw-normal">Branch</dt>
                <dd className="col-sm-8">{selectedBranch.name}</dd>
                <dt className="col-sm-4 text-muted fw-normal">Customer</dt>
                <dd className="col-sm-8">{`${values.customerFirstName} ${values.customerLastName}`.trim() || '—'}</dd>
                <dt className="col-sm-4 text-muted fw-normal">Service</dt>
                <dd className="col-sm-8">
                  {selectedService.name} ({selectedService.durationMinutes} min)
                </dd>
                <dt className="col-sm-4 text-muted fw-normal">Staff</dt>
                <dd className="col-sm-8">{selectedStaff?.fullName}</dd>
                <dt className="col-sm-4 text-muted fw-normal">Time</dt>
                <dd className="col-sm-8">Today, {selectedTimeDisplay}</dd>
              </dl>
            </FormSection>
          </>
        )}

        <div className="modal-form-actions">
          <Button type="button" variant="outline-secondary" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" isLoading={isSubmitting}>
            {isSubmitting ? 'Creating...' : 'Create Appointment'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
