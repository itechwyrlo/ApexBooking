import React, { useState } from "react";
import { Link } from "react-router-dom";
import { useForgotPassword } from "../hooks/useForgotPassword";
import { Button } from "../../../components/ui/Button";
import { Input } from "../../../components/ui/Input";
import { Alert } from "../../../components/ui/Alert";
import type { ValidationErrors } from "../../../utils/validation";
import { validateForm, validationRules } from "../../../utils/validation";

interface ForgotPasswordFormData {
  email: string;
}

const ForgotPasswordForm: React.FC = () => {
  const [email, setEmail] = useState("");
  const { forgotPassword, isLoading, error, success, clearError, clearSuccess } = useForgotPassword();
  const [validationErrors, setValidationErrors] = useState<ValidationErrors<ForgotPasswordFormData>>({});

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    clearError();
    clearSuccess();

    const errors = validateForm({ email }, {
      email: validationRules.email,
    });

    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);
      return;
    }

    await forgotPassword(email);
  };

  const handleInputChange = (field: keyof ForgotPasswordFormData, value: string) => {
    if (field === "email") setEmail(value);
    setValidationErrors(prev => ({ ...prev, [field]: undefined }));
  };

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-12 col-sm-10 col-md-6 col-lg-5">
            <div className="card border-0 shadow-sm p-4 p-md-5">
              <div className="text-center mb-4">
                <h4 className="fw-bold">Reset password</h4>
                <p className="text-muted small mb-0">
                  Or{" "}
                  <Link to="/login" className="text-decoration-none">
                    back to sign in
                  </Link>
                </p>
              </div>

              <form onSubmit={handleSubmit}>
                {error && (
                  <div className="alert alert-danger mb-3" role="alert">
                    {error}
                  </div>
                )}

                {success && (
                  <Alert variant="success" dismissible onDismiss={clearSuccess} className="mb-3">
                    {success}
                  </Alert>
                )}

                <div className="mb-4">
                  <Input
                    label="Email address"
                    type="email"
                    placeholder="Enter your email address"
                    value={email}
                    onChange={(val) => handleInputChange("email", val)}
                    required
                    error={validationErrors.email}
                  />
                  <div className="form-text small text-muted mt-1">
                    We'll send a password reset link to this email.
                  </div>
                </div>

                <Button type="submit" variant="primary" loading={isLoading} className="w-100 py-2 fw-semibold">
                  Send Reset Email
                </Button>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ForgotPasswordForm;
