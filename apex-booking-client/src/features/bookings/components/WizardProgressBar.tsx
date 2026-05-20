import React from 'react';

const STEP_LABELS: Record<number, string> = {
  1: 'Service',
  2: 'Date & Time',
  3: 'Staff',
  4: 'Details',
  5: 'Confirm',
};

interface Props {
  currentStep: number;
}

const WizardProgressBar: React.FC<Props> = ({ currentStep }) => {
  return (
    <div className="d-flex align-items-center justify-content-center mb-4">
      {[1, 2, 3, 4, 5].map((step) => {
        const isCompleted = currentStep > step;
        const isActive = currentStep === step;

        return (
          <React.Fragment key={step}>
            <div className="wizard-step-wrapper d-flex flex-column align-items-center">
              <div
                className={`wizard-step-circle rounded-circle d-flex align-items-center justify-content-center fw-semibold mb-1${isCompleted ? ' wizard-step-circle--done' : isActive ? ' wizard-step-circle--active' : ''}`}
              >
                {isCompleted
                  ? <i className="fas fa-check wizard-step-check" />
                  : step}
              </div>
              <span
                className={`wizard-step-label text-center${isActive ? ' wizard-step-label--active' : isCompleted ? ' wizard-step-label--done' : ''}`}
              >
                {STEP_LABELS[step]}
              </span>
            </div>
            {step < 5 && (
              <div
                className={`wizard-step-connector${currentStep > step ? ' wizard-step-connector--done' : ''}`}
              />
            )}
          </React.Fragment>
        );
      })}
    </div>
  );
};

export default WizardProgressBar;
