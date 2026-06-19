import React from 'react';
import './CalendarSkeleton.styles.css';

const CalendarSkeleton: React.FC = () => (
  <div className="apex-event-cal">
    <div className="apex-event-cal-header">
      <div className="apex-skeleton cal-sk-nav" />
      <div className="apex-skeleton cal-sk-title" />
      <div className="apex-skeleton cal-sk-nav" />
    </div>

    <div className="apex-event-cal-grid">
      {['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map(d => (
        <div key={d} className="apex-event-cal-day-name">{d}</div>
      ))}

      {Array.from({ length: 35 }).map((_, i) => (
        <div key={i} className="apex-event-cal-cell">
          <div className="apex-skeleton cal-sk-date" />
          {i % 3 === 0 && <div className="apex-skeleton cal-sk-event mt-1" />}
          {i % 5 === 0 && <div className="apex-skeleton cal-sk-event mt-1" />}
        </div>
      ))}
    </div>
  </div>
);

export { CalendarSkeleton };
