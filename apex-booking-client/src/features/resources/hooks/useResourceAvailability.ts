import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import { useAuth } from '../../../context/AuthContext';
import type {
  SetAvailabilityRequest,
  AvailabilityException,
  CreateExceptionRequest,
  DaySchedule,
} from '../types';

export const useResourceAvailability = (resourceId: string) => {
  const { tenantSlug } = useAuth();
  const [schedule, setScheduleState] = useState<DaySchedule[] | null>(null);
  const [exceptions, setExceptions] = useState<AvailabilityException[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const headers = { 'X-Tenant': tenantSlug };

  const getSchedule = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const res = await axiosInstance.get(`/resource/${resourceId}/availability`, { headers });
      if (res && !res.isSuccess) {
        setError(res.errors?.[0]?.message ?? 'Failed to load schedule.');
        return;
      }
      setScheduleState(res.data?.schedules ?? null);
    } catch {
      setError('Failed to load schedule.');
    } finally {
      setIsLoading(false);
    }
  }, [resourceId, tenantSlug]);

  const getExceptions = useCallback(async (pageNumber = 1, pageSize = 10) => {
    setIsLoading(true);
    setError(null);
    try {
      const res = await axiosInstance.get(`/resource/${resourceId}/exceptions`, 
        { headers,
          params: { pageNumber, pageSize }
        });
      
      setExceptions(res.data ?? []);
    } catch {
      setError('Failed to load exceptions.');
    } finally {
      setIsLoading(false);
    }
  }, [resourceId, tenantSlug]);

  const setSchedule = useCallback(async (request: SetAvailabilityRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    setSuccess(null);
    try {
      const res = await axiosInstance.put(
        `/resource/${resourceId}/availability`,
        request,
        { headers }
      );
      if (res && !res.isSuccess) {
        setError(res.errors?.[0]?.message ?? 'Failed to save schedule.');
        return false;
      }
      setSuccess('Schedule saved successfully.');
      return true;
    } catch {
      setError('Failed to save schedule.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [resourceId, tenantSlug]);

  const addException = useCallback(async (request: CreateExceptionRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    setSuccess(null);
    try {
      const res = await axiosInstance.post(
        `/resource/${resourceId}/exceptions`,
        request,
        { headers }
      );
      if (res && !res.isSuccess) {
        setError(res.errors?.[0]?.message ?? 'Failed to add exception.');
        return false;
      }
      setSuccess('Exception added.');
      await getExceptions();
      return true;
    } catch {
      setError('Failed to add exception.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [resourceId, tenantSlug, getExceptions]);

  const removeException = useCallback(async (exceptionId: string): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    setSuccess(null);
    try {
      const res = await axiosInstance.delete(
        `/resource/${resourceId}/exceptions/${exceptionId}`,
        { headers }
      );
      if (res && !res.isSuccess) {
        setError(res.errors?.[0]?.message ?? 'Failed to remove exception.');
        return false;
      }
      setSuccess('Exception removed.');
      await getExceptions();
      return true;
    } catch {
      setError('Failed to remove exception.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [resourceId, tenantSlug, getExceptions]);

  return {
    schedule,
    exceptions,
    isLoading,
    error,
    success,
    clearError: () => setError(null),
    clearSuccess: () => setSuccess(null),
    getSchedule,
    getExceptions,
    setSchedule,
    addException,
    removeException,
  };
};