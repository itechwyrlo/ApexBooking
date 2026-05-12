import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosInstance from '../../../services/axiosInstance';
import { useAuth } from '../../../context/AuthContext';
import type { BaseResponse } from '../../../types';
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
      const result = await axiosInstance.post<BaseResponse<AuthResponseData>>('/auth/accept-invitation', {
        token,
        newPassword,
        confirmPassword,
      });
      if (!result.isSuccess) {
        setError(result.errors?.[0]?.message ?? 'Failed to accept invitation.');
        return;
      }
      const { accessToken, tenantSlug } = result.data!;
      setAccessToken(accessToken);
      sessionStorage.setItem('isAuthenticated', 'true');
      if (tenantSlug) {
        setTenantSlug(tenantSlug);
        navigate(`/t/${tenantSlug}/dashboard`);
      } else {
        navigate('/login');
      }
    } catch (err: any) {
      const responseData = err.response?.data;
      setError(
        responseData?.errors?.[0]?.message ?? responseData?.message ?? 'Failed to accept invitation.'
      );
    } finally {
      setIsLoading(false);
    }
  };

  return { acceptInvitation, isLoading, error, clearError: () => setError(null) };
};
