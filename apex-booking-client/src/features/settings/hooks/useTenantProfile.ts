import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { TenantProfileDto, UpdateTenantProfileRequest } from '../types';
import type { BaseResponse } from '../../../types';

export const useTenantProfile = (slug: string | null) => {
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [profile, setProfile] = useState<TenantProfileDto | null>(null);
  
    const load = useCallback(async (): Promise<void> => {
      if (!slug) return;
      setIsLoading(true);
      setError(null);
      try {
        const result = await axiosInstance.get<BaseResponse<TenantProfileDto>>(
          `/tenant/profile/${slug}`
        );
        if (!result.isSuccess) {
          setError(result?.errors?.[0]?.message ?? 'Failed to load profile.');
          return;
        }
        setProfile(result.data ?? null);
      } catch {
        setError('Failed to load profile.');
      } finally {
        setIsLoading(false);
      }
    }, [slug]);
  
    const update = useCallback(async (request: UpdateTenantProfileRequest): Promise<boolean> => {
      if (!slug) return false;
      setIsLoading(true);
      setError(null);
      try {
        const result = await axiosInstance.put<BaseResponse<boolean>>(
          `/tenant/profile/${slug}`,
          request
        );
        if (!result.isSuccess) {
          setError(result?.errors?.[0]?.message ?? 'Failed to update profile.');
          return false;
        }
        await load();
        return true;
      } catch {
        setError('Failed to update profile.');
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