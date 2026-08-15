import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { TimeSelect } from '../common/TimeSelect'
import { Button } from '../common/Button'
import { useToast } from '../../hooks/useToast'
import { requestTimeOff } from '../../services/timeOffService'
import { TimeOffType } from '../../types/TimeOffType'
import { isRequired } from '../../utils/validators'
import type { IRequestTimeOffValues } from '../../interfaces/ITimeOffRequest'

const EMPTY_VALUES: IRequestTimeOffValues = {
  type: TimeOffType.FullDay,
  startDate: '',
  endDate: '',
  startTime: '',
  endTime: '',
  reason: '',
}

type TimeOffField = keyof IRequestTimeOffValues
type IFormErrors = Partial<Record<TimeOffField, string>>

function validate(values: IRequestTimeOffValues): IFormErrors {
  const errors: IFormErrors = {}

  if (!isRequired(values.startDate)) errors.startDate = 'Start date is required.'
  if (!isRequired(values.endDate)) errors.endDate = 'End date is required.'
  if (values.startDate && values.endDate && values.endDate < values.startDate) {
    errors.endDate = 'End date cannot be before the start date.'
  }

  if (values.type === TimeOffType.PartialDay) {
    if (!isRequired(values.startTime)) errors.startTime = 'Start time is required.'
    if (!isRequired(values.endTime)) errors.endTime = 'End time is required.'
    if (values.startTime && values.endTime && values.startTime >= values.endTime) {
      errors.endTime = 'End time must be after the start time.'
    }
  }

  return errors
}

interface IRequestTimeOffModalProps {
  isOpen: boolean
  onClose: () => void
  onRequested: () => void
}

export function RequestTimeOffModal({ isOpen, onClose, onRequested }: IRequestTimeOffModalProps) {
  const { showToast } = useToast()
  const [values, setValues] = useState<IRequestTimeOffValues>(EMPTY_VALUES)
  const [errors, setErrors] = useState<IFormErrors>({})
  const [touched, setTouched] = useState<Partial<Record<TimeOffField, boolean>>>({})
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (isOpen) {
      setValues(EMPTY_VALUES)
      setErrors({})
      setTouched({})
    }
  }, [isOpen])

  const handleFieldChange = <K extends TimeOffField>(field: K, value: IRequestTimeOffValues[K]) => {
    const nextValues = { ...values, [field]: value }
    setValues(nextValues)
    setErrors(validate(nextValues))
  }

  const handleBlur = (field: TimeOffField) => {
    setTouched((prev) => ({ ...prev, [field]: true }))
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validate(values)
    setErrors(validationErrors)
    setTouched({ startDate: true, endDate: true, startTime: true, endTime: true })

    if (Object.keys(validationErrors).length > 0) {
      return
    }

    setIsSubmitting(true)
    try {
      await requestTimeOff(values)
      showToast('success', 'Your time-off request was submitted for review.')
      onRequested()
      onClose()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to submit your time-off request. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal isOpen={isOpen} title="Request Time Off" onClose={onClose}>
      <form noValidate onSubmit={handleSubmit}>
        <FormGroup label="Type" htmlFor="timeOffTypeFullDay">
          <div className="d-flex gap-3">
            <div className="form-check">
              <input
                type="radio"
                className="form-check-input"
                id="timeOffTypeFullDay"
                name="timeOffType"
                checked={values.type === TimeOffType.FullDay}
                onChange={() => handleFieldChange('type', TimeOffType.FullDay)}
              />
              <label className="form-check-label" htmlFor="timeOffTypeFullDay">
                Full day
              </label>
            </div>
            <div className="form-check">
              <input
                type="radio"
                className="form-check-input"
                id="timeOffTypePartialDay"
                name="timeOffType"
                checked={values.type === TimeOffType.PartialDay}
                onChange={() => handleFieldChange('type', TimeOffType.PartialDay)}
              />
              <label className="form-check-label" htmlFor="timeOffTypePartialDay">
                Partial day
              </label>
            </div>
          </div>
        </FormGroup>

        <div className="row">
          <div className="col-sm-6">
            <FormGroup label="Start Date" htmlFor="startDate" required error={touched.startDate ? errors.startDate : undefined}>
              <input
                type="date"
                id="startDate"
                className={`form-control ${touched.startDate && errors.startDate ? 'is-invalid' : ''}`}
                value={values.startDate}
                onChange={(e) => handleFieldChange('startDate', e.target.value)}
                onBlur={() => handleBlur('startDate')}
              />
            </FormGroup>
          </div>
          <div className="col-sm-6">
            <FormGroup label="End Date" htmlFor="endDate" required error={touched.endDate ? errors.endDate : undefined}>
              <input
                type="date"
                id="endDate"
                className={`form-control ${touched.endDate && errors.endDate ? 'is-invalid' : ''}`}
                value={values.endDate}
                onChange={(e) => handleFieldChange('endDate', e.target.value)}
                onBlur={() => handleBlur('endDate')}
              />
            </FormGroup>
          </div>
        </div>

        {values.type === TimeOffType.PartialDay && (
          <div className="row">
            <div className="col-sm-6">
              <FormGroup label="Start Time" htmlFor="startTime" required error={touched.startTime ? errors.startTime : undefined}>
                <TimeSelect
                  id="startTime"
                  isInvalid={touched.startTime && !!errors.startTime}
                  value={values.startTime}
                  onChange={(value) => handleFieldChange('startTime', value)}
                />
              </FormGroup>
            </div>
            <div className="col-sm-6">
              <FormGroup label="End Time" htmlFor="endTime" required error={touched.endTime ? errors.endTime : undefined}>
                <TimeSelect
                  id="endTime"
                  isInvalid={touched.endTime && !!errors.endTime}
                  value={values.endTime}
                  onChange={(value) => handleFieldChange('endTime', value)}
                />
              </FormGroup>
            </div>
          </div>
        )}

        <FormGroup label="Reason" htmlFor="reason">
          <textarea
            id="reason"
            rows={3}
            className="form-control"
            value={values.reason}
            onChange={(e) => handleFieldChange('reason', e.target.value)}
          />
        </FormGroup>

        <div className="modal-form-actions">
          <Button type="button" variant="outline-secondary" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" isLoading={isSubmitting}>
            {isSubmitting ? 'Submitting...' : 'Submit Request'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
