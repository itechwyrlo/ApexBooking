import React, { useEffect, useRef } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import { Alert } from "../../../components/ui/Alert";
import { useVerifyEmail } from "../hooks/useVerifyEmail";

const VerifyEmailPageComponent: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get("token");
  const hasCalledApi = useRef(false);

  const { verify, data, isLoading, error } = useVerifyEmail();

  useEffect(() => {
    if (hasCalledApi.current || !token) return;
    hasCalledApi.current = true;
    verify(token);
  }, [token, verify]);

  useEffect(() => {
    if (!data?.url) return;
    const timer = setTimeout(() => navigate(data.url), 2000);
    return () => clearTimeout(timer);
  }, [data, navigate]);

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-12 col-sm-10 col-md-6 col-lg-5">
            <div className="card border-0 shadow-sm p-4 p-md-5 text-center">
              <h4 className="fw-bold mb-4">Email Verification</h4>

              {isLoading && (
                <div className="d-flex flex-column align-items-center gap-3 py-2">
                  <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Verifying...</span>
                  </div>
                  <p className="text-muted mb-0">Verifying your email...</p>
                </div>
              )}

              {!token && !isLoading && (
                <div className="alert alert-danger" role="alert">
                  No verification token found in URL.
                </div>
              )}

              {error && (
                <div>
                  <Alert variant="error" className="mb-3">{error}</Alert>
                  <button
                    type="button"
                    className="btn btn-primary w-100"
                    onClick={() => navigate("/login")}
                  >
                    Go to Login
                  </button>
                </div>
              )}

              {data?.url && !error && (
                <div className="alert alert-success" role="alert">
                  Email verified successfully! Redirecting...
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default VerifyEmailPageComponent;
