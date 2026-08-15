import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'
import { PageHeader } from '../../../components/common/PageHeader'
import { Card } from '../../../components/common/Card'
import { Button } from '../../../components/common/Button'
import { FormGroup } from '../../../components/common/FormGroup'
import { FormSection } from '../../../components/common/FormSection'
import { NumberInput } from '../../../components/common/NumberInput'
import { Skeleton } from '../../../components/common/Skeleton'
import { useBookingPolicy } from '../../../hooks/useBookingPolicy'
import { useToast } from '../../../hooks/useToast'
import { updateBookingPolicy } from '../../../services/bookingPolicyService'
import { BookingConfirmationMode } from '../../../types/BookingConfirmationMode'
import { CancellationPolicy } from '../../../types/CancellationPolicy'
import type { IBookingPolicy } from '../../../interfaces/IBookingPolicy'

const DEFAULT_VALUES: IBookingPolicy = {
  bookingConfirmationMode: BookingConfirmationMode.Automatic,
  minAdvanceBookingHours: 0,
  maxAdvanceBookingDays: 1,
  cancellationCutoffHours: 0,
  lateCancellationPolicy: CancellationPolicy.NoRefund,
  notifyBookingConfirmed: true,
  notifyBookingCancelled: true,
  notifyBookingReminder: true,
  notifyNewCustomer: true,
  reminderHoursBefore: 0,
}

type NumericField = 'minAdvanceBookingHours' | 'maxAdvanceBookingDays' | 'cancellationCutoffHours' | 'reminderHoursBefore'
type BooleanField =
  | 'notifyBookingConfirmed'
  | 'notifyBookingCancelled'
  | 'notifyBookingReminder'
  | 'notifyNewCustomer'

type IFormErrors = Partial<Record<NumericField, string>>

function validate(values: IBookingPolicy): IFormErrors {
  const errors: IFormErrors = {}

  if (values.minAdvanceBookingHours < 0) {
    errors.minAdvanceBookingHours = 'Minimum advance booking hours cannot be negative.'
  }
  if (values.maxAdvanceBookingDays <= 0) {
    errors.maxAdvanceBookingDays = 'Maximum advance booking days must be greater than zero.'
  }
  if (values.cancellationCutoffHours < 0) {
    errors.cancellationCutoffHours = 'Cancellation cutoff hours cannot be negative.'
  }
  if (values.reminderHoursBefore < 0) {
    errors.reminderHoursBefore = 'Reminder hours before cannot be negative.'
  }

  return errors
}

