import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { PagedResult } from '../../../types';
import type {
  Booking,
  UpdateBookingRequest,
} from '../types';

export interface AdminBookingForm {
  serviceId: string;
  resourceId: string;
  scheduledDate: string;
  scheduledStartTime: string;
  customerNotes: string;
}

export const useBookings = () => {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [total, setTotal] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const getAll = useCallback(async (pageNumber = 1, pageSize = 10) => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<PagedResult<Booking>>('/booking', {
        params: { pageNumber, pageSize }
      });
      setBookings(result.data ?? []);
      setTotal(result.total ?? 0);
    } catch (err: any) {
      setError(err?.message || 'Failed to load bookings.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const create = useCallback(
    async (request: AdminBookingForm): Promise<boolean> => {
      setIsLoading(true);
      setError(null);
      try {
        await axiosInstance.post('/booking', request);
        await getAll();
        return true;
      } catch (err: any) {
        setError(err?.message || 'Failed to create booking.');
        return false;
      } finally {
        setIsLoading(false);
      }
    },
    [getAll]
  );

  const update = useCallback(
    async (bookingId: string, request: UpdateBookingRequest): Promise<boolean> => {
      setIsLoading(true);
      setError(null);
      try {
        await axiosInstance.patch(`/booking/${bookingId}`, request);
        await getAll();
        return true;
      } catch (err: any) {
        setError(err?.message || 'Failed to update booking.');
        return false;
      } finally {
        setIsLoading(false);
      }
    },
    [getAll]
  );

  const cancel = useCallback(
    async (bookingId: string, onSuccess?: () => Promise<void>): Promise<boolean> => {
      setIsLoading(true);
      setError(null);
      try {
        await axiosInstance.post(`/booking/${bookingId}/cancel`, { reason: null });
        if (onSuccess) {
          await onSuccess();
        } else {
          await getAll();
        }
        return true;
      } catch (err: any) {
        setError(err?.message || 'Failed to cancel booking.');
        return false;
      } finally {
        setIsLoading(false);
      }
    },
    [getAll]
  );

  const confirm = useCallback(async (bookingId: string, onSuccess?: () => Promise<void>): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      await axiosInstance.post(`/booking/${bookingId}/confirm`);
      if (onSuccess) {
        await onSuccess();
      } else {
        await getAll();
      }
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to confirm booking.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [getAll]);

  return {
    bookings,
    total,
    isLoading,
    error,
    clearError: () => setError(null),
    getAll,
    create,
    update,
    cancel,
    confirm,
  };
};
