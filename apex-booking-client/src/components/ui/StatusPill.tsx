import React from 'react';

interface StatusPillProps {
  status: string;
}

const PILL_CLASS: Record<string, string> = {
  Confirmed: 'confirmed',
  PendingPayment: 'pending',
  Pending: 'pending',
  Completed: 'completed',
  Cancelled: 'cancelled',
  NoShow: 'noshow',
};

const PILL_LABEL: Record<string, string> = {
  Confirmed: 'Confirmed',
  PendingPayment: 'Pending Payment',
  Pending: 'Pending',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
  NoShow: 'No Show',
};

export const StatusPill: React.FC<StatusPillProps> = ({ status }) => (
  <span className={`apex-pill ${PILL_CLASS[status] ?? 'noshow'}`}>
    {PILL_LABEL[status] ?? status}
  </span>
);
