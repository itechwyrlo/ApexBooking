import React from 'react';

interface Props {
  service?: string;
  date?: string;
  time?: string;
  staffName?: string;
}

const Chip: React.FC<{ icon: string; label: string }> = ({ icon, label }) => (
  <span
    className="d-inline-flex align-items-center gap-1 rounded-pill px-3 py-1 small fw-medium"
    style={{
      background: 'var(--bs-primary-bg-subtle, #cfe2ff)',
      color: 'var(--bs-primary-text-emphasis, #052c65)',
      border: '1px solid var(--bs-primary-border-subtle, #9ec5fe)',
      fontSize: 13,
    }}
  >
    <i className={`fas ${icon}`} style={{ fontSize: 11 }} />
    {label}
  </span>
);

const WizardSelectionChips: React.FC<Props> = ({ service, date, time, staffName }) => {
  if (!service && !date && !time && !staffName) return null;

  return (
    <div className="d-flex flex-wrap gap-2 mb-3">
      {service && <Chip icon="fa-scissors" label={service} />}
      {date && <Chip icon="fa-calendar" label={date} />}
      {time && <Chip icon="fa-clock" label={time} />}
      {staffName && <Chip icon="fa-user" label={staffName} />}
    </div>
  );
};

export default WizardSelectionChips;
