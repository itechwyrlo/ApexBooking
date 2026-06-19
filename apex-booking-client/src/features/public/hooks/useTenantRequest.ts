import { useState } from 'react';
import axiosInstance from '../../../services/axiosInstance';

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
      await axiosInstance.post('/public/tenant-requests', data);
      setIsSuccess(true);
    } catch (err: any) {
      setError(err?.message || 'Submission failed. Please try again.');
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
