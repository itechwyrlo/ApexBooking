import { useState, useCallback, useEffect } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { MonthlyAvailabilityDto } from '../types';

export const useMonthlyAvailability = (slug: string, serviceId: string | null) => {
  const [availableDays, setAvailableDays] = useState<Set<string> | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchMonth = useCallback(async (year: number, month: number) => {
    if (!slug || !serviceId) return;

    setIsLoading(true);
    setError(null);

    try {
      const response = await axiosInstance.get<MonthlyAvailabilityDto>(
        `/public/${slug}/services/${serviceId}/monthly-availability`,
        { params: { year, month } }
      );

      const availableDates = response.days
        .filter(d => d.isAvailable)
        .map(d => d.date);

      setAvailableDays(new Set(availableDates));
    } catch (err: any) {
      setError(err?.message || 'Failed to load availability');
    } finally {
      setIsLoading(false);
    }
  }, [slug, serviceId]);

  useEffect(() => {
    if (slug && serviceId) {
      const now = new Date();
      fetchMonth(now.getFullYear(), now.getMonth() + 1);
    }
  }, [slug, serviceId, fetchMonth]);

  return {
    availableDays,
    isLoading,
    error,
    fetchMonth
  };
};
