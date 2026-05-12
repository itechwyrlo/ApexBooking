import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type {
  TenantRequestDto,
  TenantRequestDetailDto,
  ApproveTenantRequestRequest,
  RejectTenantRequestRequest,
} from '../types';
import type { BaseResponse } from '../../../types';

export const useTenantRequests = () => {
  const [requests, setRequests] = useState<TenantRequestDto[]>([]);
  const [selectedRequest, setSelectedRequest] = useState<TenantRequestDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const fetchRequests = useCallback(async (status?: string): Promise<TenantRequestDto[]> => {
    setIsLoading(true);
    setError(null);
    try {
      const url = status
        ? `/superadmin/tenant-requests?status=${status}`
        : '/superadmin/tenant-requests';
      const result = await axiosInstance.get<BaseResponse<TenantRequestDto[]>>(url);
      if (!result.isSuccess) {
        setError(result.errors?.[0]?.message ?? 'Failed to load requests.');
        return [];
      }
      const data = result.data ?? [];
      setRequests(data);
      return data;
    } catch {
      setError('Failed to load requests.');
      return [];
    } finally {
      setIsLoading(false);
    }
  }, []);

  const fetchById = useCallback(async (id: string): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<BaseResponse<TenantRequestDetailDto>>(
        `/superadmin/tenant-requests/${id}`
      );
      if (!result.isSuccess) {
        setError(result.errors?.[0]?.message ?? 'Failed to load request.');
        return;
      }
      setSelectedRequest(result.data ?? null);
    } catch {
      setError('Failed to load request.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const approve = useCallback(
    async (id: string, data: ApproveTenantRequestRequest): Promise<boolean> => {
      setIsSubmitting(true);
      setActionError(null);
      try {
        const result = await axiosInstance.post<BaseResponse<void>>(
          `/superadmin/tenant-requests/${id}/approve`,
          data
        );
        if (!result.isSuccess) {
          setActionError(result.errors?.[0]?.message ?? 'Approval failed.');
          return false;
        }
        return true;
      } catch {
        setActionError('Approval failed.');
        return false;
      } finally {
        setIsSubmitting(false);
      }
    },
    []
  );

  const reject = useCallback(
    async (id: string, data: RejectTenantRequestRequest): Promise<boolean> => {
      setIsSubmitting(true);
      setActionError(null);
      try {
        const result = await axiosInstance.post<BaseResponse<void>>(
          `/superadmin/tenant-requests/${id}/reject`,
          data
        );
        if (!result.isSuccess) {
          setActionError(result.errors?.[0]?.message ?? 'Rejection failed.');
          return false;
        }
        return true;
      } catch {
        setActionError('Rejection failed.');
        return false;
      } finally {
        setIsSubmitting(false);
      }
    },
    []
  );

  return {
    requests,
    selectedRequest,
    isLoading,
    isSubmitting,
    error,
    actionError,
    clearError: () => setError(null),
    clearActionError: () => setActionError(null),
    fetchRequests,
    fetchById,
    approve,
    reject,
  };
};
