import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { PlatformOverviewDto } from '../types';

export const useSuperAdminOrganizations = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [overview, setOverview] = useState<PlatformOverviewDto | undefined>(undefined);

  const loadOverview = useCallback(async (): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<PlatformOverviewDto>('/superadmin/overview');
      setOverview(result ?? undefined);
    } catch (err: any) {
      setError(err?.message || 'Failed to load organizations.');
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
