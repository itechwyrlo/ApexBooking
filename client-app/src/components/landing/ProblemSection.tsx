import { PROBLEMS } from '../../config/problems'
import { Card } from '../common/Card'
import { Icon } from '../common/Icon'
import { Reveal } from '../common/Reveal'

export function ProblemSection() {
  return (
    <section className="py-5 py-lg-6 border-bottom">
      <div className="container">
        <Reveal className="text-center mb-5">
          <p className="text-eyebrow mb-2">Sound familiar?</p>
          <h2 className="fw-bold font-display">What booking software should fix</h2>
        </Reveal>
        <div className="row row-cols-2 row-cols-lg-4 g-4">
          {PROBLEMS.map((problem, index) => (
            <div className="col" key={problem.id}>
              <Reveal delayStep={index}>
                <Card className="h-100 text-center">
                  <Icon name={problem.icon} size={32} className="mb-3" />
                  <p className="fw-semibold mb-1">{problem.title}</p>
                  <p className="text-secondary small mb-0">{problem.description}</p>
                </Card>
              </Reveal>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
