import { useState } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { BaseResponse } from '../../../types';

export interface SubmitTenantRequestData {
  businessName: string;
  ownerFullName: string;
  ownerEmail: string;
  ownerPhone: string;
  plan: string;
  message?: string;
}

export const useTenantRequest = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submitRequest = async (data: SubmitTenantRequestData): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.post<BaseResponse<void>>(
        '/public/tenant-requests',
        data
      );
      if (!result.isSuccess) {
        setError(result.errors?.[0]?.message ?? 'Submission failed. Please try again.');
        return;
      }
      setIsSuccess(true);
    } catch {
      setError('Submission failed. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  return {
    submitRequest,
    isLoading,
    isSuccess,
    error,
    clearError: () => setError(null),
  };
};
