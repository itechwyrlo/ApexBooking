import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { useRegister } from '../hooks/useRegister';
import { Input } from '../../../components/ui/Input';
import { Button } from '../../../components/ui/Button';
import { Alert } from '../../../components/ui/Alert';
import type { RegisterAdminRequest } from '../types';
import type { ValidationErrors } from '../../../utils/validation';
import { validateForm, validationRules } from '../../../utils/validation';

const RegisterForm: React.FC = () => {
  const { register, isLoading, error, clearError } = useRegister();
  
  const [formData, setFormData] = useState<RegisterAdminRequest>({
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    organizationName: '',
    industry: '',
    phone: '',
    country: ''
  });

  const [validationErrors, setValidationErrors] = useState<ValidationErrors<RegisterAdminRequest>>({});

  const handleInputChange = (field: keyof RegisterAdminRequest, value: string) => {
    if (error) clearError();
    setFormData(prev => ({ ...prev, [field]: value }));
    // Clear error for this field when user types
    setValidationErrors(prev => ({ ...prev, [field]: undefined }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    // Validate form
    const errors = validateForm(formData, {
      firstName: validationRules.firstName,
      lastName: validationRules.lastName,
      email: validationRules.email,
      password: validationRules.registerPassword,
      organizationName: validationRules.organizationName,
      phone: validationRules.phone,
      country: validationRules.country,
    });

    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);
      return;
    }

    await register(formData);
  };

  return (
    <div className="container d-flex align-items-center justify-content-center min-vh-100">
      <div className="card shadow-sm w-100" style={{ maxWidth: '600px' }}>
        <div className="card-body p-4">
          <h2 className="text-center mb-4">Create Admin Account</h2>
          
          {error && <Alert variant="error" dismissible onDismiss={clearError} className="mb-3">{error}</Alert>}

          <form onSubmit={handleSubmit}>
            <div className="row">
              <div className="col-md-6 mb-3">
                <Input 
                  label="First Name" 
                  value={formData.firstName} 
                  onChange={(val) => handleInputChange('firstName', val)} 
                  required
                  error={validationErrors.firstName}
                />
              </div>
              <div className="col-md-6 mb-3">
                <Input 
                  label="Last Name" 
                  value={formData.lastName} 
                  onChange={(val) => handleInputChange('lastName', val)} 
                  required
                  error={validationErrors.lastName}
                />
              </div>
            </div>

            <div className="mb-3">
              <Input 
                label="Organization Name" 
                value={formData.organizationName} 
                onChange={(val) => handleInputChange('organizationName', val)} 
                required
                error={validationErrors.organizationName}
              />
            </div>

            <div className="row">
              <div className="col-md-6 mb-3">
                <Input 
                  label="Email" 
                  type="email" 
                  value={formData.email} 
                  onChange={(val) => handleInputChange('email', val)} 
                  required
                  error={validationErrors.email}
                />
              </div>
              <div className="col-md-6 mb-3">
                <Input 
                  label="Password" 
                  type="password" 
                  value={formData.password} 
                  onChange={(val) => handleInputChange('password', val)} 
                  required
                  error={validationErrors.password}
                />
              </div>
            </div>

            <div className="mb-3">
              <Input 
                label="Industry" 
                value={formData.industry} 
                onChange={(val) => handleInputChange('industry', val)} 
                required 
              />
            </div>

            <div className="row">
              <div className="col-md-6 mb-3">
                <Input 
                  label="Phone" 
                  value={formData.phone} 
                  onChange={(val) => handleInputChange('phone', val)} 
                  required
                  error={validationErrors.phone}
                />
              </div>
              <div className="col-md-6 mb-3">
                <Input 
                  label="Country" 
                  value={formData.country} 
                  onChange={(val) => handleInputChange('country', val)} 
                  required
                  error={validationErrors.country}
                />
              </div>
            </div>

            <div className="mt-4">
              <Button 
                type="submit" 
                variant="primary"
                loading={isLoading}
                disabled={isLoading}
                className="w-100"
              >
                Register & Setup Workspace
              </Button>
            </div>

            <div className="text-center mt-3">
              <span className="text-muted">Already have an account? </span>
              <Link to="/login" className="text-decoration-none">Login here</Link>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};

export default RegisterForm;

// import React, { useState } from 'react';
// import { Link } from 'react-router-dom';
// import { useRegister } from '../hooks/useRegister';
// import { Input } from '../../../components/ui/Input';
// import { Button } from '../../../components/ui/Button';
// import { Alert } from '../../../components/ui/Alert';
// import type { RegisterAdminRequest } from '../types';

// const RegisterForm: React.FC = () => {
//   const { register, isLoading, error, clearError } = useRegister();
  
//   const [formData, setFormData] = useState<RegisterAdminRequest>({
//     email: '',
//     password: '',
//     firstName: '',
//     lastName: '',
//     organizationName: '',
//     industry: '',
//     phone: '',
//     country: ''
//   });

//   const handleInputChange = (field: keyof RegisterAdminRequest, value: string) => {
//     if (error) clearError();
//     setFormData(prev => ({ ...prev, [field]: value }));
//   };

//   const handleSubmit = async (e: React.FormEvent) => {
//     e.preventDefault();
//     await register(formData);
//   };

//   return (
//     <div className="container d-flex align-items-center justify-content-center min-vh-100">
//       <div className="card shadow-sm w-100" style={{ maxWidth: '600px' }}>
//         <div className="card-body p-4">
//           <h2 className="text-center mb-4">Create Admin Account</h2>
          
//           {error && <Alert variant="error" dismissible onDismiss={clearError} className="mb-3">{error}</Alert>}

//           <form onSubmit={handleSubmit}>
//             <div className="row">
//               <div className="col-md-6 mb-3">
//                 <Input 
//                   label="First Name" 
//                   value={formData.firstName} 
//                   onChange={(val) => handleInputChange('firstName', val)} 
//                   required 
//                 />
//               </div>
//               <div className="col-md-6 mb-3">
//                 <Input 
//                   label="Last Name" 
//                   value={formData.lastName} 
//                   onChange={(val) => handleInputChange('lastName', val)} 
//                   required 
//                 />
//               </div>
//             </div>

//             <div className="mb-3">
//               <Input 
//                 label="Organization Name" 
//                 value={formData.organizationName} 
//                 onChange={(val) => handleInputChange('organizationName', val)} 
//                 required 
//               />
//             </div>

//             <div className="row">
//               <div className="col-md-6 mb-3">
//                 <Input 
//                   label="Email" 
//                   type="email" 
//                   value={formData.email} 
//                   onChange={(val) => handleInputChange('email', val)} 
//                   required 
//                 />
//               </div>
//               <div className="col-md-6 mb-3">
//                 <Input 
//                   label="Password" 
//                   type="password" 
//                   value={formData.password} 
//                   onChange={(val) => handleInputChange('password', val)} 
//                   required 
//                 />
//               </div>
//             </div>

//             <div className="mb-3">
//               <Input 
//                 label="Industry" 
//                 value={formData.industry} 
//                 onChange={(val) => handleInputChange('industry', val)} 
//                 required 
//               />
//             </div>

//             <div className="row">
//               <div className="col-md-6 mb-3">
//                 <Input 
//                   label="Phone" 
//                   value={formData.phone} 
//                   onChange={(val) => handleInputChange('phone', val)} 
//                   required 
//                 />
//               </div>
//               <div className="col-md-6 mb-3">
//                 <Input 
//                   label="Country" 
//                   value={formData.country} 
//                   onChange={(val) => handleInputChange('country', val)} 
//                   required 
//                 />
//               </div>
//             </div>

//             <div className="mt-4">
//               <Button 
//                 type="submit" 
//                 variant="primary"
//                 loading={isLoading}
//                 disabled={isLoading}
//                 className="w-100"
//               >
//                 Register & Setup Workspace
//               </Button>
//             </div>

//             <div className="text-center mt-3">
//               <span className="text-muted">Already have an account? </span>
//               <Link to="/login" className="text-decoration-none">Login here</Link>
//             </div>
//           </form>
//         </div>
//       </div>
//     </div>
//   );
// };

// export default RegisterForm;
