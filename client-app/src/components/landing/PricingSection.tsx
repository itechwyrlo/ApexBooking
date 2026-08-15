import { PRICING_PLANS } from '../../config/pricing'
import { Card } from '../common/Card'
import { Badge } from '../common/Badge'
import { Button } from '../common/Button'
import { Icon } from '../common/Icon'
import { Reveal } from '../common/Reveal'

export function PricingSection() {
  return (
    <section id="pricing" className="py-5 py-lg-6 border-bottom">
      <div className="container">
        <Reveal className="text-center mb-5">
          <p className="text-eyebrow mb-2">Plans for every stage</p>
          <h2 className="fw-bold font-display">Simple, transparent pricing</h2>
          <p className="text-secondary mb-0">Choose the plan that fits your business.</p>
        </Reveal>
        <div className="row g-4 justify-content-center">
          {PRICING_PLANS.map((plan, index) => (
            <div className="col-md-6 col-lg-5" key={plan.id}>
              <Reveal delayStep={index}>
                <Card
                  hover
                  className={`h-100 ${plan.recommended ? 'border border-2 border-primary' : ''}`}
                  bodyClassName="d-flex flex-column"
                >
                  {plan.recommended && (
                    <Badge tone="primary" className="align-self-start mb-2">
                      Recommended
                    </Badge>
                  )}
                  <h3 className="h4 mb-1">{plan.name}</h3>
                  <p className="text-secondary">{plan.description}</p>
                  <ul className="list-unstyled d-flex flex-column gap-2 mb-4">
                    {plan.features.map((feature) => (
                      <li key={feature} className="d-flex align-items-center gap-2">
                        <Icon name="check-circle" size={16} />
                        {feature}
                      </li>
                    ))}
                  </ul>

                  <Button
                    to={`/request-access?plan=${plan.id}`}
                    variant={plan.recommended ? 'primary' : 'outline-primary'}
                    className="mt-auto"
                  >
                    {plan.ctaLabel}
                  </Button>
                </Card>
              </Reveal>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
