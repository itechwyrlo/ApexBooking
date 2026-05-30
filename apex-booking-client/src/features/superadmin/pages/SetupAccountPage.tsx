import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAcceptInvitation } from '../hooks/useAcceptInvitation';
import { Button } from '../../../components/ui/Button';
import { Input } from '../../../components/ui/Input';
import { Alert } from '../../../components/ui/Alert';

const SetupAccountPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const [token, setToken] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const { acceptInvitation, isLoading, error, clearError } = useAcceptInvitation();

  useEffect(() => {
    const t = searchParams.get('token');
    if (t) setToken(t);
  }, [searchParams]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    clearError();
    if (newPassword !== confirmPassword) return;
    await acceptInvitation(token, newPassword, confirmPassword);
  };

  const passwordMismatch = newPassword && confirmPassword && newPassword !== confirmPassword;

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-12 col-sm-10 col-md-6 col-lg-5">
            <div className="card border-0 shadow-sm p-4 p-md-5">
              <div className="text-center mb-4">
                <h4 className="fw-bold">Set up your account</h4>
                <p className="text-muted small mb-0">Create a password to activate your account.</p>
              </div>

              {!token ? (
                <Alert variant="error">
                  Invalid or expired invitation link. Contact your administrator to resend the invitation.
                </Alert>
              ) : (
                <form onSubmit={handleSubmit}>
                  {error && (
                    <div className="mb-3">
                      <Alert variant="error" dismissible onDismiss={clearError}>{error}</Alert>
                    </div>
                  )}

                  <div className="mb-3">
                    <Input
                      label="Password"
                      type="password"
                      placeholder="Create a password"
                      value={newPassword}
                      onChange={(val) => { setNewPassword(val); clearError(); }}
                      required
                      showToggle
                    />
                  </div>

                  <div className="mb-4">
                    <Input
                      label="Confirm Password"
                      type="password"
                      placeholder="Confirm your password"
                      value={confirmPassword}
                      onChange={(val) => { setConfirmPassword(val); clearError(); }}
                      required
                      showToggle
                      error={passwordMismatch ? 'Passwords do not match' : undefined}
                    />
                  </div>

                  <Button
                    type="submit"
                    variant="primary"
                    loading={isLoading}
                    disabled={!!passwordMismatch || !newPassword || !confirmPassword}
                    className="w-100 py-2 fw-semibold"
                  >
                    Activate Account
                  </Button>
                </form>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default SetupAccountPage;
