import type { ReactNode } from 'react'
import { formatDisplayDate } from '../../utils/formatDateTime'
import { formatMoney } from '../../utils/formatMoney'
import type { IPublicBranch } from '../../interfaces/publicBooking/IPublicBranch'
import type { IPublicService } from '../../interfaces/publicBooking/IPublicService'
import type { IPublicStaff } from '../../interfaces/publicBooking/IPublicStaff'
import type { IPublicSlot } from '../../interfaces/publicBooking/IPublicSlot'

interface IBookingSummaryPanelProps {
  branch: IPublicBranch | null
  service: IPublicService | null
  staffMember: IPublicStaff | null
  date: string
  slot: IPublicSlot | null
}

function SummaryRow({ label, value }: { label: string; value?: ReactNode }) {
  return (
    <div className="d-flex justify-content-between small mb-2">
      <span className="pb-muted">{label}</span>
      <span className={value ? 'fw-medium text-end' : 'pb-muted text-end'}>{value ?? 'Not selected yet'}</span>
    </div>
  )
}

// Ready to become the natural home for a future tenant banner image / brand
// color once personalization ships — same reserved-slot approach as the
// header banner and option-card image slots from Phase A.
export function BookingSummaryPanel({ branch, service, staffMember, date, slot }: IBookingSummaryPanelProps) {
  return (
    <div className="pb-ticket p-4">
      <div className="fw-semibold small text-uppercase pb-muted mb-3" style={{ letterSpacing: '0.04em' }}>
        Your Booking
      </div>

      <SummaryRow label="Service" value={service?.name} />
      {staffMember && <SummaryRow label="With" value={staffMember.fullName} />}
      {branch && <SummaryRow label="Location" value={branch.branchName} />}
      <SummaryRow label="Date" value={slot ? formatDisplayDate(date) : undefined} />
      <SummaryRow label="Time" value={slot?.timeString} />

      {service && (
        <div className="d-flex justify-content-between align-items-center pb-ticket-divider pt-3 mt-3">
          <span className="fw-semibold small">Total</span>
          <span className="pb-mono fw-semibold">{formatMoney(service.price, service.currencyCode)}</span>
        </div>
      )}
    </div>
  )
}
