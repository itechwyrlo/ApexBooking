const STEPS = ['Plan', 'Details', 'Confirm'] as const

interface IProgressTrackerProps {
  currentStep: (typeof STEPS)[number]
}

export function ProgressTracker({ currentStep }: IProgressTrackerProps) {
  return (
    <div className="progress-tracker" aria-label="Signup progress">
      {STEPS.map((step, index) => (
        <span className="progress-tracker__step" key={step}>
          <span className={`progress-tracker__label ${step === currentStep ? 'progress-tracker__label--active' : ''}`}>
            {step}
          </span>
          {index < STEPS.length - 1 && (
            <span className="progress-tracker__separator" aria-hidden="true">
              &middot;
            </span>
          )}
        </span>
      ))}
    </div>
  )
}
