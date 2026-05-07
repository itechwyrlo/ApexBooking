import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { AvailableSlotsResponse } from '../types';

export const useSlots = () => {
  const [slots, setSlots] = useState<AvailableSlotsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchSlots = useCallback(
    async (serviceId: string, resourceId: string, date: string, slug: string) => {
      setIsLoading(true);
      setError(null);

      try {
        const result = await axiosInstance.get(
          `public/services/${serviceId}/slots`,
          { params: { resourceId, date, slug } }
        );

        if (!result.isSuccess) {
          setError(result.errors?.[0]?.message ?? 'Failed to load slots.');
          return null;
        }

        setSlots(result.data ?? null);
        return result.data;
      } catch {
        setError('Failed to load slots.');
        return null;
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  return {
    slots,
    isLoading,
    error,
    clearError: () => setError(null),
    fetchSlots,
  };
};