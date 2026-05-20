import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { StaffDto } from '../types';

export const useMyProfile = () => {
  const [profile, setProfile] = useState<StaffDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const getMyProfile = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<StaffDto>('/staff/me');
      setProfile(result);
    } catch (err: any) {
      setError(err?.message || 'Failed to load profile.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const updatePhoto = useCallback(async (photoUrl: string | null): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    setSuccess(null);
    try {
      const result = await axiosInstance.put<StaffDto>('/staff/me/photo', { photoUrl });
      setProfile(result);
      setSuccess('Photo updated successfully.');
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to update photo.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  return {
    profile,
    isLoading,
    error,
    success,
    clearError: () => setError(null),
    clearSuccess: () => setSuccess(null),
    getMyProfile,
    updatePhoto,
  };
};
