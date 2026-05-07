import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosInstance from '../../../services/axiosInstance';
import { useAuth } from '../../../context/AuthContext';
import type { RegisterAdminRequest, AuthResponse } from '../types';

interface UseRegisterReturn {
  register: (data: RegisterAdminRequest) => Promise<void>;
  isLoading: boolean;
  error: string | null;
  clearError: () => void;
}

export const useRegister = (): UseRegisterReturn => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { setAccessToken } = useAuth(); 
  const navigate = useNavigate();

  const register = async (data: RegisterAdminRequest) => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await axiosInstance.post<AuthResponse>('/auth/register/admin', data);

      // Handle BaseResponse structure
      if (!result.isSuccess) {
        setError(result.errors?.[0]?.message || 'Failed to send password reset email. Please try again.');
        return;
      }

      // This single call now handles: 
      // 1. Storage 2. Authentication State 3. JWT Decoding 4. Setting User Info
      setAccessToken(result.data!.accessToken);

      // Since the context now knows email_verified is FALSE, 
      // the ProtectedRoute will keep them on the verification path.
      navigate('/verify-email-notice');
      
    } catch (err: any) {
      // Handle BaseResponse error structure
      const responseData = err.response?.data;
      let errorMessage = 'Registration failed. Please try again.';
      
      if (responseData?.errors && responseData.errors.length > 0) {
        errorMessage = responseData.errors[0].message;
      } else if (responseData?.message) {
        errorMessage = responseData.message;
      }
      
      setError(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  return { register, isLoading, error, clearError: () => setError(null) };
};