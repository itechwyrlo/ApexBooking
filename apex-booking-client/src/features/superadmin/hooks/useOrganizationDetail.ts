import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type {
  OrganizationDetailDto,
  TenantUserDto,
  CreateTenantUserRequest,
  AssignExistingUserRequest,
} from '../types';
import type { BaseResponse } from '../../../types';

export const useOrganizationDetail = (slug: string) => {
  const [isLoading, setIsLoading] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [detail, setDetail] = useState<OrganizationDetailDto | undefined>(undefined);

  const loadDetail = useCallback(async (): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get<BaseResponse<OrganizationDetailDto>>(
        `/superadmin/organizations/${slug}`
      );
      if (!result.isSuccess) {
        setError(result?.errors?.[0]?.message ?? 'Failed to load organization.');
        return;
      }
      setDetail(result.data ?? undefined);
    } catch {
      setError('Failed to load organization.');
    } finally {
      setIsLoading(false);
    }
  }, [slug]);

  const createUser = async (request: CreateTenantUserRequest): Promise<TenantUserDto | null> => {
    setActionLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.post<BaseResponse<TenantUserDto>>(
        `/superadmin/organizations/${slug}/users`,
        request
      );
      if (!result.isSuccess) {
        setError(result?.errors?.[0]?.message ?? 'Failed to create user.');
        return null;
      }
      if (result.data && detail) {
        setDetail(prev =>
          prev ? { ...prev, users: [...prev.users, result.data!], userCount: prev.userCount + 1 } : prev
        );
      }
      return result.data ?? null;
    } catch {
      setError('Failed to create user.');
      return null;
    } finally {
      setActionLoading(false);
    }
  };

  const assignUser = async (request: AssignExistingUserRequest): Promise<TenantUserDto | null> => {
    setActionLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.post<BaseResponse<TenantUserDto>>(
        `/superadmin/organizations/${slug}/users/assign`,
        request
      );
      if (!result.isSuccess) {
        setError(result?.errors?.[0]?.message ?? 'Failed to assign user.');
        return null;
      }
      if (result.data && detail) {
        setDetail(prev =>
          prev
            ? {
                ...prev,
                users: prev.users.map(u =>
                  u.id === result.data!.id ? result.data! : u
                ),
              }
            : prev
        );
      }
      return result.data ?? null;
    } catch {
      setError('Failed to assign user.');
      return null;
    } finally {
      setActionLoading(false);
    }
  };

  const resendInvite = async (userId: string): Promise<boolean> => {
    setActionLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.post<BaseResponse<boolean>>(
        `/superadmin/organizations/${slug}/users/${userId}/resend-invite`
      );
      if (!result.isSuccess) {
        setError(result?.errors?.[0]?.message ?? 'Failed to resend invitation.');
        return false;
      }
      return true;
    } catch {
      setError('Failed to resend invitation.');
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
