import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosInstance from '../../../services/axiosInstance';
import { useAuth } from '../../../context/AuthContext';
import type { AuthResponseData } from '../../auth/types';

export const useAcceptInvitation = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { setAccessToken, setTenantSlug } = useAuth();
  const navigate = useNavigate();

  const acceptInvitation = async (token: string, newPassword: string, confirmPassword: string) => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.post<AuthResponseData>('/auth/accept-invitation', {
        token,
        newPassword,
        confirmPassword,
      });
      const { accessToken, tenantSlug } = result;
      setAccessToken(accessToken);
      sessionStorage.setItem('isAuthenticated', 'true');
      if (tenantSlug) {
        setTenantSlug(tenantSlug);
        navigate(`/t/${tenantSlug}/dashboard`);
      } else {
        navigate('/login');
      }
    } catch (err: any) {
      setError(err?.message ?? 'Failed to accept invitation.');
    } finally {
      setIsLoading(false);
    }
  };

  return { acceptInvitation, isLoading, error, clearError: () => setError(null) };
};
