import { useState } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { BaseResponse } from '../../../types';

interface InitiatePaymentResult {
  approvalUrl: string;
  gatewayTransactionId: string;
  bookingReference: string;
}

export const useInitiatePayment = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const initiate = async (bookingId: string): Promise<string | null> => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await axiosInstance.post<BaseResponse<InitiatePaymentResult>>(
        `/booking/${bookingId}/payment`
      );

      if (!result.isSuccess) {
        setError(result?.errors?.[0]?.message ?? 'Failed to initiate payment.');
        return null;
      }

      return result.data?.approvalUrl ?? null;
    } catch {
      setError('Failed to initiate payment.');
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