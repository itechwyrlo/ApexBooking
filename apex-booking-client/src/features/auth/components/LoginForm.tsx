import React, { useState, useEffect } from "react";
import { Link, useSearchParams, useLocation } from "react-router-dom";
import { useLogin } from "../hooks/useLogin";
import { Button } from "../../../components/ui/Button";
import { Input } from "../../../components/ui/Input";
import { Alert } from "../../../components/ui/Alert";
import type { ValidationErrors } from "../../../utils/validation";
import { validateForm, validationRules } from "../../../utils/validation";
import { CustomerRegisterLink } from "./CustomerRegisterLink";
import "./LoginForm.styles.css";

interface LoginFormData {
  email: string;
  password: string;
}

const LoginForm: React.FC = () => {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [searchParams] = useSearchParams();
  const location = useLocation();
  const { login, isLoading, error, clearError } = useLogin();
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<ValidationErrors<LoginFormData>>({});

  const isCustomerPath = /^\/book\/[^/]+\/customer\/login/.test(location.pathname);

  useEffect(() => {
    const message = searchParams.get("message");
    const verified = searchParams.get("verified");
    const tenant = searchParams.get("tenant");
    const token = searchParams.get("token");
    const emailParam = searchParams.get("email");

    if (message === "email-verified" || verified === "email") {
      setSuccessMessage("Email verified successfully! You can now log in.");
      if (tenant) sessionStorage.setItem("redirectTenant", tenant);
      if (token) sessionStorage.setItem("verificationToken", token);
      if (emailParam) sessionStorage.setItem("verificationEmail", emailParam);
    }
  }, [searchParams]);

  useEffect(() => {
    if (error) setPassword("");
  }, [error]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    clearError();

    const errors = validateForm({ email, password }, {
      email: validationRules.email,
      password: validationRules.password,
    });

    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);
      return;
    }

    await login(email, password);
  };

  const handleInputChange = (field: keyof LoginFormData, value: string) => {
    if (field === "email") setEmail(value);
    if (field === "password") setPassword(value);
    setValidationErrors(prev => ({ ...prev, [field]: undefined }));
  };

  return (
    <div className="login-page-wrapper">
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-12 col-sm-10 col-md-7 col-lg-5 col-xl-4">
            <div className="login-card">
              <div className="login-brand">
                <img src="/apexbooking-logo.svg" alt="ApexBooking" className="login-logo" />
                <span className="login-brand-name">ApexBooking</span>
              </div>

              <h2 className="login-heading">Welcome back</h2>
              <p className="login-subheading">
                Sign in to continue to your workspace.
              </p>

              {successMessage && (
                <Alert variant="success" dismissible onDismiss={() => setSuccessMessage(null)} className="mb-4">
                  {successMessage}
                </Alert>
              )}

              {error && (
                <div className="alert alert-danger mb-4" role="alert">
                  {error}
                </div>
              )}

              <form onSubmit={handleSubmit}>
                <div className="mb-3">
                  <Input
                    label="Email address"
                    type="email"
                    placeholder="name@example.com"
                    value={email}
                    onChange={(val) => handleInputChange("email", val)}
                    required
                    error={validationErrors.email}
                  />
                </div>

                <div className="mb-3">
                  <Input
                    label="Password"
                    type="password"
                    placeholder="Enter your password"
                    value={password}
                    onChange={(val) => handleInputChange("password", val)}
                    required
                    error={validationErrors.password}
                  />
                </div>

                <div className="d-flex justify-content-between align-items-center mb-4">
                  <div className="form-check">
                    <input type="checkbox" className="form-check-input" id="remember" />
                    <label className="form-check-label small text-muted" htmlFor="remember">
                      Remember me
                    </label>
                  </div>
                  <Link to="/forgot-password" className="small login-forgot-link">
                    Forgot password?
                  </Link>
                </div>

                <Button type="submit" variant="primary" loading={isLoading} className="w-100 py-2 fw-semibold">
                  Sign in
                </Button>
              </form>

              <CustomerRegisterLink searchParams={searchParams} />

              {!isCustomerPath && (
                <p className="text-center small text-muted login-access-link">
                  Don't have access?{' '}
                  <Link to="/request-access" className="text-decoration-none fw-semibold">
                    Request access
                  </Link>
                </p>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginForm;
