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
            <div className="d-flex flex-column align-items-center" style={{ minWidth: 64 }}>
              <div
                className="rounded-circle d-flex align-items-center justify-content-center fw-semibold mb-1"
                style={{
                  width: 32,
                  height: 32,
                  fontSize: 13,
                  background: isCompleted || isActive ? 'var(--bs-primary)' : '#e9ecef',
                  color: isCompleted || isActive ? '#fff' : '#adb5bd',
                  flexShrink: 0,
                }}
              >
                {isCompleted
                  ? <i className="fas fa-check" style={{ fontSize: 11 }} />
                  : step}
              </div>
              <span
                className="text-center"
                style={{
                  fontSize: 11,
                  color: isActive ? 'var(--bs-primary)' : isCompleted ? '#495057' : '#adb5bd',
                  fontWeight: isActive ? 600 : 400,
                  lineHeight: 1.2,
                }}
              >
                {STEP_LABELS[step]}
              </span>
            </div>
            {step < 5 && (
              <div
                style={{
                  flex: 1,
                  height: 2,
                  marginBottom: 18,
                  background: currentStep > step ? 'var(--bs-primary)' : '#e9ecef',
                  maxWidth: 40,
                }}
              />
            )}
          </React.Fragment>
        );
      })}
    </div>
  );
};

export default WizardProgressBar;
