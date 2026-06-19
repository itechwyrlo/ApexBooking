import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { TenantSettingsDto, UpdateTenantSettingsRequest } from '../types';

export const useTenantSettings = () => {
  const [settings, setSettings] = useState<TenantSettingsDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<TenantSettingsDto>('/settings/tenant');
      setSettings(result ?? null);
    } catch (err: any) {
      setError(err?.message || 'Failed to load settings.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const update = async (request: UpdateTenantSettingsRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.patch<TenantSettingsDto>('/settings/tenant', request);
      setSettings(result ?? null);
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to update settings.');
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
