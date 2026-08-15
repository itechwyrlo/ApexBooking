import { useEffect, useState } from 'react'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { TimeSelect } from '../common/TimeSelect'
import { Button } from '../common/Button'
import { isRequired } from '../../utils/validators'
import { formatDisplayDate } from '../../utils/formatDateTime'

interface IBlockMyTimeModalProps {
  isOpen: boolean
  date: string
  isSubmitting: boolean
  onClose: () => void
  onSubmit: (startTime: string, endTime: string, reason: string) => void
}

export function BlockMyTimeModal({ isOpen, date, isSubmitting, onClose, onSubmit }: IBlockMyTimeModalProps) {
  const [startTime, setStartTime] = useState('')
  const [endTime, setEndTime] = useState('')
  const [reason, setReason] = useState('')
  const [touched, setTouched] = useState(false)

  useEffect(() => {
    if (isOpen) {
      setStartTime('')
      setEndTime('')
      setReason('')
      setTouched(false)
    }
  }, [isOpen])

  const startTimeError = touched && !isRequired(startTime) ? 'Start time is required.' : undefined
  const endTimeError = touched
    ? !isRequired(endTime)
      ? 'End time is required.'
      : startTime && endTime && startTime >= endTime
        ? 'End time must be after the start time.'
        : undefined
    : undefined

  const handleClose = () => {
    onClose()
  }

  const handleSubmit = () => {
    setTouched(true)
    if (!isRequired(startTime) || !isRequired(endTime) || startTime >= endTime) return
    onSubmit(startTime, endTime, reason.trim())
  }

  return (
    <Modal
      isOpen={isOpen}
      title="Block My Time"
      description={`Mark part of today, ${formatDisplayDate(date)}, as unavailable.`}
      onClose={handleClose}
      footer={
        <div className="d-flex justify-content-end gap-2">
          <Button variant="outline-secondary" onClick={handleClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} isLoading={isSubmitting}>
            Block Time
          </Button>
        </div>
      }
    >
      <div className="row">
        <div className="col-sm-6">
          <FormGroup label="Start Time" htmlFor="blockStartTime" required error={startTimeError}>
            <TimeSelect id="blockStartTime" isInvalid={!!startTimeError} value={startTime} onChange={setStartTime} disabled={isSubmitting} />
          </FormGroup>
        </div>
        <div className="col-sm-6">
          <FormGroup label="End Time" htmlFor="blockEndTime" required error={endTimeError}>
            <TimeSelect id="blockEndTime" isInvalid={!!endTimeError} value={endTime} onChange={setEndTime} disabled={isSubmitting} />
          </FormGroup>
        </div>
      </div>
      <FormGroup label="Reason" htmlFor="blockReason">
        <textarea
          id="blockReason"
          rows={2}
          className="form-control"
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          disabled={isSubmitting}
          placeholder="e.g. Lunch break"
        />
      </FormGroup>
    </Modal>
  )
}
