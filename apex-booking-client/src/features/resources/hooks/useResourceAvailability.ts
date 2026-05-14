import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import { useAuth } from '../../../context/AuthContext';
import type {
  SetAvailabilityRequest,
  AvailabilityException,
  CreateExceptionRequest,
  DaySchedule,
} from '../types';

export const useResourceAvailability = (staffId: string) => {
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
      const res = await axiosInstance.get(`/staff/${staffId}/availability`, { headers });
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
  }, [staffId, tenantSlug]);

  const getExceptions = useCallback(async (pageNumber = 1, pageSize = 10) => {
    setIsLoading(true);
    setError(null);
    try {
      const res = await axiosInstance.get(`/staff/${staffId}/exceptions`, 
        { headers,
          params: { pageNumber, pageSize }
        });
      
      setExceptions(res.data ?? []);
    } catch {
      setError('Failed to load exceptions.');
    } finally {
      setIsLoading(false);
    }
  }, [staffId, tenantSlug]);

  const setSchedule = useCallback(async (request: SetAvailabilityRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    setSuccess(null);
    try {
      const res = await axiosInstance.put(
        `/staff/${staffId}/availability`,
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
  }, [staffId, tenantSlug]);

  const addException = useCallback(async (request: CreateExceptionRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    setSuccess(null);
    try {
      const res = await axiosInstance.post(
        `/staff/${staffId}/exceptions`,
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
  }, [staffId, tenantSlug, getExceptions]);

  const removeException = useCallback(async (exceptionId: string): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    setSuccess(null);
    try {
      const res = await axiosInstance.delete(
        `/staff/${staffId}/exceptions/${exceptionId}`,
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
  }, [staffId, tenantSlug, getExceptions]);

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