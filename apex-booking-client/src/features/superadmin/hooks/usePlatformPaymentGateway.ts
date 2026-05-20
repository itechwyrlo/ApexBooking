import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type {
  ConfigurePlatformPaymentGatewayRequest,
  PlatformPaymentGatewayDto,
  GatewayProvider,
  GatewayMode,
} from '../types';

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
  const [gateway, setGateway] = useState<PlatformPaymentGatewayDto | null | undefined>(undefined);
  const [prefill, setPrefill] = useState<GatewayPrefill>(DEFAULT_PREFILL);

  const loadGateway = useCallback(async (): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<PlatformPaymentGatewayDto>(
        '/superadmin/payment-gateway'
      );
      setGateway(result ?? null);
      if (result) {
        setPrefill({
          gatewayProvider: result.gatewayProvider,
          mode: result.mode,
        });
      }
    } catch (err: any) {
      setError(err?.message || 'Failed to load gateway.');
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
      const result = await axiosInstance.post<PlatformPaymentGatewayDto>(
        '/superadmin/payment-gateway',
        request
      );
      setGateway(result ?? null);
      if (result) {
        setPrefill({
          gatewayProvider: result.gatewayProvider,
          mode: result.mode,
        });
      }
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to configure gateway.');
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
