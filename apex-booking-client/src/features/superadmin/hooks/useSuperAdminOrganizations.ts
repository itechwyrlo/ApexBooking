import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { PlatformOverviewDto } from '../types';
import type { BaseResponse } from '../../../types';

export const useSuperAdminOrganizations = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [overview, setOverview] = useState<PlatformOverviewDto | undefined>(undefined);

  const loadOverview = useCallback(async (): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<BaseResponse<PlatformOverviewDto>>(
        '/superadmin/overview'
      );
      if (!result.isSuccess) {
        setError(result?.errors?.[0]?.message ?? 'Failed to load organizations.');
        return;
      }
      setOverview(result.data ?? undefined);
    } catch {
      setError('Failed to load organizations.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  return {
    loadOverview,
    overview,
    isLoading,
    error,
    clearError: () => setError(null),
  };
};
