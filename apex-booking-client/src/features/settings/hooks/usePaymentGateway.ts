import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { TenantPaymentGatewayStatusDto } from '../types';
import type { BaseResponse } from '../../../types';

export const usePaymentGateway = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [gatewayStatus, setGatewayStatus] = useState<TenantPaymentGatewayStatusDto | null>(null);

  const loadGateway = useCallback(async (): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<BaseResponse<TenantPaymentGatewayStatusDto>>(
        '/settings/payment-gateway'
      );
      if (!result.isSuccess) {
        setError(result?.errors?.[0]?.message ?? 'Failed to load gateway settings.');
        return;
      }
      setGatewayStatus(result.data ?? null);
    } catch {
      setError('Failed to load gateway settings.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  return {
    loadGateway,
    gatewayStatus,
    isLoading,
    error,
    clearError: () => setError(null),
  };
};
