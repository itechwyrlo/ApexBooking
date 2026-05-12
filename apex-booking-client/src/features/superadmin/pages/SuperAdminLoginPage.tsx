import React, { useState } from 'react';
import { useSuperAdminLogin } from '../hooks/useSuperAdminLogin';
import { Input } from '../../../components/ui/Input';
import { Button } from '../../../components/ui/Button';
import { Alert } from '../../../components/ui/Alert';
import { validateForm, validationRules } from '../../../utils/validation';
import type { ValidationErrors } from '../../../utils/validation';

interface LoginFormData {
  email: string;
  password: string;
}

const SuperAdminLoginPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [validationErrors, setValidationErrors] = useState<ValidationErrors<LoginFormData>>({});
  const { login, isLoading, error, clearError } = useSuperAdminLogin();

  const handleSubmit = async (e: React.FormEvent) => {
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

  const handleChange = (field: keyof LoginFormData, value: string) => {
    if (field === 'email') setEmail(value);
    if (field === 'password') setPassword(value);
    setValidationErrors(prev => ({ ...prev, [field]: undefined }));
  };

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div className="card shadow-sm border-0 p-4" style={{ width: '100%', maxWidth: '400px' }}>
        <div className="text-center mb-4">
          <div
            className="bg-primary rounded d-flex align-items-center justify-content-center mx-auto mb-3"
            style={{ width: 44, height: 44 }}
          >
            <span className="text-white fw-bold">A</span>
          </div>
          <h3 className="fw-bold">Platform Admin Sign In</h3>
          <p className="text-muted small mb-0">ApexBooking Super Admin</p>
        </div>

        <form onSubmit={handleSubmit}>
          {error && (
            <div className="mb-3">
              <Alert variant="error" dismissible onDismiss={clearError}>
                {error}
              </Alert>
            </div>
          )}

          <div className="mb-3">
            <Input
              label="Email address"
              type="email"
              placeholder="admin@example.com"
              value={email}
              onChange={val => handleChange('email', val)}
              required
              error={validationErrors.email}
            />
          </div>

          <div className="mb-4">
            <Input
              label="Password"
              type="password"
              placeholder="Enter your password"
              value={password}
              onChange={val => handleChange('password', val)}
              required
              error={validationErrors.password}
            />
          </div>

          <Button
            type="submit"
            className="w-100 py-2 fw-semibold"
            loading={isLoading}
          >
            Sign in
          </Button>
        </form>
      </div>
    </div>
  );
};

export default SuperAdminLoginPage;
