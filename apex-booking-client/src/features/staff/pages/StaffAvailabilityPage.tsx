import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { faPlus, faTrash, faArrowLeft, faSave } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { Alert } from '../../../components/ui/Alert';
import { Button } from '../../../components/ui/Button';
import { ConfirmModal } from '../../../components/ui/modal/ConfirmModal';
import { useStaffAvailability } from '../hooks/useStaffAvailability';
import { StaffAvailabilityPageSkeleton } from '../components/StaffAvailabilityPageSkeleton';
import type {
  DaySchedule,
  BreakPeriod,
  CreateExceptionRequest,
  ExceptionType,
  AvailabilityException,
} from '../types';

const DAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

const buildDefaultSchedule = (): DaySchedule[] =>
  DAY_NAMES.map((_, i) => ({
    dayOfWeek: i,
    isAvailable: false,
    startTime: null,
    endTime: null,
    breaks: [],
  }));

const EMPTY_EXCEPTION: CreateExceptionRequest = {
  exceptionDate: '',
  exceptionType: 'UnavailableAllDay',
  startTime: undefined,
  endTime: undefined,
  note: '',
};

const EXCEPTION_TYPE_LABELS: Record<ExceptionType, string> = {
  UnavailableAllDay: 'Unavailable All Day',
  UnavailableHours: 'Unavailable During Hours',
  AvailableExtraHours: 'Available Extra Hours',
};

