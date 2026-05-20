import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { PagedResult } from '../../../types';
import type { ClientSummaryDto } from '../types';

export const useClients = () => {
  const [clients, setClients] = useState<ClientSummaryDto[]>([]);
  const [total, setTotal] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const getAll = useCallback(async (pageNumber = 1, pageSize = 10) => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<PagedResult<ClientSummaryDto>>('/client', {
        params: { pageNumber, pageSize },
      });
      setClients(result.data ?? []);
      setTotal(result.total ?? 0);
    } catch (err: any) {
      setError(err?.message || 'Failed to load clients.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  return {
    clients,
    total,
    isLoading,
    error,
    clearError: () => setError(null),
    getAll,
  };
};
