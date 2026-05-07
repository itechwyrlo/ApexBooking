import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosInstance from '../../../services/axiosInstance';
import type { ForgotPasswordRequest, ForgotPasswordResponse } from '../types';

interface UseForgotPasswordReturn {
  forgotPassword: (email: string) => Promise<void>;
  isLoading: boolean;
  error: string | null;
  success: string | null;
  clearError: () => void;
  clearSuccess: () => void;
}

export const useForgotPassword = (): UseForgotPasswordReturn => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const navigate = useNavigate();

  const clearError = () => setError(null);
  const clearSuccess = () => setSuccess(null);

  const forgotPassword = async (email: string) => {
    setIsLoading(true);
    setError(null);
    setSuccess(null);

    try {
      const result = await axiosInstance.post<ForgotPasswordResponse>('/auth/forgot-password', {
        email,
      } as ForgotPasswordRequest);

      if (!result.isSuccess) {
        setError(result.errors?.[0]?.message || 'Failed to send password reset email. Please try again.');
        return;
      }

      setSuccess(result.data?.message || 'Password reset email sent successfully.');

      setTimeout(() => {
        navigate('/login');
      }, 3000);
    } catch (err: any) {
      setError(
        err?.response?.data?.errors?.[0]?.message || 'Failed to send password reset email. Please try again.'
      );
    } finally {
      setIsLoading(false);
    }
  };

  return {
    forgotPassword,
    isLoading,
    error,
    success,
    clearError,
    clearSuccess,
  };
};