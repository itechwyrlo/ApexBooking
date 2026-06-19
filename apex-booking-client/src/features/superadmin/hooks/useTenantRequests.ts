import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type {
  TenantRequestDto,
  TenantRequestDetailDto,
  ApproveTenantRequestRequest,
  RejectTenantRequestRequest,
} from '../types';

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
      const result = await axiosInstance.get<TenantRequestDto[]>(url);
      const data = result ?? [];
      setRequests(data);
      return data;
    } catch (err: any) {
      setError(err?.message || 'Failed to load requests.');
      return [];
    } finally {
      setIsLoading(false);
    }
  }, []);

  const fetchById = useCallback(async (id: string): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<TenantRequestDetailDto>(
        `/superadmin/tenant-requests/${id}`
      );
      setSelectedRequest(result ?? null);
    } catch (err: any) {
      setError(err?.message || 'Failed to load request.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const approve = useCallback(
    async (id: string, data: ApproveTenantRequestRequest): Promise<boolean> => {
      setIsSubmitting(true);
      setActionError(null);
      try {
        await axiosInstance.post(`/superadmin/tenant-requests/${id}/approve`, data);
        return true;
      } catch (err: any) {
        setActionError(err?.message || 'Approval failed.');
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
        await axiosInstance.post(`/superadmin/tenant-requests/${id}/reject`, data);
        return true;
      } catch (err: any) {
        setActionError(err?.message || 'Rejection failed.');
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
