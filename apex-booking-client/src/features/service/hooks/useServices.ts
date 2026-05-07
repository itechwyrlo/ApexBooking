import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import { useAuth } from '../../../context/AuthContext';
import type { Service, CreateServiceRequest, UpdateServiceRequest } from '../types';

export const useServices = () => {
  const { tenantSlug } = useAuth();
  const [services, setServices] = useState<Service[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const headers = { 'X-Tenant': tenantSlug };

  const getAll = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get('/service', { headers });
      if (!result.isSuccess) {
        setError(result.errors?.[0]?.message ?? 'Failed to load services.');
        return;
      }
      setServices(result.data ?? []);
    } catch {
      setError('Failed to load services.');
    } finally {
      setIsLoading(false);
    }
  }, [tenantSlug]);

  const create = useCallback(async (request: CreateServiceRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.post('/service', request, { headers });
      if (result && !result.isSuccess) {
        setError(result.errors?.[0]?.message ?? 'Failed to create service.');
        return false;
      }
      await getAll();
      return true;
    } catch {
      setError('Failed to create service.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [tenantSlug, getAll]);

  const update = useCallback(async (serviceId: string, request: UpdateServiceRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.patch(`/service/${serviceId}`, request, { headers });
      if (result && !result.isSuccess) {
        setError(result.errors?.[0]?.message ?? 'Failed to update service.');
        return false;
      }
      await getAll();
      return true;
    } catch {
      setError('Failed to update service.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [tenantSlug, getAll]);

  const deactivate = useCallback(async (serviceId: string): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.patch(`/service/${serviceId}/status`, {}, { headers });
      if (result && !result.isSuccess) {
        setError(result.errors?.[0]?.message ?? 'Failed to deactivate service.');
        return false;
      }
      await getAll();
      return true;
    } catch {
      setError('Failed to deactivate service.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [tenantSlug, getAll]);

  return {
    services,
    isLoading,
    error,
    clearError: () => setError(null),
    getAll,
    create,
    update,
    deactivate,
  };
};