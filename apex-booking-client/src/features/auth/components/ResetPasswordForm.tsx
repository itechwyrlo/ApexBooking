import React, { useState, useEffect } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useResetPassword } from '../hooks/useResetPassword';
import { Button } from '../../../components/ui/Button';
import { Input } from '../../../components/ui/Input';
import { Alert } from '../../../components/ui/Alert';
import type { ValidationErrors } from '../../../utils/validation';
import { validateForm, validationRules  } from '../../../utils/validation';

interface ResetPasswordFormData {
  token: string;
  newPassword: string;
  confirmPassword: string;
}

const ResetPasswordForm: React.FC = () => {
  const [searchParams] = useSearchParams();
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [token, setToken] = useState('');
  const [validationErrors, setValidationErrors] = useState<ValidationErrors<ResetPasswordFormData>>({});
  
  const { resetPassword, isLoading, error, success, clearError, clearSuccess } = useResetPassword();

  useEffect(() => {
    const tokenParam = searchParams.get('token');
    if (tokenParam) {
      setToken(tokenParam);
    }
  }, [searchParams]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    clearError();
    clearSuccess();
    
    if (newPassword !== confirmPassword) return;
    
    // Validate form
    const errors = validateForm({ token, newPassword, confirmPassword }, {
      token: validationRules.token,
      newPassword: validationRules.registerPassword,
      confirmPassword: [
        { validate: (v: string) => v === newPassword, message: "Confirm password must match the new password", severity: 'error' },
      ],
    });

    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);
      return;
    }
    
    await resetPassword(token, newPassword, confirmPassword);
  };

  const passwordMismatch = newPassword && confirmPassword && newPassword !== confirmPassword;

  const handleInputChange = (field: keyof ResetPasswordFormData, value: string) => {
    if (field === 'newPassword') setNewPassword(value);
    if (field === 'confirmPassword') setConfirmPassword(value);
    // Clear error for this field when user types
    setValidationErrors(prev => ({ ...prev, [field]: undefined }));
  };

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div className="card shadow-sm border-0 p-4" style={{ width: '100%', maxWidth: '400px' }}>
        <div className="card-body">
          <div className="text-center mb-4">
            <h3 className="fw-bold">Set new password</h3>
            <p className="text-muted small">
              Please enter your new credentials below
            </p>
          </div>

          {!token ? (
            <div className="text-center">
              <Alert variant="error">
                Invalid or missing reset token.
              </Alert>
              <Link to="/forgot-password" softness-sm="true" className="btn btn-link btn-sm mt-2 text-decoration-none">
                Request a new link
              </Link>
            </div>
          ) : (
            <form onSubmit={handleSubmit}>
              {(error || success) && (
                <div className="mb-3">
                  {error && <Alert variant="error" dismissible onDismiss={clearError}>{error}</Alert>}
                  {success && <Alert variant="success" dismissible onDismiss={clearSuccess}>{success}</Alert>}
                </div>
              )}

              <div className="mb-3">
                <Input
                  label="New Password"
                  type="password"
                  placeholder="Enter new password"
                  value={newPassword}
                  onChange={(val) => handleInputChange('newPassword', val)}
                  required
                  error={validationErrors.newPassword}
                />
              </div>

              <div className="mb-4">
                <Input
                  label="Confirm Password"
                  type="password"
                  placeholder="Confirm new password"
                  value={confirmPassword}
                  onChange={(val) => handleInputChange('confirmPassword', val)}
                  required
                  error={passwordMismatch ? 'Passwords do not match' : validationErrors.confirmPassword}
                />
              </div>

              <Button
                type="submit"
                className="btn btn-primary w-100 py-2 fw-semibold"
                disabled={isLoading || !!passwordMismatch || !token}
              >
                {isLoading ? "Updating..." : "Reset Password"}
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
  );
};

export default ResetPasswordForm;
// import React, { useState, useEffect } from 'react';
// import { Link, useSearchParams } from 'react-router-dom';
// import { useResetPassword } from '../hooks/useResetPassword';
// import { Button } from '../../../components/ui/Button';
// import { Input } from '../../../components/ui/Input';
// import { Alert } from '../../../components/ui/Alert';

// const ResetPasswordForm: React.FC = () => {
//   const [searchParams] = useSearchParams();
//   const [newPassword, setNewPassword] = useState('');
//   const [confirmPassword, setConfirmPassword] = useState('');
//   const [token, setToken] = useState('');
  
//   const { resetPassword, isLoading, error, success, clearError, clearSuccess } = useResetPassword();

//   useEffect(() => {
//     const tokenParam = searchParams.get('token');
//     if (tokenParam) {
//       setToken(tokenParam);
//     }
//   }, [searchParams]);

//   const handleSubmit = async (e: React.FormEvent) => {
//     e.preventDefault();
//     clearError();
//     clearSuccess();
    
//     if (newPassword !== confirmPassword) return;
    
//     await resetPassword(token, newPassword, confirmPassword);
//   };

//   const passwordMismatch = newPassword && confirmPassword && newPassword !== confirmPassword;

//   return (
//     <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
//       <div className="card shadow-sm border-0 p-4" style={{ width: '100%', maxWidth: '400px' }}>
//         <div className="card-body">
//           <div className="text-center mb-4">
//             <h3 className="fw-bold">Set new password</h3>
//             <p className="text-muted small">
//               Please enter your new credentials below
//             </p>
//           </div>

//           {!token ? (
//             <div className="text-center">
//               <Alert variant="error">
//                 Invalid or missing reset token.
//               </Alert>
//               <Link to="/forgot-password" softness-sm="true" className="btn btn-link btn-sm mt-2 text-decoration-none">
//                 Request a new link
//               </Link>
//             </div>
//           ) : (
//             <form onSubmit={handleSubmit}>
//               {(error || success) && (
//                 <div className="mb-3">
//                   {error && <Alert variant="error" dismissible onDismiss={clearError}>{error}</Alert>}
//                   {success && <Alert variant="success" dismissible onDismiss={clearSuccess}>{success}</Alert>}
//                 </div>
//               )}

//               <div className="mb-3">
//                 <Input
//                   label="New Password"
//                   type="password"
//                   placeholder="Enter new password"
//                   value={newPassword}
//                   onChange={(e: any) => setNewPassword(e.target.value)}
//                   required
//                 />
//               </div>

//               <div className="mb-4">
//                 <Input
//                   label="Confirm Password"
//                   type="password"
//                   placeholder="Confirm new password"
//                   value={confirmPassword}
//                   onChange={(e: any) => setConfirmPassword(e.target.value)}
//                   required
//                   error={passwordMismatch ? 'Passwords do not match' : undefined}
//                 />
//               </div>

//               <Button
//                 type="submit"
//                 className="btn btn-primary w-100 py-2 fw-semibold"
//                 disabled={isLoading || !!passwordMismatch || !token}
//               >
//                 {isLoading ? "Updating..." : "Reset Password"}
//               </Button>

//               <div className="text-center mt-3">
//                 <Link to="/login" className="small text-decoration-none text-muted">
//                   Back to sign in
//                 </Link>
//               </div>
//             </form>
//           )}
//         </div>
//       </div>
//     </div>
//   );
// };

// export default ResetPasswordForm;
