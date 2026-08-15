import { useEffect, useMemo, useState } from 'react'
import { BookingCalendar } from './BookingCalendar'
import type { IPublicSlot } from '../../interfaces/publicBooking/IPublicSlot'
import type { WizardDirection } from '../../hooks/usePublicBookingWizard'

interface IScheduleStepProps {
  date: string
  onDateChange: (date: string) => void
  slots: IPublicSlot[]
  isLoading: boolean
  error: string | null
  unavailableReason: string | null
  selectedSlotRawTime: string | null
  direction: WizardDirection
  onSelectSlot: (slot: IPublicSlot) => void
}

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10)
}

const PERIODS = [
  { id: 'morning', label: 'Morning' },
  { id: 'afternoon', label: 'Afternoon' },
  { id: 'evening', label: 'Evening' },
] as const

type Period = (typeof PERIODS)[number]['id']

function getSlotPeriod(slot: IPublicSlot): Period {
  const hour = Number(slot.rawTime.slice(0, 2))
  if (hour < 12) return 'morning'
  if (hour < 17) return 'afternoon'
  return 'evening'
}

function groupSlotsByPeriod(slots: IPublicSlot[]): Record<Period, IPublicSlot[]> {
  const buckets: Record<Period, IPublicSlot[]> = { morning: [], afternoon: [], evening: [] }
  for (const slot of slots) buckets[getSlotPeriod(slot)].push(slot)
  return buckets
}

export function ScheduleStep({
  date,
  onDateChange,
  slots,
  isLoading,
  error,
  unavailableReason,
  selectedSlotRawTime,
  direction,
  onSelectSlot,
}: IScheduleStepProps) {
  const groupedSlots = useMemo(() => groupSlotsByPeriod(slots), [slots])
  const [activePeriod, setActivePeriod] = useState<Period>('morning')

  useEffect(() => {
    const selectedSlot = slots.find((slot) => slot.rawTime === selectedSlotRawTime)
    if (selectedSlot) {
      setActivePeriod(getSlotPeriod(selectedSlot))
      return
    }
    const firstAvailable = PERIODS.find((period) => groupedSlots[period.id].length > 0)
    setActivePeriod(firstAvailable?.id ?? 'morning')
  }, [groupedSlots, slots, selectedSlotRawTime])

  const activeSlots = groupedSlots[activePeriod]

  return (
    <div className={`pb-step-enter-${direction}`}>
      <h1 className="pb-display fs-3 mb-1">Pick a date and time</h1>
      <p className="pb-muted mb-4">Available times update as you change the date.</p>

      <div className="row g-4">
        <div className="col-12 col-md-6">
          <BookingCalendar value={date} onChange={onDateChange} minDate={todayIsoDate()} />
        </div>

        <div className="col-12 col-md-6">
          <p className="form-label small fw-semibold pb-muted mb-2">Available Times</p>

          {error && <div className="pb-alert-danger mb-3">{error}</div>}

          <div aria-live="polite">
            {isLoading ? (
              <p className="pb-muted">Loading open times...</p>
            ) : slots.length === 0 ? (
              <p className="pb-muted">{unavailableReason ?? 'No open times on this date. Try another day.'}</p>
            ) : (
              <>
                <div className="pb-segmented mb-3" role="tablist" aria-label="Time of day">
                  {PERIODS.map((period) => (
                    <button
                      key={period.id}
                      type="button"
                      role="tab"
                      aria-selected={activePeriod === period.id}
                      className={`pb-segmented-btn ${activePeriod === period.id ? 'is-active' : ''}`.trim()}
                      onClick={() => setActivePeriod(period.id)}
                    >
                      {period.label}
                    </button>
                  ))}
                </div>

                {activeSlots.length === 0 ? (
                  <p className="pb-muted small">No {activePeriod} times available on this date.</p>
                ) : (
                  <div className="row g-2">
                    {activeSlots.map((slot) => (
                      <div key={slot.rawTime} className="col-4 col-md-6 col-lg-4">
                        <button
                          type="button"
                          className={`pb-slot w-100 ${slot.rawTime === selectedSlotRawTime ? 'is-selected' : ''}`.trim()}
                          onClick={() => onSelectSlot(slot)}
                        >
                          {slot.timeString}
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
