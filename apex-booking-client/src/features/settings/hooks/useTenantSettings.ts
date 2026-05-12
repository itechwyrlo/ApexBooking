import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { BaseResponse } from '../../../types';
import type { TenantSettingsDto, UpdateTenantSettingsRequest } from '../types';

export const useTenantSettings = () => {
  const [settings, setSettings] = useState<TenantSettingsDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<BaseResponse<TenantSettingsDto>>(
        '/settings/tenant'
      );
      if (!result.isSuccess) {
        setError(result?.errors?.[0]?.message ?? 'Failed to load settings.');
        return;
      }
      setSettings(result.data ?? null);
    } catch {
      setError('Failed to load settings.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const update = async (request: UpdateTenantSettingsRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.patch<BaseResponse<TenantSettingsDto>>(
        '/settings/tenant',
        request
      );
      if (!result.isSuccess) {
        setError(result?.errors?.[0]?.message ?? 'Failed to update settings.');
        return false;
      }
      setSettings(result.data ?? null);
      return true;
    } catch {
      setError('Failed to update settings.');
      return false;
    } finally {
      setIsLoading(false);
    }
  };

  return {
    settings,
    isLoading,
    error,
    clearError: () => setError(null),
    load,
    update,
  };
};