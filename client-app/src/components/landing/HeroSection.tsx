import { Button } from '../common/Button'
import { BrowserFrame } from '../common/BrowserFrame'
import { Reveal } from '../common/Reveal'
import { scrollToPricing } from '../../utils/scrollToPricing'
import heroScreenshot from '../../assets/hero-dashboard.png'

export function HeroSection() {
  return (
    <section id="home" className="hero-section py-5 py-lg-6 border-bottom">
      <div className="container">
        <div className="row align-items-center gy-5">
          <Reveal className="col-lg-6">
            <p className="text-eyebrow mb-3">Online booking for local businesses</p>
            <h1 className="display-5 fw-bold font-display mb-3">
              Booking software that stops the back-and-forth.
            </h1>
            <p className="lead text-secondary mb-4">
              Online booking, staff schedules, and today&apos;s appointments in one place — so customers book
              themselves in, and you stop chasing confirmations.
            </p>
            <div className="d-grid d-sm-flex gap-3">
              <Button to="/#pricing" size="lg" onClick={scrollToPricing}>
                Request Access
              </Button>
              <a href="#features" className="btn btn-outline-primary btn-lg">
                Explore Features
              </a>
            </div>
            <p className="hero-trust-line mt-3 mb-0">No setup fees. Live in minutes.</p>
          </Reveal>

          <Reveal className="col-lg-6" delayStep={1}>
            <div className="hero-frame-wrap">
              <BrowserFrame url="app.apexbooking.com/booking">
                <img
                  src={heroScreenshot}
                  alt="ApexBooking dashboard showing today's schedule"
                  className="w-100 d-block"
                />
              </BrowserFrame>
            </div>
          </Reveal>
        </div>
      </div>
    </section>
  )
}
