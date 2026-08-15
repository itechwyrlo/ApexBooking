import { Button } from '../common/Button'
import { Reveal } from '../common/Reveal'
import { scrollToPricing } from '../../utils/scrollToPricing'

export function CallToActionSection() {
  return (
    <section className="py-5 py-lg-6 bg-gradient-brand text-white text-center">
      <div className="container">
        <Reveal>
          <h2 className="fw-bold font-display mb-3">Ready to bring your bookings online?</h2>
          <p className="mb-4 opacity-75">
            Join local businesses already using ApexBooking to fill their schedule and delight customers.
          </p>
          <div className="d-flex flex-wrap justify-content-center gap-3">
            <Button to="/#pricing" variant="light" size="lg" onClick={scrollToPricing}>
              Request Access
            </Button>
          </div>
        </Reveal>
      </div>
    </section>
  )
}
