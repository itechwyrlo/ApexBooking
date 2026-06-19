import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { TenantPaymentPolicyDto, UpdateTenantPaymentPolicyRequest } from '../types';

export const usePaymentPolicy = () => {
  const [policy, setPolicy] = useState<TenantPaymentPolicyDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<TenantPaymentPolicyDto>('/settings/payment-policy');
      setPolicy(result ?? null);
    } catch (err: any) {
      setError(err?.message || 'Failed to load payment policy.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const update = async (request: UpdateTenantPaymentPolicyRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.patch<TenantPaymentPolicyDto>('/settings/payment-policy', request);
      setPolicy(result ?? null);
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to update payment policy.');
      return false;
    } finally {
      setIsLoading(false);
    }
  };

  return { policy, isLoading, error, clearError: () => setError(null), load, update };
};
