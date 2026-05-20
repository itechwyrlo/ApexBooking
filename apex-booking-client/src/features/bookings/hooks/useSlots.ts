import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { AvailableSlotsResponse } from '../types';

export const useSlots = () => {
  const [slots, setSlots] = useState<AvailableSlotsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchSlots = useCallback(
    async (serviceId: string, resourceId: string | null, date: string, slug: string) => {
      setIsLoading(true);
      setError(null);

      try {
        const params: Record<string, string> = { date, slug };
        if (resourceId !== null) {
          params.resourceId = resourceId;
        }

        const result = await axiosInstance.get<AvailableSlotsResponse>(
          `/public/services/${serviceId}/slots`,
          { params }
        );

        setSlots(result ?? null);
        return result;
      } catch {
        setError('Failed to load slots.');
        setSlots(null);
        return null;
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  const clearSlots = useCallback(() => setSlots(null), []);

  return {
    slots,
    isLoading,
    error,
    clearError: () => setError(null),
    clearSlots,
    fetchSlots,
  };
};
