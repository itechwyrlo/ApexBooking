import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import { useAuth } from '../../../context/AuthContext';
import type {
  Booking,
  CreateBookingRequest,
  UpdateBookingRequest,
} from '../types';

export const useBookings = () => {
  const { tenantSlug } = useAuth();

  const [bookings, setBookings] = useState<Booking[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const headers = { 'X-Tenant': tenantSlug };

  const getAll = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await axiosInstance.get('/booking', { headers });

      if (!result.isSuccess) {
        setError(result.errors?.[0]?.message ?? 'Failed to load bookings.');
        return;
      }

      setBookings(result.data ?? []);
    } catch {
      setError('Failed to load bookings.');
    } finally {
      setIsLoading(false);
    }
  }, [tenantSlug]);

  const create = useCallback(
    async (request: CreateBookingRequest): Promise<boolean> => {
      setIsLoading(true);
      setError(null);

      try {
        const result = await axiosInstance.post('/booking', request, {
          headers,
        });

        if (result && !result.isSuccess) {
          setError(result.errors?.[0]?.message ?? 'Failed to create booking.');
          return false;
        }

        await getAll();
        return true;
      } catch {
        setError('Failed to create booking.');
        return false;
      } finally {
        setIsLoading(false);
      }
    },
    [tenantSlug, getAll]
  );

  const update = useCallback(
    async (bookingId: string, request: UpdateBookingRequest): Promise<boolean> => {
      setIsLoading(true);
      setError(null);

      try {
        const result = await axiosInstance.patch(
          `/booking/${bookingId}`,
          request,
          { headers }
        );

        if (result && !result.isSuccess) {
          setError(result.errors?.[0]?.message ?? 'Failed to update booking.');
          return false;
        }

        await getAll();
        return true;
      } catch {
        setError('Failed to update booking.');
        return false;
      } finally {
        setIsLoading(false);
      }
    },
    [tenantSlug, getAll]
  );

  const cancel = useCallback(
    async (bookingId: string): Promise<boolean> => {
      setIsLoading(true);
      setError(null);

      try {
        const result = await axiosInstance.patch(
          `/booking/${bookingId}/cancel`,
          {},
          { headers }
        );

        if (result && !result.isSuccess) {
          setError(result.errors?.[0]?.message ?? 'Failed to cancel booking.');
          return false;
        }

        await getAll();
        return true;
      } catch {
        setError('Failed to cancel booking.');
        return false;
      } finally {
        setIsLoading(false);
      }
    },
    [tenantSlug, getAll]
  );

  return {
    bookings,
    isLoading,
    error,
    clearError: () => setError(null),
    getAll,
    create,
    update,
    cancel,
  };
};