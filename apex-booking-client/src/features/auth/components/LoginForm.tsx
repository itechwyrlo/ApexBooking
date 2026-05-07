import React, { useState, useEffect } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useLogin } from "../hooks/useLogin";
import { Button } from "../../../components/ui/Button";
import { Input } from "../../../components/ui/Input";
import { Alert } from "../../../components/ui/Alert";
import type { ValidationErrors } from "../../../utils/validation"
import { validateForm, validationRules,  } from "../../../utils/validation";

interface LoginFormData {
  email: string;
  password: string;
}

const LoginForm: React.FC = () => {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [searchParams] = useSearchParams();
  const { login, isLoading, error, clearError } = useLogin();
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<ValidationErrors<LoginFormData>>({});

  useEffect(() => {
    const message = searchParams.get('message');
    const verified = searchParams.get('verified');
    const tenant = searchParams.get('tenant');
    const token = searchParams.get('token');
    const email = searchParams.get('email');
    
    if (message === 'email-verified' || verified === 'email') {
      setSuccessMessage('Email verified successfully! You can now log in.');
      // Auto-redirect to tenant dashboard after successful login
      if (tenant) {
        // Store tenant slug for redirect after login
        sessionStorage.setItem('redirectTenant', tenant);
      }
      
      // Store additional parameters if needed
      if (token) {
        sessionStorage.setItem('verificationToken', token);
      }
      if (email) {
        sessionStorage.setItem('verificationEmail', email);
      }
    }
  }, [searchParams]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    clearError();
    
    // Validate form
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
    if (field === 'email') setEmail(value);
    if (field === 'password') setPassword(value);
    // Clear error for this field when user types
    setValidationErrors(prev => ({ ...prev, [field]: undefined }));
  };

  return (
    <>
      <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
        <div
          className="card shadow-sm border-0 p-4"
          style={{ width: "100%", maxWidth: "400px" }}
        >
          <div className="text-center mb-4">
            <h3 className="fw-bold">Sign in</h3>
            {successMessage && (
              <Alert variant="success" dismissible onDismiss={() => setSuccessMessage(null)} className="mb-3">
                {successMessage}
              </Alert>
            )}
            <p className="text-muted small">
              Or{" "}
              <Link to="/register" className="text-decoration-none">
                create a new account
              </Link>
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

            <div className="mb-3">
              <Input
                label="Email address"
                type="email"
                placeholder="name@example.com"
                value={email}
                onChange={(val) => handleInputChange('email', val)}
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
                onChange={(val) => handleInputChange('password', val)}
                required
                error={validationErrors.password}
              />
            </div>

            <div className="d-flex justify-content-between align-items-center mb-4">
              <div className="form-check">
                <input
                  type="checkbox"
                  className="form-check-input"
                  id="remember"
                />
                <label
                  className="form-check-label small text-muted"
                  htmlFor="remember"
                >
                  Remember me
                </label>
              </div>
              <Link
                to="/forgot-password"
                className="small text-decoration-none"
              >
                Forgot password?
              </Link>
            </div>

            <Button
              type="submit"
              className="btn btn-primary w-100 py-2 fw-semibold"
              disabled={isLoading}
            >
              {isLoading ? "Signing in..." : "Sign in"}
            </Button>
          </form>
        </div>
      </div>
    </>
  );
};

export default LoginForm;
// import React, { useState, useEffect } from "react";
// import { Link, useSearchParams } from "react-router-dom";
// import { useLogin } from "../hooks/useLogin";
// import { Button } from "../../../components/ui/Button";
// import { Input } from "../../../components/ui/Input";
// import { Alert } from "../../../components/ui/Alert";

// const LoginForm: React.FC = () => {
//   const [email, setEmail] = useState("");
//   const [password, setPassword] = useState("");
//   const [searchParams] = useSearchParams();
//   const { login, isLoading, error, clearError } = useLogin();
//   const [successMessage, setSuccessMessage] = useState<string | null>(null);

//   useEffect(() => {
//     const message = searchParams.get('message');
//     const verified = searchParams.get('verified');
//     const tenant = searchParams.get('tenant');
//     const token = searchParams.get('token');
//     const email = searchParams.get('email');
    
//     if (message === 'email-verified' || verified === 'email') {
//       setSuccessMessage('Email verified successfully! You can now log in.');
//       // Auto-redirect to tenant dashboard after successful login
//       if (tenant) {
//         // Store tenant slug for redirect after login
//         sessionStorage.setItem('redirectTenant', tenant);
//       }
      
//       // Store additional parameters if needed
//       if (token) {
//         sessionStorage.setItem('verificationToken', token);
//       }
//       if (email) {
//         sessionStorage.setItem('verificationEmail', email);
//       }
//     }
//   }, [searchParams]);

//   const handleSubmit = async (e: React.FormEvent) => {
//     e.preventDefault();
//     clearError();
//     await login(email, password);
//   };

//   return (
//     <>
//       <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
//         <div
//           className="card shadow-sm border-0 p-4"
//           style={{ width: "100%", maxWidth: "400px" }}
//         >
//           <div className="text-center mb-4">
//             <h3 className="fw-bold">Sign in</h3>
//             {successMessage && (
//               <Alert variant="success" dismissible onDismiss={() => setSuccessMessage(null)} className="mb-3">
//                 {successMessage}
//               </Alert>
//             )}
//             <p className="text-muted small">
//               Or{" "}
//               <Link to="/register" className="text-decoration-none">
//                 create a new account
//               </Link>
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

//             {/* Note: Removed manual <label> since <Input /> likely handles it via the 'label' prop */}
//             <div className="mb-3">
//               <Input
//                 label="Email address"
//                 type="email"
//                 placeholder="name@example.com"
//                 value={email}
//                 onChange={setEmail}
//                 required
//               />
//             </div>

//             <div className="mb-3">
//               <Input
//                 label="Password"
//                 type="password"
//                 placeholder="Enter your password"
//                 value={password}
//                 onChange={setPassword}
//                 required
//               />
//             </div>

//             <div className="d-flex justify-content-between align-items-center mb-4">
//               <div className="form-check">
//                 <input
//                   type="checkbox"
//                   className="form-check-input"
//                   id="remember"
//                 />
//                 <label
//                   className="form-check-label small text-muted"
//                   htmlFor="remember"
//                 >
//                   Remember me
//                 </label>
//               </div>
//               <Link
//                 to="/forgot-password"
//                 className="small text-decoration-none"
//               >
//                 Forgot password?
//               </Link>
//             </div>

//             <Button
//               type="submit"
//               className="btn btn-primary w-100 py-2 fw-semibold"
//               disabled={isLoading}
//             >
//               {isLoading ? "Signing in..." : "Sign in"}
//             </Button>
//           </form>
//         </div>
//       </div>
//     </>
//   );
// };

// export default LoginForm;
