import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type {
  ConfigurePlatformPaymentGatewayRequest,
  PlatformPaymentGatewayDto,
  GatewayProvider,
  GatewayMode,
} from '../types';
import type { BaseResponse } from '../../../types';

interface GatewayPrefill {
  gatewayProvider: GatewayProvider;
  mode: GatewayMode;
}

const DEFAULT_PREFILL: GatewayPrefill = {
  gatewayProvider: 'PayPal',
  mode: 'Test',
};

export const usePlatformPaymentGateway = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // undefined = not yet fetched, null = fetched but no gateway, object = fetched with data
  const [gateway, setGateway] = useState<PlatformPaymentGatewayDto | null | undefined>(undefined);
  const [prefill, setPrefill] = useState<GatewayPrefill>(DEFAULT_PREFILL);

  const loadGateway = useCallback(async (): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<BaseResponse<PlatformPaymentGatewayDto>>(
        '/superadmin/payment-gateway'
      );
      if (!result.isSuccess) {
        setError(result?.errors?.[0]?.message ?? 'Failed to load gateway.');
        return;
      }
      setGateway(result.data ?? null);
      if (result.data) {
        setPrefill({
          gatewayProvider: result.data.gatewayProvider,
          mode: result.data.mode,
        });
      }
    } catch {
      setError('Failed to load gateway.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const configure = async (
    request: ConfigurePlatformPaymentGatewayRequest
  ): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.post<BaseResponse<PlatformPaymentGatewayDto>>(
        '/superadmin/payment-gateway',
        request
      );
      if (!result.isSuccess) {
        setError(result?.errors?.[0]?.message ?? 'Failed to configure gateway.');
        return false;
      }
      setGateway(result.data ?? null);
      if (result.data) {
        setPrefill({
          gatewayProvider: result.data.gatewayProvider,
          mode: result.data.mode,
        });
      }
      return true;
    } catch {
      setError('Failed to configure gateway.');
      return false;
    } finally {
      setIsLoading(false);
    }
  };

  return {
    loadGateway,
    configure,
    gateway,
    prefill,
    isLoading,
    error,
    clearError: () => setError(null),
  };
};
