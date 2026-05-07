import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { useForgotPassword } from '../hooks/useForgotPassword';
import { Button } from '../../../components/ui/Button';
import { Input } from '../../../components/ui/Input';
import { Alert } from '../../../components/ui/Alert';
import type { ValidationErrors } from '../../../utils/validation';
import { validateForm, validationRules } from '../../../utils/validation';

interface ForgotPasswordFormData {
  email: string;
}

const ForgotPasswordForm: React.FC = () => {
  const [email, setEmail] = useState('');
  const { forgotPassword, isLoading, error, success, clearError, clearSuccess } = useForgotPassword();
  const [validationErrors, setValidationErrors] = useState<ValidationErrors<ForgotPasswordFormData>>({});

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    clearError();
    clearSuccess();
    
    // Validate form
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
    if (field === 'email') setEmail(value);
    // Clear error for this field when user types
    setValidationErrors(prev => ({ ...prev, [field]: undefined }));
  };

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div className="card shadow-sm border-0 p-4" style={{ width: '100%', maxWidth: '400px' }}>
        <div className="card-body">
          <div className="text-center mb-4">
            <h3 className="fw-bold">Reset password</h3>
            <p className="text-muted small">
              Or <Link to="/login" className="text-decoration-none">back to sign in</Link>
            </p>
          </div>

          <form onSubmit={handleSubmit}>
            {error && (
              <div className="mb-3">
                <Alert variant="error" dismissible onDismiss={clearError}>
                  {error}
                </Alert>
              </div>
            )}

            {success && (
              <div className="mb-3">
                <Alert variant="success" dismissible onDismiss={clearSuccess}>
                  {success}
                </Alert>
              </div>
            )}

            <div className="mb-4">
              <Input
                label="Email address"
                type="email"
                placeholder="Enter your email address"
                value={email}
                onChange={(val) => handleInputChange('email', val)}
                required
                error={validationErrors.email}
              />
              <div className="form-text small text-muted mt-1">
                We'll send a password reset link to this email.
              </div>
            </div>

            <Button
              type="submit"
              className="btn btn-primary w-100 py-2 fw-semibold"
              disabled={isLoading}
            >
              {isLoading ? "Sending..." : "Send Reset Email"}
            </Button>
          </form>
        </div>
      </div>
    </div>
  );
};

export default ForgotPasswordForm;
// import React, { useState } from 'react';
// import { Link } from 'react-router-dom';
// import { useForgotPassword } from '../hooks/useForgotPassword';
// import { Button } from '../../../components/ui/Button';
// import { Input } from '../../../components/ui/Input';
// import { Alert } from '../../../components/ui/Alert';

// const ForgotPasswordForm: React.FC = () => {
//   const [email, setEmail] = useState('');
//   const { forgotPassword, isLoading, error, success, clearError, clearSuccess } = useForgotPassword();

//   const handleSubmit = async (e: React.FormEvent) => {
//     e.preventDefault();
//     clearError();
//     clearSuccess();
//     await forgotPassword(email);
//   };

//   return (
//     <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
//       <div className="card shadow-sm border-0 p-4" style={{ width: '100%', maxWidth: '400px' }}>
//         <div className="card-body">
//           <div className="text-center mb-4">
//             <h3 className="fw-bold">Reset password</h3>
//             <p className="text-muted small">
//               Or <Link to="/login" className="text-decoration-none">back to sign in</Link>
//             </p>
//           </div>

//           <form onSubmit={handleSubmit}>
//             {error && (
//               <div className="mb-3">
//                 <Alert variant="error" dismissible onDismiss={clearError}>
//                   {error}
//                 </Alert>
//               </div>
//             )}

//             {success && (
//               <div className="mb-3">
//                 <Alert variant="success" dismissible onDismiss={clearSuccess}>
//                   {success}
//                 </Alert>
//               </div>
//             )}

//             <div className="mb-4">
//               <Input
//                 label="Email address"
//                 type="email"
//                 placeholder="Enter your email address"
//                 value={email}
//                 onChange={(e: any) => setEmail(e.target.value)}
//                 required
//               />
//               <div className="form-text small text-muted mt-1">
//                 We'll send a password reset link to this email.
//               </div>
//             </div>

//             <Button
//               type="submit"
//               className="btn btn-primary w-100 py-2 fw-semibold"
//               disabled={isLoading}
//             >
//               {isLoading ? "Sending..." : "Send Reset Email"}
//             </Button>
//           </form>
//         </div>
//       </div>
//     </div>
//   );
// };

// export default ForgotPasswordForm;
