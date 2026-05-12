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

  // Pad to complete last row
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
        <span className="fw-semibold d-flex align-items-center" style={{ fontSize: 15 }}>
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

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(7, 1fr)',
          gap: 2,
          textAlign: 'center',
        }}
      >
        {DAY_NAMES.map(d => (
          <div key={d} className="text-muted" style={{ fontSize: 11, fontWeight: 600, paddingBottom: 4 }}>
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

          let cellStyle: React.CSSProperties = {
            width: 34,
            height: 34,
            borderRadius: '50%',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            margin: '0 auto',
            fontSize: 13,
            cursor: isPast ? 'default' : 'pointer',
          };

          if (isSelected) {
            cellStyle = { ...cellStyle, background: 'var(--bs-primary)', color: '#fff', fontWeight: 600 };
          } else if (isToday) {
            cellStyle = { ...cellStyle, background: 'var(--bs-primary-bg-subtle, #cfe2ff)', color: 'var(--bs-primary)', fontWeight: 600 };
          } else if (isPast) {
            cellStyle = { ...cellStyle, color: '#ced4da' };
          } else {
            cellStyle = { ...cellStyle, color: '#212529' };
          }

          return (
            <div key={idx}>
              <div
                style={cellStyle}
                onClick={() => !isPast && onDateSelect(dateStr)}
                onMouseEnter={e => {
                  if (!isPast && !isSelected) (e.currentTarget as HTMLDivElement).style.background = '#f8f9fa';
                }}
                onMouseLeave={e => {
                  if (!isPast && !isSelected && !isToday)
                    (e.currentTarget as HTMLDivElement).style.background = '';
                }}
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
