import React, { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faChevronLeft, faChevronRight } from '@fortawesome/free-solid-svg-icons';
import type { Booking, BookingStatus } from '../types';
import './BookingEventCalendar.styles.css';

interface BookingEventCalendarProps {
  bookings: Booking[];
  isLoading: boolean;
  onMonthChange: (year: number, month: number) => void;
  onEventClick: (booking: Booking) => void;
  onDateSelect: (date: string | null) => void;
}

const DAY_NAMES = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];

const MAX_VISIBLE_EVENTS = 3;

function toDateStr(year: number, month: number, day: number): string {
  return `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
}

function eventClass(status: BookingStatus): string {
  switch (status) {
    case 'Confirmed':      return 'apex-event-cal-event--confirmed';
    case 'Pending':        return 'apex-event-cal-event--pending';
    case 'PendingPayment': return 'apex-event-cal-event--pending';
    case 'Completed':      return 'apex-event-cal-event--completed';
    case 'Cancelled':      return 'apex-event-cal-event--cancelled';
    case 'NoShow':         return 'apex-event-cal-event--noshow';
  }
}

const BookingEventCalendar: React.FC<BookingEventCalendarProps> = ({
  bookings,
  isLoading,
  onMonthChange,
  onEventClick,
  onDateSelect,
}) => {
  const today = new Date();
  const todayStr = toDateStr(today.getFullYear(), today.getMonth(), today.getDate());

  const [viewYear, setViewYear] = useState(today.getFullYear());
  const [viewMonth, setViewMonth] = useState(today.getMonth());
  const [selectedDate, setSelectedDate] = useState<string | null>(null);

  const firstDayOfMonth = new Date(viewYear, viewMonth, 1).getDay();
  const daysInMonth = new Date(viewYear, viewMonth + 1, 0).getDate();
  const prevDaysInMonth = new Date(viewYear, viewMonth, 0).getDate();

  const navigateTo = (y: number, m: number) => {
    setViewYear(y);
    setViewMonth(m);
    onMonthChange(y, m + 1);
  };

  const goToPrev = () => {
    const m = viewMonth === 0 ? 11 : viewMonth - 1;
    const y = viewMonth === 0 ? viewYear - 1 : viewYear;
    navigateTo(y, m);
  };

  const goToNext = () => {
    const m = viewMonth === 11 ? 0 : viewMonth + 1;
    const y = viewMonth === 11 ? viewYear + 1 : viewYear;
    navigateTo(y, m);
  };

  const goToToday = () => {
    const ty = today.getFullYear();
    const tm = today.getMonth();
    if (ty !== viewYear || tm !== viewMonth) {
      navigateTo(ty, tm);
    }
    selectDate(todayStr);
  };

  const selectDate = (dateStr: string) => {
    setSelectedDate(dateStr);
    onDateSelect(dateStr);
  };

  const bookingsByDate = bookings.reduce<Record<string, Booking[]>>((acc, b) => {
    const key = b.scheduledDate;
    if (!acc[key]) acc[key] = [];
    acc[key].push(b);
    return acc;
  }, {});

  const leadingCells = Array.from({ length: firstDayOfMonth }, (_, i) => ({
    day: prevDaysInMonth - firstDayOfMonth + i + 1,
    otherMonth: true,
    dateStr: '',
  }));

  const currentCells = Array.from({ length: daysInMonth }, (_, i) => ({
    day: i + 1,
    otherMonth: false,
    dateStr: toDateStr(viewYear, viewMonth, i + 1),
  }));

  const allCells = [...leadingCells, ...currentCells];
  const trailingCount = (7 - (allCells.length % 7)) % 7;
  const trailingCells = Array.from({ length: trailingCount }, (_, i) => ({
    day: i + 1,
    otherMonth: true,
    dateStr: '',
  }));

  const cells = [...allCells, ...trailingCells];

  return (
    <div className="apex-event-cal">
      <div className="apex-event-cal-header">
        <span className="apex-event-cal-title">
          Bookings Calendar
          {isLoading && <span className="spinner-border spinner-border-sm ms-2" role="status" />}
        </span>

        <div className="apex-event-cal-nav">
          <button
            type="button"
            className="btn btn-sm btn-light px-2"
            onClick={goToPrev}
            aria-label="Previous month"
          >
            <FontAwesomeIcon icon={faChevronLeft} />
          </button>
          <span className="apex-event-cal-month-label">
            {MONTH_NAMES[viewMonth]} {viewYear}
          </span>
          <button
            type="button"
            className="btn btn-sm btn-light px-2"
            onClick={goToNext}
            aria-label="Next month"
          >
            <FontAwesomeIcon icon={faChevronRight} />
          </button>
          <button
            type="button"
            className="btn btn-sm btn-outline-secondary ms-1"
            onClick={goToToday}
          >
            Today
          </button>
        </div>
      </div>

      <div className="apex-event-cal-grid">
        {DAY_NAMES.map(d => (
          <div key={d} className="apex-event-cal-day-name">{d}</div>
        ))}

        {cells.map((cell, idx) => {
          if (cell.otherMonth) {
            return (
              <div key={idx} className="apex-event-cal-cell apex-event-cal-cell--other">
                <span className="apex-event-cal-date">{cell.day}</span>
              </div>
            );
          }

          const dayBookings = (bookingsByDate[cell.dateStr] ?? [])
            .slice()
            .sort((a, b) => a.scheduledStartTime.localeCompare(b.scheduledStartTime));
          const visible = dayBookings.slice(0, MAX_VISIBLE_EVENTS);
          const overflow = dayBookings.length - visible.length;
          const isToday = cell.dateStr === todayStr;
          const isSelected = cell.dateStr === selectedDate;

          const cellClass = [
            'apex-event-cal-cell',
            isToday ? 'apex-event-cal-cell--today' : '',
            isSelected ? 'apex-event-cal-cell--selected' : '',
            dayBookings.length > 0 ? 'apex-event-cal-has-events' : '',
          ].filter(Boolean).join(' ');

          return (
            <div
              key={idx}
              className={cellClass}
              onClick={() => selectDate(cell.dateStr)}
            >
              <span className={`apex-event-cal-date${isToday ? ' apex-event-cal-date--today' : ''}`}>
                {cell.day}
              </span>

              {visible.map(b => (
                <div
                  key={b.bookingId}
                  className={`apex-event-cal-event ${eventClass(b.status)}`}
                  onClick={e => { e.stopPropagation(); onEventClick(b); }}
                  title={`${b.guest?.firstName ?? ''} ${b.guest?.lastName ?? ''} — ${b.serviceName}`}
                >
                  {b.scheduledStartTime.slice(0, 5)} {b.guest?.firstName ?? 'Client'}
                </div>
              ))}

              {overflow > 0 && (
                <div className="apex-event-cal-more">+{overflow} more</div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};

export default BookingEventCalendar;
