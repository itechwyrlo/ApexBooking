import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { Button } from '../common/Button'
import { Skeleton } from '../common/Skeleton'
import { TimeSelect } from '../common/TimeSelect'
import { useToast } from '../../hooks/useToast'
import { useTeamMemberSchedule } from '../../hooks/useTeamMemberSchedule'
import { useStaffBreaks } from '../../hooks/useStaffBreaks'
import { updateTeamMember, updateTeamMemberSchedule, addStaffBreak, removeStaffBreak } from '../../services/teamService'
import { Role } from '../../types/Role'
import { DayOfWeek, DAYS_OF_WEEK_ORDER } from '../../types/DayOfWeek'
import { isRequired } from '../../utils/validators'
import { formatDisplayTime } from '../../utils/formatDateTime'
import type { IEditTeamMemberValues, ITeamMember } from '../../interfaces/ITeamMember'
import type { IDaySchedule } from '../../interfaces/IDaySchedule'

const DEFAULT_START_TIME = '09:00:00'
const DEFAULT_END_TIME = '17:00:00'
const DEFAULT_BREAK_START_TIME = '12:00'
const DEFAULT_BREAK_END_TIME = '13:00'

function defaultSchedule(): IDaySchedule[] {
  return DAYS_OF_WEEK_ORDER.map((dayOfWeek) => ({
    dayOfWeek,
    startTime: DEFAULT_START_TIME,
    endTime: DEFAULT_END_TIME,
    isOff: true,
  }))
}

function toTimeInputValue(wireTime: string): string {
  return wireTime.slice(0, 5)
}

function toWireTime(inputValue: string): string {
  return `${inputValue}:00`
}

function validateSchedule(schedule: IDaySchedule[]): string | null {
  const invalidDay = schedule.find((entry) => !entry.isOff && entry.startTime >= entry.endTime)
  return invalidDay ? `Start time must be earlier than end time for ${invalidDay.dayOfWeek}.` : null
}

type DetailsField = keyof IEditTeamMemberValues
type IDetailsErrors = Partial<Record<DetailsField, string>>

function toDetailsValues(member: ITeamMember): IEditTeamMemberValues {
  return {
    firstName: member.firstName,
    lastName: member.lastName,
    contactNumber: member.contactNumber,
    customJobTitle: member.customJobTitle ?? '',
    role: member.role,
  }
}

function validateDetails(values: IEditTeamMemberValues): IDetailsErrors {
  const errors: IDetailsErrors = {}

  if (!isRequired(values.firstName)) errors.firstName = 'First name is required.'
  if (!isRequired(values.lastName)) errors.lastName = 'Last name is required.'
  if (!isRequired(values.role)) errors.role = 'Please select a role.'

  return errors
}

interface IEditTeamMemberModalProps {
  isOpen: boolean
  member: ITeamMember | null
  onClose: () => void
  onSaved: () => void
}

