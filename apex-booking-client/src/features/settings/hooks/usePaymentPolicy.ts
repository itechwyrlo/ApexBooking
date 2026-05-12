import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { BaseResponse } from '../../../types';
import type { TenantPaymentPolicyDto, UpdateTenantPaymentPolicyRequest } from '../types';

export const usePaymentPolicy = () => {
  const [policy, setPolicy] = useState<TenantPaymentPolicyDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<BaseResponse<TenantPaymentPolicyDto>>(
        '/settings/payment-policy'
      );
      if (!result.isSuccess) {
        setError(result?.errors?.[0]?.message ?? 'Failed to load payment policy.');
        return;
      }
      setPolicy(result.data ?? null);
    } catch {
      setError('Failed to load payment policy.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const update = async (request: UpdateTenantPaymentPolicyRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.patch<BaseResponse<TenantPaymentPolicyDto>>(
        '/settings/payment-policy',
        request
      );
      if (!result.isSuccess) {
        setError(result?.errors?.[0]?.message ?? 'Failed to update payment policy.');
        return false;
      }
      setPolicy(result.data ?? null);
      return true;
    } catch {
      setError('Failed to update payment policy.');
      return false;
    } finally {
      setIsLoading(false);
    }
  };

  return { policy, isLoading, error, clearError: () => setError(null), load, update };
};
