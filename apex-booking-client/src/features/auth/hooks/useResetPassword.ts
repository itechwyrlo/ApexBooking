import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosInstance from '../../../services/axiosInstance';
import { useAuth } from '../../../context/AuthContext';
import type { ResetPasswordRequest, ResetPasswordResponse } from '../types';

interface UseResetPasswordReturn {
  resetPassword: (token: string, newPassword: string, confirmPassword: string) => Promise<void>;
  isLoading: boolean;
  error: string | null;
  success: string | null;
  clearError: () => void;
  clearSuccess: () => void;
}

export const useResetPassword = (): UseResetPasswordReturn => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const { setAccessToken } = useAuth();
  const navigate = useNavigate();

  const clearError = () => setError(null);
  const clearSuccess = () => setSuccess(null);

  const resetPassword = async (token: string, newPassword: string, confirmPassword: string) => {
    setIsLoading(true);
    setError(null);
    setSuccess(null);

    try {
      const result = await axiosInstance.post<ResetPasswordResponse>('/auth/reset-password', {
        token,
        newPassword,
        confirmPassword,
      } as ResetPasswordRequest);

      // Handle BaseResponse structure
      if (!result.isSuccess) {
        const errorMessage = result.errors?.[0]?.message || 'Password reset failed. Please try again.';
        setError(errorMessage);
        return;
      }

      const { accessToken } = result.data!;

      // If tokens are returned, user is automatically logged in
      if (accessToken) {
        // This single call now handles: 
        // 1. Storage 2. Authentication State 3. JWT Decoding 4. Setting User Info
        setAccessToken(accessToken);
        navigate('/dashboard');
      } else {
        setSuccess('Password reset successfully. Please login with your new password.');
        setTimeout(() => {
          navigate('/login');
        }, 2000);
      }
    } catch (err: any) {
      // Handle BaseResponse error structure
      const responseData = err.response?.data;
      let errorMessage = 'Password reset failed. Please try again.';
      
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

  return {
    resetPassword,
    isLoading,
    error,
    success,
    clearError,
    clearSuccess,
  };
};
