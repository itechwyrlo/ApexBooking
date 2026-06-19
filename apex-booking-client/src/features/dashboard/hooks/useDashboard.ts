import { useState, useEffect } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { DashboardSummaryDto } from '../types';

export function useDashboard() {
  const [data, setData] = useState<DashboardSummaryDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function fetchSummary() {
      setIsLoading(true);
      setError(null);
      try {
        const result = await axiosInstance.get<DashboardSummaryDto>('/dashboard/summary');
        if (!cancelled) setData(result);
      } catch (err: unknown) {
        if (!cancelled) {
          const apiErr = err as { message?: string };
          setError(apiErr.message ?? 'Failed to load dashboard.');
        }
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    }

    fetchSummary();
    return () => { cancelled = true; };
  }, []);

  return { data, isLoading, error };
}