export function BookingSettingsPage() {
  const { policy, isLoading, refetch } = useBookingPolicy()
  const { showToast } = useToast()
  const [values, setValues] = useState<IBookingPolicy>(DEFAULT_VALUES)
  const [errors, setErrors] = useState<IFormErrors>({})
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (policy) {
      setValues(policy)
    }
  }, [policy])

  const handleNumberChange = (field: NumericField, nextValue: number) => {
    const nextValues = { ...values, [field]: nextValue }
    setValues(nextValues)
    setErrors(validate(nextValues))
  }

  const handleToggle = (field: BooleanField) => {
    setValues((prev) => ({ ...prev, [field]: !prev[field] }))
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validate(values)
    setErrors(validationErrors)

    if (Object.keys(validationErrors).length > 0) {
      return
    }

    setIsSubmitting(true)
    try {
      await updateBookingPolicy(values)
      showToast('success', 'Booking settings updated.')
      refetch()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to update booking settings. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  if (isLoading) {
    return (
      <Card>
        <Skeleton height="2.4rem" className="mb-3" />
        <Skeleton height="2.4rem" className="mb-3" />
        <Skeleton height="2.4rem" />
      </Card>
    )
  }

  return (
    <div>
      <PageHeader
        title="Booking Settings"
        description="Control booking rules like lead time, cancellations, and availability windows."
      />
      <Card>
        <form noValidate onSubmit={handleSubmit}>
          <FormSection title="Booking Rules" description="Control how far ahead customers can book and how confirmations work.">
            <FormGroup label="Booking Confirmation" htmlFor="bookingConfirmationMode">
              <select
                id="bookingConfirmationMode"
                className="form-select"
                value={values.bookingConfirmationMode}
                onChange={(e) =>
                  setValues((prev) => ({ ...prev, bookingConfirmationMode: e.target.value as IBookingPolicy['bookingConfirmationMode'] }))
                }
              >
                <option value={BookingConfirmationMode.Automatic}>Automatic</option>
                <option value={BookingConfirmationMode.Manual}>Manual (requires approval)</option>
              </select>
            </FormGroup>

            <div className="row">
              <div className="col-sm-6">
                <FormGroup
                  label="Minimum Advance Booking (hours)"
                  htmlFor="minAdvanceBookingHours"
                  error={errors.minAdvanceBookingHours}
                >
                  <NumberInput
                    id="minAdvanceBookingHours"
                    min={0}
                    isInvalid={!!errors.minAdvanceBookingHours}
                    value={values.minAdvanceBookingHours}
                    onChange={(value) => handleNumberChange('minAdvanceBookingHours', value)}
                  />
                </FormGroup>
              </div>
              <div className="col-sm-6">
                <FormGroup
                  label="Maximum Advance Booking (days)"
                  htmlFor="maxAdvanceBookingDays"
                  error={errors.maxAdvanceBookingDays}
                >
                  <NumberInput
                    id="maxAdvanceBookingDays"
                    min={1}
                    isInvalid={!!errors.maxAdvanceBookingDays}
                    value={values.maxAdvanceBookingDays}
                    onChange={(value) => handleNumberChange('maxAdvanceBookingDays', value)}
                  />
                </FormGroup>
              </div>
            </div>
          </FormSection>

          <hr className="my-4" />

          <FormSection title="Cancellations & Reminders">
            <div className="row">
              <div className="col-sm-6">
                <FormGroup
                  label="Cancellation Cutoff (hours)"
                  htmlFor="cancellationCutoffHours"
                  error={errors.cancellationCutoffHours}
                >
                  <NumberInput
                    id="cancellationCutoffHours"
                    min={0}
                    isInvalid={!!errors.cancellationCutoffHours}
                    value={values.cancellationCutoffHours}
                    onChange={(value) => handleNumberChange('cancellationCutoffHours', value)}
                  />
                </FormGroup>
              </div>
              <div className="col-sm-6">
                <FormGroup label="Late Cancellation Policy" htmlFor="lateCancellationPolicy">
                  <select
                    id="lateCancellationPolicy"
                    className="form-select"
                    value={values.lateCancellationPolicy}
                    onChange={(e) =>
                      setValues((prev) => ({
                        ...prev,
                        lateCancellationPolicy: e.target.value as IBookingPolicy['lateCancellationPolicy'],
                      }))
                    }
                  >
                    <option value={CancellationPolicy.NoRefund}>No Refund</option>
                    <option value={CancellationPolicy.PartialRefund}>Partial Refund</option>
                    <option value={CancellationPolicy.FullRefund}>Full Refund</option>
                  </select>
                </FormGroup>
              </div>
            </div>

            <FormGroup label="Reminder Sent (hours before)" htmlFor="reminderHoursBefore" error={errors.reminderHoursBefore}>
              <NumberInput
                id="reminderHoursBefore"
                min={0}
                isInvalid={!!errors.reminderHoursBefore}
                value={values.reminderHoursBefore}
                onChange={(value) => handleNumberChange('reminderHoursBefore', value)}
              />
            </FormGroup>
          </FormSection>

          <hr className="my-4" />

          <FormSection title="Notifications" description="Choose which booking events send an automatic notification.">
            <div className="form-check form-switch mb-2">
              <input
                type="checkbox"
                className="form-check-input"
                id="notifyBookingConfirmed"
                checked={values.notifyBookingConfirmed}
                onChange={() => handleToggle('notifyBookingConfirmed')}
              />
              <label className="form-check-label" htmlFor="notifyBookingConfirmed">
                Notify when a booking is confirmed
              </label>
            </div>

            <div className="form-check form-switch mb-2">
              <input
                type="checkbox"
                className="form-check-input"
                id="notifyBookingCancelled"
                checked={values.notifyBookingCancelled}
                onChange={() => handleToggle('notifyBookingCancelled')}
              />
              <label className="form-check-label" htmlFor="notifyBookingCancelled">
                Notify when a booking is cancelled
              </label>
            </div>

            <div className="form-check form-switch mb-2">
              <input
                type="checkbox"
                className="form-check-input"
                id="notifyBookingReminder"
                checked={values.notifyBookingReminder}
                onChange={() => handleToggle('notifyBookingReminder')}
              />
              <label className="form-check-label" htmlFor="notifyBookingReminder">
                Send booking reminders
              </label>
            </div>

            <div className="form-check form-switch mb-0">
              <input
                type="checkbox"
                className="form-check-input"
                id="notifyNewCustomer"
                checked={values.notifyNewCustomer}
                onChange={() => handleToggle('notifyNewCustomer')}
              />
              <label className="form-check-label" htmlFor="notifyNewCustomer">
                Notify staff when a new customer books
              </label>
            </div>
          </FormSection>

          <div className="d-flex justify-content-end">
            <Button type="submit" isLoading={isSubmitting}>
              {isSubmitting ? 'Saving...' : 'Save Changes'}
            </Button>
          </div>
        </form>
      </Card>
    </div>
  )
}
