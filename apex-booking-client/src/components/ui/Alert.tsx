import React from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faInfoCircle, faCheckCircle, faExclamationTriangle, faExclamationCircle } from '@fortawesome/free-solid-svg-icons';

interface AlertProps {
  children: React.ReactNode;
  variant?: 'info' | 'success' | 'warning' | 'error';
  dismissible?: boolean;
  onDismiss?: () => void;
  className?: string;
}

export const Alert: React.FC<AlertProps> = ({
  children,
  variant = 'info',
  dismissible = false,
  onDismiss,
  className = '',
}) => {
  const variantClasses = {
    info: 'alert-info',
    success: 'alert-success',
    warning: 'alert-warning',
    error: 'alert-danger',
  };

  const iconClasses = {
    info: faInfoCircle,
    success: faCheckCircle,
    warning: faExclamationTriangle,
    error: faExclamationCircle,
  };

  const classes = `alert ${variantClasses[variant]} d-flex align-items-center ${className}`;

  return (
    <div className={classes} role="alert">
      <FontAwesomeIcon icon={iconClasses[variant]} className="me-2" />
      <div className="flex-grow-1">
        {children}
      </div>
      {dismissible && (
        <button
          type="button"
          onClick={onDismiss}
          className="btn-close ms-2"
          aria-label="Dismiss"
        >
          <span className="visually-hidden">Dismiss</span>
        </button>
      )}
    </div>
  );
};
