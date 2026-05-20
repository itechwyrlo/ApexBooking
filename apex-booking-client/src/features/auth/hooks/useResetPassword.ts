import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosInstance from '../../../services/axiosInstance';
import type { ResetPasswordRequest } from '../types';

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
  const navigate = useNavigate();

  const clearError = () => setError(null);
  const clearSuccess = () => setSuccess(null);

  const resetPassword = async (token: string, newPassword: string, confirmPassword: string) => {
    setIsLoading(true);
    setError(null);
    setSuccess(null);

    try {
      await axiosInstance.post('/auth/reset-password', {
        token,
        newPassword,
        confirmPassword,
      } as ResetPasswordRequest);

      setSuccess('Password reset successfully. Please login with your new password.');
      setTimeout(() => {
        navigate('/login');
      }, 2000);
    } catch (err: any) {
      setError(err?.message || 'Password reset failed. Please try again.');
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
