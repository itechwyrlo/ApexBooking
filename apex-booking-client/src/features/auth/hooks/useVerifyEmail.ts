import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { AccountVerificationResponseDto } from '../types';

export const useVerifyEmail = () => {
  const [data, setData] = useState<AccountVerificationResponseDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const verify = useCallback(async (token: string) => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<AccountVerificationResponseDto>(
        '/auth/verify-account',
        { params: { token } }
      );
      setData(result);
    } catch (err: any) {
      setError(err.message ?? 'Verification failed.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  return { verify, data, isLoading, error };
};
