import { Icon } from '../common/Icon'

type StepStatus = 'complete' | 'active' | 'pending'

interface IStep {
  label: string
  status: StepStatus
}

const STEPS: IStep[] = [
  { label: 'Submitted', status: 'complete' },
  { label: 'Under Review', status: 'active' },
  { label: 'Approved', status: 'pending' },
]

export function StatusStepper() {
  return (
    <ol className="status-stepper list-unstyled" aria-label="Request status">
      {STEPS.map((step, index) => (
        <li className={`status-stepper__step status-stepper__step--${step.status}`} key={step.label}>
          <span className="status-stepper__dot">
            {step.status === 'complete' && <Icon name="check-circle-light" size={12} />}
          </span>
          <span className="status-stepper__label">{step.label}</span>
          {index < STEPS.length - 1 && <span className="status-stepper__connector" aria-hidden="true" />}
        </li>
      ))}
    </ol>
  )
}
