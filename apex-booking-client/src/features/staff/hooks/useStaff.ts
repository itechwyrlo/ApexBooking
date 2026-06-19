import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { PagedResult } from '../../../types';
import type { StaffDto, CreateStaffRequest, UpdateStaffRequest } from '../types';

export const useStaff = () => {
  const [staff, setStaff] = useState<StaffDto[]>([]);
  const [total, setTotal] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const getAll = useCallback(async (pageNumber = 1, pageSize = 10) => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<PagedResult<StaffDto>>('/staff', {
        params: { pageNumber, pageSize },
      });
      setStaff(result.data ?? []);
      setTotal(result.total ?? 0);
    } catch (err: any) {
      setError(err?.message || 'Failed to load staff.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const create = useCallback(async (request: CreateStaffRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      await axiosInstance.post('/staff', request);
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to create staff.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const update = useCallback(async (staffId: string, request: UpdateStaffRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      await axiosInstance.patch(`/staff/${staffId}`, request);
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to update staff.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const deactivate = useCallback(async (staffId: string): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      await axiosInstance.patch(`/staff/${staffId}/status`, {});
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to deactivate staff.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  return {
    staff,
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
