import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { ClientDetailDto } from '../types';

export const useClientDetail = () => {
  const [detail, setDetail] = useState<ClientDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const getByEmail = useCallback(async (email: string) => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<ClientDetailDto>('/client/detail', {
        params: { email },
      });
      setDetail(result);
    } catch (err: any) {
      setError(err?.message || 'Failed to load client details.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const clear = useCallback(() => {
    setDetail(null);
    setError(null);
  }, []);

  return {
    detail,
    isLoading,
    error,
    getByEmail,
    clear,
  };
};
