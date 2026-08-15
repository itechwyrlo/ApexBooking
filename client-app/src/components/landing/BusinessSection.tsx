import { INDUSTRIES } from '../../config/industries'
import { Card } from '../common/Card'
import { Badge } from '../common/Badge'
import { Reveal } from '../common/Reveal'

export function BusinessSection() {
  return (
    <section id="businesses" className="py-5 py-lg-6 bg-light border-bottom">
      <div className="container">
        <Reveal className="text-center mb-5">
          <p className="text-eyebrow mb-2">Built for appointment-based businesses</p>
          <h2 className="fw-bold font-display">If your business runs on bookings, ApexBooking fits</h2>
          <p className="text-secondary mb-0">From first-time customers to regulars, every visit starts with a booking.</p>
        </Reveal>
        <div className="row g-4">
          {INDUSTRIES.map((industry, index) => (
            <div className="col-6 col-lg-3" key={industry.id}>
              <Reveal delayStep={index}>
                <Card hover className="h-100 text-center position-relative">
                  <Badge
                    tone={industry.status === 'live' ? 'success' : 'neutral'}
                    className="position-absolute top-0 end-0 mt-2 me-2"
                  >
                    {industry.status === 'live' ? 'Live' : 'Coming Soon'}
                  </Badge>
                  <img src={industry.icon} alt="" width={40} height={40} className="mb-3" />
                  <p className="fw-medium mb-0">{industry.name}</p>
                </Card>
              </Reveal>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