const StaffAvailabilityPage: React.FC = () => {
  const { tenant, id: staffId } = useParams<{ tenant: string; id: string }>();
  const navigate = useNavigate();

  const {
    schedule: savedSchedule,
    exceptions,
    isLoading,
    error,
    success,
    clearError,
    clearSuccess,
    getSchedule,
    getExceptions,
    setSchedule,
    addException,
    removeException,
  } = useStaffAvailability(staffId!);

  const [schedule, setScheduleState] = useState<DaySchedule[]>(buildDefaultSchedule());
  const [exceptionForm, setExceptionForm] = useState<CreateExceptionRequest>(EMPTY_EXCEPTION);
  const [showExceptionForm, setShowExceptionForm] = useState(false);
  const [confirmRemoveId, setConfirmRemoveId] = useState<string | null>(null);

  useEffect(() => {
    getSchedule();
    getExceptions();
  }, [getSchedule, getExceptions]);

  useEffect(() => {
    if (!savedSchedule || savedSchedule.length === 0) return;
    setScheduleState(savedSchedule);
  }, [savedSchedule]);

  const toggleDay = (index: number) => {
    setScheduleState(prev =>
      prev.map((d, i) =>
        i !== index ? d : {
          ...d,
          isAvailable: !d.isAvailable,
          startTime: !d.isAvailable ? '08:00' : null,
          endTime: !d.isAvailable ? '17:00' : null,
          breaks: [],
        }
      )
    );
  };

  const updateDayTime = (index: number, field: 'startTime' | 'endTime', value: string) => {
    setScheduleState(prev =>
      prev.map((d, i) => i !== index ? d : { ...d, [field]: value })
    );
  };

  const addBreak = (dayIndex: number) => {
    setScheduleState(prev =>
      prev.map((d, i) =>
        i !== dayIndex ? d : {
          ...d,
          breaks: [...d.breaks, { breakStartTime: '12:00', breakEndTime: '13:00' }],
        }
      )
    );
  };

  const updateBreak = (dayIndex: number, breakIndex: number, field: keyof BreakPeriod, value: string) => {
    setScheduleState(prev =>
      prev.map((d, i) =>
        i !== dayIndex ? d : {
          ...d,
          breaks: d.breaks.map((b, bi) => bi !== breakIndex ? b : { ...b, [field]: value }),
        }
      )
    );
  };

  const removeBreak = (dayIndex: number, breakIndex: number) => {
    setScheduleState(prev =>
      prev.map((d, i) =>
        i !== dayIndex ? d : {
          ...d,
          breaks: d.breaks.filter((_, bi) => bi !== breakIndex),
        }
      )
    );
  };

  const handleSaveSchedule = async () => {
    await setSchedule({ schedules: schedule });
  };

  const handleAddException = async () => {
    const ok = await addException(exceptionForm);
    if (ok) {
      setExceptionForm(EMPTY_EXCEPTION);
      setShowExceptionForm(false);
    }
  };

  const handleRemoveException = async () => {
    if (!confirmRemoveId) return;
    await removeException(confirmRemoveId);
    setConfirmRemoveId(null);
  };

  const needsTimeRange = (type: ExceptionType) =>
    type === 'UnavailableHours' || type === 'AvailableExtraHours';

  if (savedSchedule === null) return <StaffAvailabilityPageSkeleton showBackButton />;

  return (
    <div className="container-fluid px-3 px-md-4 py-4">
      <div className="row mb-4 align-items-center">
        <div className="col d-flex align-items-center gap-3">
          <button
            className="btn btn-sm btn-outline-secondary"
            onClick={() => navigate(`/t/${tenant}/staff`)}
          >
            <FontAwesomeIcon icon={faArrowLeft} className="me-1" />
            Back
          </button>
          <div>
            <h5 className="fw-bold mb-0">Staff Availability</h5>
            <small className="text-muted">Set weekly schedule and date-specific exceptions.</small>
          </div>
        </div>
      </div>

      {error && (
        <div className="alert alert-danger alert-dismissible d-flex align-items-center mb-3" role="alert">
          {error}
          <button type="button" className="btn-close ms-auto" onClick={clearError} aria-label="Dismiss" />
        </div>
      )}

      {success && (
        <Alert variant="success" dismissible onDismiss={clearSuccess} className="mb-3">
          {success}
        </Alert>
      )}

      <div className="card border-0 shadow-sm mb-4">
        <div className="card-header bg-white border-bottom py-3">
          <div className="fw-semibold">Weekly Schedule</div>
          <div className="text-muted small mt-1">
            Defines the recurring working hours for this staff member. This is a full replacement on save.
          </div>
        </div>
        <div className="card-body p-0">
          {schedule.map((day, dayIndex) => (
            <div
              key={day.dayOfWeek}
              className={`px-4 py-3 border-bottom ${!day.isAvailable ? 'bg-light' : ''}`}
            >
              <div className="apex-avail-row">
                <div className="apex-avail-day-col">
                  <div className="form-check mb-0">
                    <input
                      type="checkbox"
                      className="form-check-input"
                      id={`day-${dayIndex}`}
                      checked={day.isAvailable}
                      onChange={() => toggleDay(dayIndex)}
                    />
                    <label className="form-check-label fw-medium" htmlFor={`day-${dayIndex}`}>
                      {DAY_NAMES[day.dayOfWeek]}
                    </label>
                  </div>
                </div>

                {day.isAvailable ? (
                  <div className="apex-avail-row-controls">
                    <div className="d-flex align-items-center gap-2">
                      <input
                        type="time"
                        className="form-control form-control-sm apex-avail-time-input"
                        value={day.startTime ?? ''}
                        onChange={e => updateDayTime(dayIndex, 'startTime', e.target.value)}
                      />
                      <span className="text-muted small">to</span>
                      <input
                        type="time"
                        className="form-control form-control-sm apex-avail-time-input"
                        value={day.endTime ?? ''}
                        onChange={e => updateDayTime(dayIndex, 'endTime', e.target.value)}
                      />
                    </div>
                    <button
                      className="btn btn-sm btn-outline-secondary ms-auto"
                      onClick={() => addBreak(dayIndex)}
                    >
                      <FontAwesomeIcon icon={faPlus} className="me-1" />
                      Add Break
                    </button>
                  </div>
                ) : (
                  <span className="text-muted small">Unavailable</span>
                )}
              </div>

              {day.isAvailable && day.breaks.length > 0 && (
                <div className="mt-2 ps-4 d-flex flex-column gap-2">
                  {day.breaks.map((brk, breakIndex) => (
                    <div key={breakIndex} className="apex-avail-break-row">
                      <span className="text-muted small apex-avail-break-prefix">Break</span>
                      <input
                        type="time"
                        className="form-control form-control-sm apex-avail-time-input"
                        value={brk.breakStartTime}
                        onChange={e => updateBreak(dayIndex, breakIndex, 'breakStartTime', e.target.value)}
                      />
                      <span className="text-muted small">to</span>
                      <input
                        type="time"
                        className="form-control form-control-sm apex-avail-time-input"
                        value={brk.breakEndTime}
                        onChange={e => updateBreak(dayIndex, breakIndex, 'breakEndTime', e.target.value)}
                      />
                      <input
                        type="text"
                        className="form-control form-control-sm apex-avail-break-label"
                        placeholder="Label (optional)"
                        value={brk.label ?? ''}
                        onChange={e => updateBreak(dayIndex, breakIndex, 'label', e.target.value)}
                      />
                      <button
                        className="btn btn-sm btn-outline-danger"
                        onClick={() => removeBreak(dayIndex, breakIndex)}
                      >
                        <FontAwesomeIcon icon={faTrash} />
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>
          ))}
        </div>
        <div className="card-footer bg-white d-flex justify-content-end py-3">
          <Button variant="primary" icon={faSave} loading={isLoading} onClick={handleSaveSchedule}>
            Save Schedule
          </Button>
        </div>
      </div>

      <div className="card border-0 shadow-sm">
        <div className="card-header bg-white border-bottom py-3 d-flex justify-content-between align-items-center">
          <div>
            <div className="fw-semibold">Date Exceptions</div>
            <div className="text-muted small mt-1">
              Override the weekly schedule for specific dates.
            </div>
          </div>
          <Button
            variant="secondary"
            size="sm"
            icon={faPlus}
            onClick={() => setShowExceptionForm(v => !v)}
          >
            Add Exception
          </Button>
        </div>

        {showExceptionForm && (
          <div className="card-body border-bottom bg-light-subtle">
            <div className="row g-3 align-items-end">
              <div className="col-md-3">
                <label className="form-label small fw-medium mb-1">Date <span className="text-danger">*</span></label>
                <input
                  type="date"
                  className="form-control form-control-sm"
                  value={exceptionForm.exceptionDate}
                  onChange={e => setExceptionForm(f => ({ ...f, exceptionDate: e.target.value }))}
                />
              </div>
              <div className="col-md-3">
                <label className="form-label small fw-medium mb-1">Exception Type <span className="text-danger">*</span></label>
                <select
                  className="form-select form-select-sm"
                  value={exceptionForm.exceptionType}
                  onChange={e => setExceptionForm(f => ({
                    ...f,
                    exceptionType: e.target.value as ExceptionType,
                    startTime: undefined,
                    endTime: undefined,
                  }))}
                >
                  {(Object.keys(EXCEPTION_TYPE_LABELS) as ExceptionType[]).map(t => (
                    <option key={t} value={t}>{EXCEPTION_TYPE_LABELS[t]}</option>
                  ))}
                </select>
              </div>

              {needsTimeRange(exceptionForm.exceptionType) && (
                <>
                  <div className="col-md-2">
                    <label className="form-label small fw-medium mb-1">Start <span className="text-danger">*</span></label>
                    <input
                      type="time"
                      className="form-control form-control-sm"
                      value={exceptionForm.startTime ?? ''}
                      onChange={e => setExceptionForm(f => ({ ...f, startTime: e.target.value }))}
                    />
                  </div>
                  <div className="col-md-2">
                    <label className="form-label small fw-medium mb-1">End <span className="text-danger">*</span></label>
                    <input
                      type="time"
                      className="form-control form-control-sm"
                      value={exceptionForm.endTime ?? ''}
                      onChange={e => setExceptionForm(f => ({ ...f, endTime: e.target.value }))}
                    />
                  </div>
                </>
              )}

              <div className="col-md-3">
                <label className="form-label small fw-medium mb-1">Note</label>
                <input
                  type="text"
                  className="form-control form-control-sm"
                  placeholder="Optional note"
                  value={exceptionForm.note ?? ''}
                  onChange={e => setExceptionForm(f => ({ ...f, note: e.target.value }))}
                />
              </div>

              <div className="col-12 d-flex gap-2">
                <Button variant="primary" size="sm" loading={isLoading} onClick={handleAddException}>
                  Save Exception
                </Button>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => { setShowExceptionForm(false); setExceptionForm(EMPTY_EXCEPTION); }}
                >
                  Cancel
                </Button>
              </div>
            </div>
          </div>
        )}

        <div className="card-body p-0">
          {exceptions.length === 0 ? (
            <div className="p-4 text-center text-muted small">No exceptions defined.</div>
          ) : (
            <div className="table-responsive">
              <table className="table table-hover align-middle mb-0">
                <thead className="table-light">
                  <tr>
                    <th className="ps-4 fw-semibold small text-muted">Date</th>
                    <th className="fw-semibold small text-muted">Type</th>
                    <th className="fw-semibold small text-muted">Time Window</th>
                    <th className="fw-semibold small text-muted">Note</th>
                    <th className="fw-semibold small text-muted text-end pe-4">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {exceptions.map((ex: AvailabilityException) => (
                    <tr key={ex.id}>
                      <td className="ps-4 fw-medium">{ex.exceptionDate}</td>
                      <td className="text-muted small">{EXCEPTION_TYPE_LABELS[ex.exceptionType]}</td>
                      <td className="text-muted small">
                        {ex.exceptionType === 'UnavailableAllDay'
                          ? 'All day'
                          : `${ex.startTime} – ${ex.endTime}`}
                      </td>
                      <td className="text-muted small">{ex.note ?? '—'}</td>
                      <td className="text-end pe-4">
                        <button
                          className="btn btn-sm btn-outline-danger"
                          onClick={() => setConfirmRemoveId(ex.id)}
                        >
                          <FontAwesomeIcon icon={faTrash} />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      <ConfirmModal
        isOpen={!!confirmRemoveId}
        title="Remove Exception"
        message="Are you sure you want to remove this exception? The base weekly schedule will apply for this date."
        onConfirm={handleRemoveException}
        onCancel={() => setConfirmRemoveId(null)}
      />
    </div>
  );
};

export default StaffAvailabilityPage;
