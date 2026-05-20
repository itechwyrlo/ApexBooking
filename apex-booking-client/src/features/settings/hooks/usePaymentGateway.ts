import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { TenantPaymentGatewayStatusDto } from '../types';

export const usePaymentGateway = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [gatewayStatus, setGatewayStatus] = useState<TenantPaymentGatewayStatusDto | null>(null);

  const loadGateway = useCallback(async (): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<TenantPaymentGatewayStatusDto>('/settings/payment-gateway');
      setGatewayStatus(result ?? null);
    } catch (err: any) {
      setError(err?.message || 'Failed to load gateway settings.');
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
