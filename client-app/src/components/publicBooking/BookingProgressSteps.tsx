import { Fragment } from 'react'

interface IBookingProgressStepsProps {
  currentIndex: number // 0-based index into the visible step list
  total: number
  stepLabels: string[] // one label per visible step, same order/length as `total`
  onStepClick?: (index: number) => void
}

function StepCircle({
  index,
  label,
  isDone,
  isCurrent,
  onStepClick,
}: {
  index: number
  label: string
  isDone: boolean
  isCurrent: boolean
  onStepClick?: (index: number) => void
}) {
  const className = `pb-stepper-circle ${isDone ? 'is-done' : ''} ${isCurrent ? 'is-current' : ''}`.trim()
  const content = isDone ? '✓' : index + 1

  if (isDone && onStepClick) {
    return (
      <button type="button" className={className} onClick={() => onStepClick(index)} aria-label={`Back to ${label}`}>
        {content}
      </button>
    )
  }

  return (
    <span className={className} aria-hidden="true">
      {content}
    </span>
  )
}

export function BookingProgressSteps({ currentIndex, total, stepLabels, onStepClick }: IBookingProgressStepsProps) {
  const stepNumber = Math.min(currentIndex + 1, total)
  const currentLabel = stepLabels[currentIndex] ?? ''

  return (
    <div>
      <div className="d-none d-sm-block">
        <div className="pb-stepper" role="progressbar" aria-valuenow={stepNumber} aria-valuemin={1} aria-valuemax={total}>
          {stepLabels.map((label, index) => {
            const isDone = index < currentIndex
            const isCurrent = index === currentIndex
            const isLast = index === stepLabels.length - 1

            return (
              <Fragment key={label}>
                <div className="pb-stepper-item">
                  <StepCircle index={index} label={label} isDone={isDone} isCurrent={isCurrent} onStepClick={onStepClick} />
                  <span className={`pb-stepper-label ${isCurrent ? 'is-current' : ''}`}>{label}</span>
                </div>
                {!isLast && <span className={`pb-stepper-bar ${isDone ? 'is-done' : ''}`} aria-hidden="true" />}
              </Fragment>
            )
          })}
        </div>
      </div>

      <div className="d-sm-none">
        <p className="pb-steps-mobile-label mb-2">
          STEP {String(stepNumber).padStart(2, '0')} / {String(total).padStart(2, '0')} · {currentLabel}
        </p>
        <div
          className="pb-stepper"
          role="progressbar"
          aria-valuenow={stepNumber}
          aria-valuemin={1}
          aria-valuemax={total}
          aria-label={`Step ${stepNumber} of ${total}: ${currentLabel}`}
        >
          {stepLabels.map((label, index) => {
            const isDone = index < currentIndex
            const isCurrent = index === currentIndex
            const isLast = index === stepLabels.length - 1

            return (
              <div className="d-flex align-items-center flex-grow-1" key={label}>
                <StepCircle index={index} label={label} isDone={isDone} isCurrent={isCurrent} onStepClick={onStepClick} />
                {!isLast && <span className={`pb-stepper-bar ${isDone ? 'is-done' : ''}`} aria-hidden="true" />}
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}
