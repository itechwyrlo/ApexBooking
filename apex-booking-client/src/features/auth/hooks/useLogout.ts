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

export const useLogout = (): UseLogoutReturn => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { clearAllAuthData } = useAuth();
  const navigate = useNavigate();

  const clearError = () => setError(null);

  const logout = async () => {
    setIsLoading(true);
    setError(null);

    try {
      // Call backend logout endpoint
      await axiosInstance.post('/auth/logout', {} as LogoutRequest);
    } catch (err: any) {
      // Even if logout fails, we should still clear local state
      console.error('Logout API call failed:', err);
    } finally {
      // This single call now handles: 
      // 1. Clearing all auth states 2. Clearing sessionStorage 3. Resetting claims
      clearAllAuthData();
      
      // Redirect to login
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
