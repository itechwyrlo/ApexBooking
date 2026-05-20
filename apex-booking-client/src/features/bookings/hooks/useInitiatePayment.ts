import { useState } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { InitiatePaymentResult } from '../types';

export const useInitiatePayment = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const initiate = async (bookingId: string): Promise<string | null> => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await axiosInstance.post<InitiatePaymentResult>(
        `/booking/${bookingId}/payment`
      );

      return result.approvalUrl ?? null;
    } catch (err: any) {
      setError(err?.message || 'Failed to initiate payment.');
      return null;
    } finally {
      setIsLoading(false);
    }
  };

  return {
    initiate,
    isLoading,
    error,
    clearError: () => setError(null),
  };
};
