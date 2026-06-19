import React from 'react';

interface Props {
  service?: string;
  date?: string;
  time?: string;
  staffName?: string;
}

const Chip: React.FC<{ icon: string; label: string }> = ({ icon, label }) => (
  <span className="wizard-chip d-inline-flex align-items-center gap-1 rounded-pill px-3 py-1 small fw-medium">
    <i className={`fas ${icon}`} />
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
