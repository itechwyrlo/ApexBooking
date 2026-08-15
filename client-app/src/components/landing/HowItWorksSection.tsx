import { HOW_IT_WORKS_STEPS } from '../../config/howItWorks'
import { Reveal } from '../common/Reveal'

export function HowItWorksSection() {
  return (
    <section className="py-5 py-lg-6 bg-light border-bottom">
      <div className="container">
        <Reveal className="text-center mb-5">
          <p className="text-eyebrow mb-2">From sign-up to first booking</p>
          <h2 className="fw-bold font-display">How it works</h2>
        </Reveal>
        <div className="row g-4">
          {HOW_IT_WORKS_STEPS.map((item, index) => (
            <div className="col-sm-6 col-lg-3" key={item.step}>
              <Reveal delayStep={index}>
                <div className="text-center text-sm-start">
                  <p className="step-number font-display mb-2">{String(item.step).padStart(2, '0')}</p>
                  <h3 className="h6">{item.title}</h3>
                  <p className="text-secondary mb-0">{item.description}</p>
                </div>
              </Reveal>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
