import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type {
  OrganizationDetailDto,
  TenantUserDto,
  CreateTenantUserRequest,
  AssignExistingUserRequest,
} from '../types';

export const useOrganizationDetail = (slug: string) => {
  const [isLoading, setIsLoading] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [detail, setDetail] = useState<OrganizationDetailDto | undefined>(undefined);

  const loadDetail = useCallback(async (): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<OrganizationDetailDto>(
        `/superadmin/organizations/${slug}`
      );
      setDetail(result ?? undefined);
    } catch (err: any) {
      setError(err?.message || 'Failed to load organization.');
    } finally {
      setIsLoading(false);
    }
  }, [slug]);

  const createUser = async (request: CreateTenantUserRequest): Promise<TenantUserDto | null> => {
    setActionLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.post<TenantUserDto>(
        `/superadmin/organizations/${slug}/users`,
        request
      );
      if (result && detail) {
        setDetail(prev =>
          prev ? { ...prev, users: [...prev.users, result], userCount: prev.userCount + 1 } : prev
        );
      }
      return result ?? null;
    } catch (err: any) {
      setError(err?.message || 'Failed to create user.');
      return null;
    } finally {
      setActionLoading(false);
    }
  };

  const assignUser = async (request: AssignExistingUserRequest): Promise<TenantUserDto | null> => {
    setActionLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.post<TenantUserDto>(
        `/superadmin/organizations/${slug}/users/assign`,
        request
      );
      if (result && detail) {
        setDetail(prev =>
          prev
            ? {
                ...prev,
                users: prev.users.map(u => (u.id === result.id ? result : u)),
              }
            : prev
        );
      }
      return result ?? null;
    } catch (err: any) {
      setError(err?.message || 'Failed to assign user.');
      return null;
    } finally {
      setActionLoading(false);
    }
  };

  const resendInvite = async (userId: string): Promise<boolean> => {
    setActionLoading(true);
    setError(null);
    try {
      await axiosInstance.post(
        `/superadmin/organizations/${slug}/users/${userId}/resend-invite`
      );
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to resend invitation.');
      return false;
    } finally {
      setActionLoading(false);
    }
  };

  return {
    loadDetail,
    createUser,
    assignUser,
    resendInvite,
    detail,
    isLoading,
    actionLoading,
    error,
    clearError: () => setError(null),
  };
};
