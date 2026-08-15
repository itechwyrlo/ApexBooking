import { Icon } from '../common/Icon'
import { SchedulePreviewCard } from './SchedulePreviewCard'
import { BrowserFrame } from '../common/BrowserFrame'
import { Reveal } from '../common/Reveal'

const DASHBOARD_HIGHLIGHTS = [
  "Today's bookings at a glance",
  'A shared calendar for every team member',
  'Upcoming appointments for the week ahead',
  'Team availability, always up to date',
  'Booking reports without spreadsheets',
]

export function DashboardPreviewSection() {
  return (
    <section className="py-5 py-lg-6 bg-light border-bottom">
      <div className="container">
        <div className="row align-items-center gy-5">
          <Reveal className="col-lg-6 order-2 order-lg-1">
            <p className="text-eyebrow mb-2">The booking dashboard</p>
            <h2 className="fw-bold font-display mb-3">See your whole day at a glance</h2>
            <p className="text-secondary mb-4">
              The Booking dashboard brings today&apos;s schedule, team availability, and booking activity together
              in one operational view — no digging through menus to find out what's happening.
            </p>
            <ul className="list-unstyled d-flex flex-column gap-3 mb-0">
              {DASHBOARD_HIGHLIGHTS.map((item) => (
                <li key={item} className="d-flex align-items-center gap-2">
                  <Icon name="check-circle" size={20} />
                  {item}
                </li>
              ))}
            </ul>
          </Reveal>
          <Reveal className="col-lg-6 order-1 order-lg-2" delayStep={1}>
            <BrowserFrame url="app.apexbooking.com/booking/calendar">
              <div className="p-3 p-lg-4">
                <SchedulePreviewCard className="border-0 shadow-none" />
              </div>
            </BrowserFrame>
          </Reveal>
        </div>
      </div>
    </section>
  )
}
