import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { Booking } from '../types';

export const useCalendarBookings = () => {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchMonth = useCallback(async (year: number, month: number) => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<Booking[]>('/booking/calendar', {
        params: { year, month },
      });
      setBookings(result ?? []);
    } catch (err: any) {
      setError(err?.message || 'Failed to load calendar bookings.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  return {
    bookings,
    isLoading,
    error,
    clearError: () => setError(null),
    fetchMonth,
  };
};
