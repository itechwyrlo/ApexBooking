import React from "react";
import { Link, useSearchParams } from "react-router-dom";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faEnvelope, faInfoCircle, faRedo, faArrowLeft } from "@fortawesome/free-solid-svg-icons";

const VerifyEmailNoticePage: React.FC = () => {
  const [searchParams] = useSearchParams();

  const backToLoginPath = (() => {
    const returnTo = searchParams.get("returnTo");
    if (returnTo) {
      try {
        const decoded = decodeURIComponent(returnTo);
        const tenantMatch = decoded.match(/^\/book\/([^/]+)\//);
        if (tenantMatch) {
          return `/book/${tenantMatch[1]}/customer/login?returnTo=${encodeURIComponent(returnTo)}`;
        }
      } catch {
        return "/login";
      }
    }
    return "/login";
  })();

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-12 col-sm-10 col-md-6 col-lg-5">
            <div className="card border-0 shadow-sm p-4 p-md-5">
              <div className="card-body text-center">
                <div className="mb-4">
                  <div className="apex-auth-icon-circle rounded-circle bg-primary bg-opacity-10 d-flex align-items-center justify-content-center mx-auto">
                    <FontAwesomeIcon icon={faEnvelope} size="2x" className="text-primary" />
                  </div>
                </div>

                <h4 className="fw-bold mb-3">Check your inbox</h4>

                <p className="text-muted mb-4">
                  We've sent a verification email to your registered email address.
                  Please check your inbox and click the verification link to activate
                  your account.
                </p>

                <div className="alert alert-info small mb-4" role="alert">
                  <FontAwesomeIcon icon={faInfoCircle} className="me-2" />
                  <strong>Didn't receive the email?</strong> Check your spam folder or
                  wait a few minutes for delivery.
                </div>

                <div className="d-grid gap-2">
                  <button
                    type="button"
                    className="btn btn-outline-primary"
                    onClick={() => window.location.reload()}
                  >
                    <FontAwesomeIcon icon={faRedo} className="me-2" />
                    Resend Verification Email
                  </button>

                  <Link to={backToLoginPath} className="btn btn-outline-secondary">
                    <FontAwesomeIcon icon={faArrowLeft} className="me-2" />
                    Back to Sign In
                  </Link>
                </div>

                <p className="text-muted small mt-4 mb-0">
                  If you continue to have issues, please contact our support team.
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default VerifyEmailNoticePage;
