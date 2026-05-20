import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { NotificationSummaryDto } from '../types';

export const useNotifications = () => {
  const [data, setData] = useState<NotificationSummaryDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isMarkingRead, setIsMarkingRead] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetch = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<NotificationSummaryDto>('/notification');
      setData(result ?? null);
    } catch (err: any) {
      setError(err?.message || 'Failed to load notifications.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const markAllRead = useCallback(async () => {
    setIsMarkingRead(true);
    try {
      await axiosInstance.patch('/notification/read-all');
      setData(prev =>
        prev
          ? {
              ...prev,
              unreadCount: 0,
              items: prev.items.map(n => ({ ...n, isRead: true })),
            }
          : prev
      );
    } catch {
    } finally {
      setIsMarkingRead(false);
    }
  }, []);

  return { data, isLoading, isMarkingRead, error, fetch, markAllRead };
};
