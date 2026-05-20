import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { PagedResult } from '../../../types';
import type {
  StaffAvailabilityDto,
  SetAvailabilityRequest,
  AvailabilityException,
  CreateExceptionRequest,
  DaySchedule,
} from '../types';

export const useStaffAvailability = (staffId: string) => {
  const [schedule, setScheduleState] = useState<DaySchedule[] | null>(null);
  const [exceptions, setExceptions] = useState<AvailabilityException[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const getSchedule = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const res = await axiosInstance.get<StaffAvailabilityDto>(`/staff/${staffId}/availability`);
      setScheduleState(res.schedules ?? null);
    } catch (err: any) {
      setError(err?.message || 'Failed to load schedule.');
    } finally {
      setIsLoading(false);
    }
  }, [staffId]);

  const getExceptions = useCallback(async (pageNumber = 1, pageSize = 10) => {
    setIsLoading(true);
    setError(null);
    try {
      const res = await axiosInstance.get<PagedResult<AvailabilityException>>(`/staff/${staffId}/exceptions`, {
        params: { pageNumber, pageSize },
      });
      setExceptions(res.data ?? []);
    } catch (err: any) {
      setError(err?.message || 'Failed to load exceptions.');
    } finally {
      setIsLoading(false);
    }
  }, [staffId]);

  const setSchedule = useCallback(async (request: SetAvailabilityRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    setSuccess(null);
    try {
      await axiosInstance.put(`/staff/${staffId}/availability`, request);
      setSuccess('Schedule saved successfully.');
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to save schedule.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [staffId]);

  const addException = useCallback(async (request: CreateExceptionRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    setSuccess(null);
    try {
      await axiosInstance.post(`/staff/${staffId}/exceptions`, request);
      setSuccess('Exception added.');
      await getExceptions();
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to add exception.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [staffId, getExceptions]);

  const removeException = useCallback(async (exceptionId: string): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    setSuccess(null);
    try {
      await axiosInstance.delete(`/staff/${staffId}/exceptions/${exceptionId}`);
      setSuccess('Exception removed.');
      await getExceptions();
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to remove exception.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [staffId, getExceptions]);

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
