import React from 'react';
import { Link } from 'react-router-dom';
import { Button } from '../../../components/ui/Button';

const VerifyEmailNoticePage: React.FC = () => {
  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div className="card shadow-sm border-0 p-4" style={{ width: '100%', maxWidth: '500px' }}>
        <div className="card-body text-center">
          {/* Email Icon */}
          <div className="mb-4">
            <div className="rounded-circle bg-primary bg-opacity-10 d-flex align-items-center justify-content-center mx-auto" style={{ width: '80px', height: '80px' }}>
              <i className="fas fa-envelope fa-2x text-primary"></i>
            </div>
          </div>

          {/* Header */}
          <h2 className="fw-bold mb-3">Check your inbox</h2>
          
          {/* Message */}
          <p className="text-muted mb-4">
            We've sent a verification email to your registered email address. 
            Please check your inbox and click the verification link to activate your account.
          </p>

          {/* Additional Info */}
          <div className="alert alert-info small mb-4" role="alert">
            <i className="fas fa-info-circle me-2"></i>
            <strong>Didn't receive the email?</strong> Check your spam folder or 
            wait a few minutes for delivery.
          </div>

          {/* Action Buttons */}
          <div className="d-grid gap-2">
            <Button
              type="button"
              className="btn btn-outline-primary"
              onClick={() => window.location.reload()}
            >
              <i className="fas fa-redo me-2"></i>
              Resend Verification Email
            </Button>
            
            <Link to="/login" className="btn btn-outline-secondary">
              <i className="fas fa-arrow-left me-2"></i>
              Back to Sign In
            </Link>
          </div>

          {/* Help Text */}
          <p className="text-muted small mt-4 mb-0">
            If you continue to have issues, please contact our support team.
          </p>
        </div>
      </div>
    </div>
  );
};

export default VerifyEmailNoticePage;
