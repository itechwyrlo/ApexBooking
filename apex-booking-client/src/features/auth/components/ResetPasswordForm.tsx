import React, { useState, useEffect } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useResetPassword } from "../hooks/useResetPassword";
import { Button } from "../../../components/ui/Button";
import { Input } from "../../../components/ui/Input";
import { Alert } from "../../../components/ui/Alert";
import type { ValidationErrors } from "../../../utils/validation";
import { validateForm, validationRules } from "../../../utils/validation";

interface ResetPasswordFormData {
  userId: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}

const ResetPasswordForm: React.FC = () => {
  const [searchParams] = useSearchParams();
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [token, setToken] = useState("");
  const [userId, setUserId] = useState("");
  const [validationErrors, setValidationErrors] = useState<ValidationErrors<ResetPasswordFormData>>({});

  const { resetPassword, isLoading, error, success, clearError, clearSuccess } = useResetPassword();

  useEffect(() => {
    const tokenParam = searchParams.get("token");
    const userIdParam = searchParams.get("userId");
    if (tokenParam) setToken(tokenParam);
    if (userIdParam) setUserId(userIdParam);
  }, [searchParams]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    clearError();
    clearSuccess();

    if (newPassword !== confirmPassword) return;

    const errors = validateForm({ userId, token, newPassword, confirmPassword }, {
      token: validationRules.token,
      newPassword: validationRules.registerPassword,
      confirmPassword: [
        { validate: (v: string) => v === newPassword, message: "Confirm password must match the new password", severity: "error" },
      ],
    });

    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);
      return;
    }

    await resetPassword(userId, token, newPassword, confirmPassword);
  };

  const passwordMismatch = newPassword && confirmPassword && newPassword !== confirmPassword;

  const handleInputChange = (field: keyof ResetPasswordFormData, value: string) => {
    if (field === "newPassword") setNewPassword(value);
    if (field === "confirmPassword") setConfirmPassword(value);
    setValidationErrors(prev => ({ ...prev, [field]: undefined }));
  };

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-12 col-sm-10 col-md-6 col-lg-5">
            <div className="card border-0 shadow-sm p-4 p-md-5">
              <div className="text-center mb-4">
                <h4 className="fw-bold">Set new password</h4>
                <p className="text-muted small mb-0">Please enter your new credentials below</p>
              </div>

              {!token || !userId ? (
                <div className="text-center">
                  <div className="alert alert-danger mb-3" role="alert">
                    Invalid or missing reset token.
                  </div>
                  <Link to="/forgot-password" className="btn btn-link btn-sm text-decoration-none">
                    Request a new link
                  </Link>
                </div>
              ) : (
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

                  <div className="mb-3">
                    <Input
                      label="New Password"
                      type="password"
                      placeholder="Enter new password"
                      value={newPassword}
                      onChange={(val) => handleInputChange("newPassword", val)}
                      required
                      showToggle
                      error={validationErrors.newPassword}
                    />
                  </div>

                  <div className="mb-4">
                    <Input
                      label="Confirm Password"
                      type="password"
                      placeholder="Confirm new password"
                      value={confirmPassword}
                      onChange={(val) => handleInputChange("confirmPassword", val)}
                      required
                      showToggle
                      error={passwordMismatch ? "Passwords do not match" : validationErrors.confirmPassword}
                    />
                  </div>

                  <Button
                    type="submit"
                    variant="primary"
                    loading={isLoading}
                    disabled={!!passwordMismatch || !token || !userId}
                    className="w-100 py-2 fw-semibold"
                  >
                    Reset Password
                  </Button>

                  <div className="text-center mt-3">
                    <Link to="/login" className="small text-decoration-none text-muted">
                      Back to sign in
                    </Link>
                  </div>
                </form>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ResetPasswordForm;