export function EditTeamMemberModal({ isOpen, member, onClose, onSaved }: IEditTeamMemberModalProps) {
  const { showToast } = useToast()
  const [activeTab, setActiveTab] = useState<'details' | 'schedule' | 'breaks'>('details')

  const [detailsValues, setDetailsValues] = useState<IEditTeamMemberValues>(
    member ? toDetailsValues(member) : { firstName: '', lastName: '', contactNumber: '', customJobTitle: '', role: '' },
  )
  const [detailsErrors, setDetailsErrors] = useState<IDetailsErrors>({})
  const [detailsTouched, setDetailsTouched] = useState<Partial<Record<DetailsField, boolean>>>({})
  const [isSubmittingDetails, setIsSubmittingDetails] = useState(false)

  const { schedule: loadedSchedule, isLoading: isLoadingSchedule } = useTeamMemberSchedule(member?.id ?? null)
  const [schedule, setSchedule] = useState<IDaySchedule[]>(defaultSchedule())
  const [scheduleError, setScheduleError] = useState<string | null>(null)
  const [isSubmittingSchedule, setIsSubmittingSchedule] = useState(false)
  const [copySourceDay, setCopySourceDay] = useState<DayOfWeek>(DayOfWeek.Monday)

  const { breaks, isLoading: isLoadingBreaks, error: breaksLoadError, refetch: refetchBreaks } = useStaffBreaks(member?.id ?? null)
  const [isAddingBreak, setIsAddingBreak] = useState(false)
  const [breakName, setBreakName] = useState('')
  const [breakStart, setBreakStart] = useState(DEFAULT_BREAK_START_TIME)
  const [breakEnd, setBreakEnd] = useState(DEFAULT_BREAK_END_TIME)
  const [breakFormError, setBreakFormError] = useState<string | null>(null)
  const [isSubmittingBreak, setIsSubmittingBreak] = useState(false)
  const [removingBreakId, setRemovingBreakId] = useState<string | null>(null)

  useEffect(() => {
    if (isOpen && member) {
      setActiveTab('details')
      setDetailsValues(toDetailsValues(member))
      setDetailsErrors({})
      setDetailsTouched({})
      setScheduleError(null)
      setIsAddingBreak(false)
      setBreakName('')
      setBreakStart(DEFAULT_BREAK_START_TIME)
      setBreakEnd(DEFAULT_BREAK_END_TIME)
      setBreakFormError(null)
    }
  }, [isOpen, member])

  useEffect(() => {
    if (loadedSchedule.length > 0) {
      setSchedule(loadedSchedule)
    }
  }, [loadedSchedule])

  const handleDetailsFieldChange = <K extends DetailsField>(field: K, value: IEditTeamMemberValues[K]) => {
    const nextValues = { ...detailsValues, [field]: value }
    setDetailsValues(nextValues)
    setDetailsErrors(validateDetails(nextValues))
  }

  const handleDetailsBlur = (field: DetailsField) => {
    setDetailsTouched((prev) => ({ ...prev, [field]: true }))
  }

  const handleDetailsSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!member) return

    const validationErrors = validateDetails(detailsValues)
    setDetailsErrors(validationErrors)
    setDetailsTouched({ firstName: true, lastName: true, role: true })

    if (Object.keys(validationErrors).length > 0) return

    setIsSubmittingDetails(true)
    try {
      await updateTeamMember(member.id, detailsValues)
      showToast('success', `${detailsValues.firstName} ${detailsValues.lastName}'s profile was updated.`)
      onSaved()
      onClose()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to update this team member. Please try again.')
    } finally {
      setIsSubmittingDetails(false)
    }
  }

  const handleDayChange = (dayOfWeek: DayOfWeek, patch: Partial<IDaySchedule>) => {
    const nextSchedule = schedule.map((entry) => (entry.dayOfWeek === dayOfWeek ? { ...entry, ...patch } : entry))
    setSchedule(nextSchedule)
    setScheduleError(validateSchedule(nextSchedule))
  }

  const handleCopyToAllDays = (dayOfWeek: DayOfWeek) => {
    const source = schedule.find((entry) => entry.dayOfWeek === dayOfWeek)
    if (!source) return

    const nextSchedule = schedule.map((entry) => ({ ...entry, isOff: source.isOff, startTime: source.startTime, endTime: source.endTime }))
    setSchedule(nextSchedule)
    setScheduleError(validateSchedule(nextSchedule))
  }

  const handleScheduleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!member) return

    const validationError = validateSchedule(schedule)
    setScheduleError(validationError)
    if (validationError) return

    setIsSubmittingSchedule(true)
    try {
      await updateTeamMemberSchedule(member.id, schedule)
      showToast('success', `${member.fullName}'s working hours were updated.`)
      onSaved()
    } catch (submitError) {
      const detail = axios.isAxiosError(submitError)
        ? (submitError.response?.data as { detail?: string } | undefined)?.detail
        : undefined
      showToast('error', detail ?? 'Failed to update working hours. Please try again.')
    } finally {
      setIsSubmittingSchedule(false)
    }
  }

  const handleAddBreakSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!member) return

    if (!isRequired(breakName)) {
      setBreakFormError('Break name is required.')
      return
    }
    if (breakStart >= breakEnd) {
      setBreakFormError('Start time must be earlier than end time.')
      return
    }

    setBreakFormError(null)
    setIsSubmittingBreak(true)
    try {
      await addStaffBreak(member.id, { name: breakName, startTime: toWireTime(breakStart), endTime: toWireTime(breakEnd) })
      showToast('success', `${breakName} was added.`)
      setIsAddingBreak(false)
      setBreakName('')
      setBreakStart(DEFAULT_BREAK_START_TIME)
      setBreakEnd(DEFAULT_BREAK_END_TIME)
      refetchBreaks()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      setBreakFormError(detail ?? 'Failed to add break. Please try again.')
    } finally {
      setIsSubmittingBreak(false)
    }
  }

  const handleRemoveBreak = async (breakId: string, name: string) => {
    if (!member) return

    setRemovingBreakId(breakId)
    try {
      await removeStaffBreak(member.id, breakId)
      refetchBreaks()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? `Failed to remove ${name}. Please try again.`)
    } finally {
      setRemovingBreakId(null)
    }
  }

  return (
    <Modal isOpen={isOpen} title={`Edit Team Member${member ? ` — ${member.fullName}` : ''}`} onClose={onClose}>
      <ul className="nav nav-tabs mb-3">
        <li className="nav-item">
          <button
            type="button"
            className={`nav-link ${activeTab === 'details' ? 'active' : ''}`}
            onClick={() => setActiveTab('details')}
          >
            Details
          </button>
        </li>
        <li className="nav-item">
          <button
            type="button"
            className={`nav-link ${activeTab === 'schedule' ? 'active' : ''}`}
            onClick={() => setActiveTab('schedule')}
          >
            Working Schedule
          </button>
        </li>
        <li className="nav-item">
          <button
            type="button"
            className={`nav-link ${activeTab === 'breaks' ? 'active' : ''}`}
            onClick={() => setActiveTab('breaks')}
          >
            Breaks
          </button>
        </li>
      </ul>

      {activeTab === 'details' && (
        <form noValidate onSubmit={handleDetailsSubmit}>
          <div className="row">
            <div className="col-sm-6">
              <FormGroup
                label="First Name"
                htmlFor="editFirstName"
                required
                error={detailsTouched.firstName ? detailsErrors.firstName : undefined}
              >
                <input
                  type="text"
                  id="editFirstName"
                  className={`form-control ${detailsTouched.firstName && detailsErrors.firstName ? 'is-invalid' : ''}`}
                  value={detailsValues.firstName}
                  onChange={(e) => handleDetailsFieldChange('firstName', e.target.value)}
                  onBlur={() => handleDetailsBlur('firstName')}
                />
              </FormGroup>
            </div>
            <div className="col-sm-6">
              <FormGroup
                label="Last Name"
                htmlFor="editLastName"
                required
                error={detailsTouched.lastName ? detailsErrors.lastName : undefined}
              >
                <input
                  type="text"
                  id="editLastName"
                  className={`form-control ${detailsTouched.lastName && detailsErrors.lastName ? 'is-invalid' : ''}`}
                  value={detailsValues.lastName}
                  onChange={(e) => handleDetailsFieldChange('lastName', e.target.value)}
                  onBlur={() => handleDetailsBlur('lastName')}
                />
              </FormGroup>
            </div>
          </div>

          <FormGroup label="Email" htmlFor="editEmail">
            <input type="email" id="editEmail" className="form-control" value={member?.email ?? ''} disabled readOnly />
            <div className="form-text">Login email can't be changed here.</div>
          </FormGroup>

          <FormGroup label="Contact Number" htmlFor="editContactNumber">
            <input
              type="tel"
              id="editContactNumber"
              className="form-control"
              value={detailsValues.contactNumber}
              onChange={(e) => handleDetailsFieldChange('contactNumber', e.target.value)}
            />
          </FormGroup>

          <FormGroup label="Job Title" htmlFor="editCustomJobTitle">
            <input
              type="text"
              id="editCustomJobTitle"
              className="form-control"
              placeholder="e.g. Senior Stylist"
              value={detailsValues.customJobTitle}
              onChange={(e) => handleDetailsFieldChange('customJobTitle', e.target.value)}
            />
          </FormGroup>

          <FormGroup label="Role" htmlFor="editRole" required error={detailsTouched.role ? detailsErrors.role : undefined}>
            {member?.role === Role.Owner ? (
              <>
                <input type="text" id="editRole" className="form-control" value="Owner" disabled readOnly />
                <div className="form-text">Ownership can't be changed here.</div>
              </>
            ) : (
              <select
                id="editRole"
                className={`form-select ${detailsTouched.role && detailsErrors.role ? 'is-invalid' : ''}`}
                value={detailsValues.role}
                onChange={(e) => handleDetailsFieldChange('role', e.target.value as IEditTeamMemberValues['role'])}
                onBlur={() => handleDetailsBlur('role')}
              >
                <option value="">Select a role</option>
                <option value={Role.Admin}>Admin</option>
                <option value={Role.Staff}>Staff</option>
              </select>
            )}
          </FormGroup>

          <div className="modal-form-actions">
            <Button type="button" variant="outline-secondary" onClick={onClose} disabled={isSubmittingDetails}>
              Cancel
            </Button>
            <Button type="submit" isLoading={isSubmittingDetails}>
              {isSubmittingDetails ? 'Saving...' : 'Save Changes'}
            </Button>
          </div>
        </form>
      )}

      {activeTab === 'schedule' &&
        (isLoadingSchedule ? (
          <div>
            <Skeleton height="1.5rem" className="mb-2" />
            <Skeleton height="1.5rem" className="mb-2" />
            <Skeleton height="1.5rem" />
          </div>
        ) : (
          <form noValidate onSubmit={handleScheduleSubmit}>
            {scheduleError && <div className="alert alert-danger py-2">{scheduleError}</div>}

            <div className="d-flex flex-wrap align-items-center gap-2 p-2 mb-3 rounded-3" style={{ backgroundColor: 'var(--color-canvas)' }}>
              <span className="small fw-semibold text-nowrap me-1">Copy hours from</span>
              <div className="btn-group" role="group" aria-label="Day to copy hours from">
                {DAYS_OF_WEEK_ORDER.map((day) => (
                  <button
                    key={day}
                    type="button"
                    className={`btn btn-sm ${copySourceDay === day ? 'btn-primary' : 'btn-outline-secondary'}`}
                    aria-pressed={copySourceDay === day}
                    onClick={() => setCopySourceDay(day)}
                  >
                    {day.slice(0, 3)}
                  </button>
                ))}
              </div>
              <Button type="button" variant="outline-primary" size="sm" onClick={() => handleCopyToAllDays(copySourceDay)}>
                Apply to All Days
              </Button>
            </div>

            {schedule.map((entry) => (
              <div key={entry.dayOfWeek} className="d-flex flex-column gap-2 py-2 border-bottom">
                <div className="d-flex align-items-center gap-3">
                  <div style={{ width: '5.5rem' }} className="fw-semibold small">
                    {entry.dayOfWeek}
                  </div>
                  <div className="form-check form-switch mb-0">
                    <input
                      type="checkbox"
                      className="form-check-input"
                      id={`edit-working-${entry.dayOfWeek}`}
                      checked={!entry.isOff}
                      onChange={(e) => handleDayChange(entry.dayOfWeek, { isOff: !e.target.checked })}
                    />
                    <label className="form-check-label small" htmlFor={`edit-working-${entry.dayOfWeek}`}>
                      Working
                    </label>
                  </div>
                </div>
                {!entry.isOff && (
                  <div className="d-flex flex-wrap align-items-center gap-2">
                    <TimeSelect
                      id={`edit-start-${entry.dayOfWeek}`}
                      className="form-select-sm"
                      style={{ minWidth: '8rem', flex: '1 1 8rem' }}
                      value={toTimeInputValue(entry.startTime)}
                      onChange={(value) => handleDayChange(entry.dayOfWeek, { startTime: toWireTime(value) })}
                    />
                    <span className="text-muted small">to</span>
                    <TimeSelect
                      id={`edit-end-${entry.dayOfWeek}`}
                      className="form-select-sm"
                      style={{ minWidth: '8rem', flex: '1 1 8rem' }}
                      value={toTimeInputValue(entry.endTime)}
                      onChange={(value) => handleDayChange(entry.dayOfWeek, { endTime: toWireTime(value) })}
                    />
                  </div>
                )}
              </div>
            ))}

            <div className="modal-form-actions">
              <Button type="button" variant="outline-secondary" onClick={onClose} disabled={isSubmittingSchedule}>
                Close
              </Button>
              <Button type="submit" isLoading={isSubmittingSchedule}>
                {isSubmittingSchedule ? 'Saving...' : 'Save Hours'}
              </Button>
            </div>
          </form>
        ))}

      {activeTab === 'breaks' &&
        (isLoadingBreaks ? (
          <div>
            <Skeleton height="1.5rem" className="mb-2" />
            <Skeleton height="1.5rem" className="mb-2" />
            <Skeleton height="1.5rem" />
          </div>
        ) : (
          <div>
            {breaksLoadError && <div className="alert alert-danger py-2">{breaksLoadError}</div>}

            {breaks.length === 0 && !isAddingBreak && (
              <p className="text-muted small mb-3">
                No breaks configured yet. Breaks (e.g. lunch) block booking slots during that window every day.
              </p>
            )}

            {breaks.map((entry) => (
              <div key={entry.id} className="d-flex align-items-center justify-content-between gap-2 py-2 border-bottom">
                <div>
                  <div className="fw-semibold small">{entry.name}</div>
                  <div className="text-muted small">
                    {formatDisplayTime(entry.startTime)} – {formatDisplayTime(entry.endTime)}
                  </div>
                </div>
                <Button
                  type="button"
                  variant="outline-danger"
                  size="sm"
                  icon="trash"
                  iconOnly
                  aria-label={`Remove ${entry.name}`}
                  isLoading={removingBreakId === entry.id}
                  onClick={() => handleRemoveBreak(entry.id, entry.name)}
                >
                  Remove
                </Button>
              </div>
            ))}

            {isAddingBreak ? (
              <form noValidate onSubmit={handleAddBreakSubmit} className={breaks.length > 0 ? 'pt-3' : ''}>
                {breakFormError && <div className="alert alert-danger py-2">{breakFormError}</div>}

                <FormGroup label="Break Name" htmlFor="newBreakName" required>
                  <input
                    type="text"
                    id="newBreakName"
                    className="form-control"
                    placeholder="e.g. Lunch Break"
                    value={breakName}
                    onChange={(e) => setBreakName(e.target.value)}
                    autoComplete="off"
                  />
                </FormGroup>

                <div className="d-flex flex-wrap align-items-center gap-2 mb-3">
                  <TimeSelect
                    id="newBreakStart"
                    className="form-select-sm"
                    style={{ minWidth: '8rem', flex: '1 1 8rem' }}
                    value={breakStart}
                    onChange={setBreakStart}
                  />
                  <span className="text-muted small">to</span>
                  <TimeSelect
                    id="newBreakEnd"
                    className="form-select-sm"
                    style={{ minWidth: '8rem', flex: '1 1 8rem' }}
                    value={breakEnd}
                    onChange={setBreakEnd}
                  />
                </div>

                <div className="d-flex gap-2">
                  <Button
                    type="button"
                    variant="outline-secondary"
                    size="sm"
                    onClick={() => {
                      setIsAddingBreak(false)
                      setBreakFormError(null)
                    }}
                    disabled={isSubmittingBreak}
                  >
                    Cancel
                  </Button>
                  <Button type="submit" size="sm" isLoading={isSubmittingBreak}>
                    {isSubmittingBreak ? 'Adding...' : 'Add Break'}
                  </Button>
                </div>
              </form>
            ) : (
              <Button
                type="button"
                variant="outline-primary"
                size="sm"
                icon="plus"
                className={breaks.length > 0 ? 'mt-3' : ''}
                onClick={() => setIsAddingBreak(true)}
              >
                Add Break
              </Button>
            )}

            <div className="modal-form-actions">
              <Button type="button" variant="outline-secondary" onClick={onClose}>
                Close
              </Button>
            </div>
          </div>
        ))}
    </Modal>
  )
}
