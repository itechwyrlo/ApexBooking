import React from 'react';
import type { ScheduleItemDto } from '../types';
import { StatusPill } from '../../../components/ui/StatusPill';

interface TodayScheduleTableProps {
  items: ScheduleItemDto[];
}

export const TodayScheduleTable: React.FC<TodayScheduleTableProps> = ({ items }) => (
  <div className="card border shadow-sm rounded p-3">
    <div className="fw-bold mb-3" style={{ fontSize: 13, color: '#111827' }}>Today's Schedule</div>
    <div style={{ overflowX: 'auto' }}>
      <table className="apex-table">
        <thead>
          <tr>
            <th>Guest</th>
            <th>Service</th>
            <th>Staff</th>
            <th>Start</th>
            <th>End</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {items.length === 0 ? (
            <tr>
              <td colSpan={6} className="text-center text-muted py-4" style={{ fontSize: 13 }}>
                No bookings scheduled for today.
              </td>
            </tr>
          ) : (
            items.map((item, i) => (
              <tr key={i}>
                <td>{item.guestName}</td>
                <td>{item.serviceName}</td>
                <td>{item.staffName}</td>
                <td className="text-muted">{item.scheduledStartTime}</td>
                <td className="text-muted">{item.scheduledEndTime}</td>
                <td><StatusPill status={item.status} /></td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  </div>
);
