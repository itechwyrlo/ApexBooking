import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { Service, CreateServiceRequest, UpdateServiceRequest } from '../types';
import type { PagedResult } from '../../../types';

export const useServices = () => {
  const [services, setServices] = useState<Service[]>([]);
  const [total, setTotal] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const getAll = useCallback(async (pageNumber = 1, pageSize = 10) => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<PagedResult<Service>>('/service', {
        params: { pageNumber, pageSize },
      });
      setServices(result.data ?? []);
      setTotal(result.total ?? 0);
    } catch (err: any) {
      setError(err?.message || 'Failed to load services.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const create = useCallback(async (request: CreateServiceRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      await axiosInstance.post('/service', request);
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to create service.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const update = useCallback(async (serviceId: string, request: UpdateServiceRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      await axiosInstance.patch(`/service/${serviceId}`, request);
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to update service.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const deactivate = useCallback(async (serviceId: string): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      await axiosInstance.patch(`/service/${serviceId}/status`, {});
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to deactivate service.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  return {
    services,
    total,
    isLoading,
    error,
    clearError: () => setError(null),
    getAll,
    create,
    update,
    deactivate,
  };
};
