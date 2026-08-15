import { BOOKING_FEATURES } from '../../config/features'
import { Card } from '../common/Card'
import { Badge } from '../common/Badge'
import { Reveal } from '../common/Reveal'
import { FeatureIllustration } from './FeatureIllustration'

const ILLUSTRATED_IDS = new Set(['online-booking', 'booking-calendar', 'dashboard-reports'])

export function FeaturesSection() {
  return (
    <section id="features" className="py-5 py-lg-6 border-bottom">
      <div className="container">
        <Reveal className="text-center mb-5">
          <p className="text-eyebrow mb-2">Everything runs through booking</p>
          <h2 className="fw-bold font-display">Everything you need to manage bookings</h2>
          <p className="text-secondary mb-0">The Booking module is the first available product on ApexBooking.</p>
        </Reveal>
        <div className="feature-grid">
          {BOOKING_FEATURES.map((feature, index) => (
            <Reveal
              key={feature.id}
              delayStep={index}
              className={feature.size === 'large' ? 'feature-grid__item feature-grid__item--large' : 'feature-grid__item'}
            >
              <Card hover className="h-100">
                <div className="d-flex align-items-center gap-2 mb-2">
                  <img src={feature.icon} alt="" width={24} height={24} />
                  <h3 className="h5 mb-0">{feature.title}</h3>
                  {feature.comingSoon && (
                    <Badge tone="neutral" className="ms-auto">
                      Coming Soon
                    </Badge>
                  )}
                </div>
                <p className="text-secondary mb-0">{feature.description}</p>
                {ILLUSTRATED_IDS.has(feature.id) && (
                  <FeatureIllustration variant={feature.id as 'online-booking' | 'booking-calendar' | 'dashboard-reports'} />
                )}
              </Card>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  )
}
