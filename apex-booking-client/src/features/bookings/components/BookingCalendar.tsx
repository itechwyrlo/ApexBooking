import React, { useState } from 'react';

interface BookingCalendarProps {
  selectedDate: string;
  onDateSelect: (date: string) => void;
  minDate: string;
  maxDate?: string;
  availableDays?: Set<string> | null;
  isLoadingAvailability?: boolean;
  onMonthChange?: (year: number, month: number) => void;
}

const DAY_NAMES = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'];
const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];

function toLocalDateString(year: number, month: number, day: number): string {
  return `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
}

const BookingCalendar: React.FC<BookingCalendarProps> = ({
  selectedDate,
  onDateSelect,
  minDate,
  maxDate,
  availableDays,
  isLoadingAvailability,
  onMonthChange
}) => {
  const today = new Date();
  const [viewYear, setViewYear] = useState(today.getFullYear());
  const [viewMonth, setViewMonth] = useState(today.getMonth());

  const todayStr = toLocalDateString(today.getFullYear(), today.getMonth(), today.getDate());

  const firstDayOfMonth = new Date(viewYear, viewMonth, 1).getDay();
  const daysInMonth = new Date(viewYear, viewMonth + 1, 0).getDate();

  const prevMonth = () => {
    let newYear = viewYear;
    let newMonth = viewMonth;
    if (viewMonth === 0) {
      newYear = viewYear - 1;
      newMonth = 11;
      setViewYear(newYear);
      setViewMonth(newMonth);
    } else {
      newMonth = viewMonth - 1;
      setViewMonth(newMonth);
    }
    onMonthChange?.(newYear, newMonth + 1);
  };

  const nextMonth = () => {
    let newYear = viewYear;
    let newMonth = viewMonth;
    if (viewMonth === 11) {
      newYear = viewYear + 1;
      newMonth = 0;
      setViewYear(newYear);
      setViewMonth(newMonth);
    } else {
      newMonth = viewMonth + 1;
      setViewMonth(newMonth);
    }
    onMonthChange?.(newYear, newMonth + 1);
  };

  const cells: (number | null)[] = [
    ...Array(firstDayOfMonth).fill(null),
    ...Array.from({ length: daysInMonth }, (_, i) => i + 1),
  ];

  while (cells.length % 7 !== 0) cells.push(null);

  return (
    <div>
      <div className="d-flex align-items-center justify-content-between mb-3">
        <button
          className="btn btn-sm btn-light px-2"
          onClick={prevMonth}
          type="button"
          aria-label="Previous month"
        >
          <i className="fas fa-chevron-left" />
        </button>
        <span className="apex-cal-month fw-semibold d-flex align-items-center">
          {MONTH_NAMES[viewMonth]} {viewYear}
          {isLoadingAvailability && <span className="spinner-border spinner-border-sm ms-2" role="status"></span>}
        </span>
        <button
          className="btn btn-sm btn-light px-2"
          onClick={nextMonth}
          type="button"
          aria-label="Next month"
        >
          <i className="fas fa-chevron-right" />
        </button>
      </div>

      <div className="apex-cal-grid">
        {DAY_NAMES.map(d => (
          <div key={d} className="apex-cal-day-name text-muted">
            {d}
          </div>
        ))}

        {cells.map((day, idx) => {
          if (day === null) return <div key={idx} />;

          const dateStr = toLocalDateString(viewYear, viewMonth, day);
          const isBeforeMin = dateStr < minDate;
          const isAfterMax = maxDate ? dateStr > maxDate : false;
          const isUnavailable = availableDays ? !availableDays.has(dateStr) : false;
          const isPast = isBeforeMin || isAfterMax || isUnavailable;
          const isToday = dateStr === todayStr;
          const isSelected = dateStr === selectedDate;

          const cellClass = [
            'apex-cal-day',
            isSelected ? 'apex-cal-day--selected' : '',
            !isSelected && isToday ? 'apex-cal-day--today' : '',
            isPast ? 'apex-cal-day--disabled' : '',
          ].filter(Boolean).join(' ');

          return (
            <div key={idx}>
              <div
                className={cellClass}
                onClick={() => !isPast && onDateSelect(dateStr)}
              >
                {day}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

export default BookingCalendar;
