import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosInstance from '../../../services/axiosInstance';
import { useAuth } from '../../../context/AuthContext';
import type { AuthResponseData } from '../../auth/types';

export const useSuperAdminLogin = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { setAccessToken } = useAuth();
  const navigate = useNavigate();

  const login = async (email: string, password: string): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.post<AuthResponseData>(
        '/auth/login/superadmin',
        { email, password }
      );
      const accessToken = result.accessToken;
      if (!accessToken) {
        setError('Missing access token.');
        return;
      }
      sessionStorage.setItem('access_token', accessToken);
      sessionStorage.setItem('isAuthenticated', 'true');
      sessionStorage.setItem('user_type', 'superadmin');
      setAccessToken(accessToken);
      navigate('/superadmin/payment-gateway');
    } catch (err: any) {
      setError(err?.message || 'Login failed.');
    } finally {
      setIsLoading(false);
    }
  };

  return {
    login,
    isLoading,
    error,
    clearError: () => setError(null),
  };
};
