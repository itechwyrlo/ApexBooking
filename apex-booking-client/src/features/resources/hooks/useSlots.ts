import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { AvailableSlotsResponse } from '../types';

export const useSlots = () => {
  const [slots, setSlots] = useState<AvailableSlotsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchSlots = useCallback(async (
    serviceId: string,
    resourceId: string,
    date: string,
    tenantId: string
  ) => {
    setIsLoading(true);
    setError(null);
    setSlots(null);
    try {
      const res = await axiosInstance.get(`/service/${serviceId}/slots`, {
        params: { resourceId, date },
        headers: { 'X-Tenant-Id': tenantId },
      });
      if (!res.isSuccess) {
        setError(res.errors?.[0]?.message ?? 'Failed to load available slots.');
        return;
      }
      setSlots(res.data);
    } catch {
      setError('Failed to load available slots.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  return {
    slots,
    isLoading,
    error,
    clearError: () => setError(null),
    fetchSlots,
  };
};