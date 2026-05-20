import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { TenantProfileDto, UpdateTenantProfileRequest } from '../types';

export const useTenantProfile = (slug: string | null) => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [profile, setProfile] = useState<TenantProfileDto | null>(null);

  const load = useCallback(async (): Promise<void> => {
    if (!slug) return;
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<TenantProfileDto>(`/tenant/profile/${slug}`);
      setProfile(result ?? null);
    } catch (err: any) {
      setError(err?.message || 'Failed to load profile.');
    } finally {
      setIsLoading(false);
    }
  }, [slug]);

  const update = useCallback(async (request: UpdateTenantProfileRequest): Promise<boolean> => {
    if (!slug) return false;
    setIsLoading(true);
    setError(null);
    try {
      await axiosInstance.put(`/tenant/profile/${slug}`, request);
      await load();
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to update profile.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [slug, load]);

  return {
    profile,
    isLoading,
    error,
    clearError: () => setError(null),
    load,
    update,
  };
};
