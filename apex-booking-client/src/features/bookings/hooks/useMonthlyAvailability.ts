import { useState, useCallback, useEffect } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { BaseResponse } from '../../../types';

interface DayAvailabilityDto {
  date: string;
  isAvailable: boolean;
}

interface MonthlyAvailabilityDto {
  year: number;
  month: number;
  days: DayAvailabilityDto[];
}

export const useMonthlyAvailability = (slug: string, serviceId: string | null) => {
  const [availableDays, setAvailableDays] = useState<Set<string> | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchMonth = useCallback(async (year: number, month: number) => {
    if (!slug || !serviceId) return;

    setIsLoading(true);
    setError(null);

    try {
      const response = await axiosInstance.get<BaseResponse<MonthlyAvailabilityDto>>(
        `/public/${slug}/services/${serviceId}/monthly-availability`,
        { params: { year, month } }
      );

      if (response.isSuccess && response.data) {
        const availableDates = response.data.days
          .filter(d => d.isAvailable)
          .map(d => d.date);
        
        setAvailableDays(new Set(availableDates));
      } else {
        setError(response.errors?.[0]?.message || 'Failed to load availability');
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load availability');
    } finally {
      setIsLoading(false);
    }
  }, [slug, serviceId]);

  // Initial fetch for current month
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
