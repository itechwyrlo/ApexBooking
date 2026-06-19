import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosInstance from '../../../services/axiosInstance';
import { useAuth } from '../../../context/AuthContext';
import type { LogoutRequest } from '../types';

interface UseLogoutReturn {
  logout: () => Promise<void>;
  isLoading: boolean;
  error: string | null;
  clearError: () => void;
}

export const useLogout = (currentFcmToken?: string | null): UseLogoutReturn => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { clearAllAuthData } = useAuth();
  const navigate = useNavigate();

  const clearError = () => setError(null);

  const logout = async () => {
    setIsLoading(true);
    setError(null);

    if (currentFcmToken) {
      try {
        await axiosInstance.delete('/notification/unregister-token', {
          data: { token: currentFcmToken },
        });
      } catch {
        // unregister is best-effort — server cleans up stale tokens automatically
      }
    }

    try {
      await axiosInstance.post('/auth/logout', {} as LogoutRequest);
    } catch (err: any) {
      console.error('Logout API call failed:', err);
    } finally {
      clearAllAuthData();
      navigate('/login');
      setIsLoading(false);
    }
  };

  return {
    logout,
    isLoading,
    error,
    clearError,
  };
};
